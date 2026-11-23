using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Hi_Trade.DAL;
using Hi_Trade.Models;
using Hi_Trade.Models.DTOs;
using Hi_Trade.Services;
using Hi_Trade.Services.Interfaces;
using Hi_Trade.Services.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Hi_Trade.Tests;

public class RedisCacheTests
{
    private class TestDbContextFactory : IDbContextFactory<TechScannerContext>
    {
        private readonly DbContextOptions<TechScannerContext> _options;

        public TestDbContextFactory(DbContextOptions<TechScannerContext> options)
        {
            _options = options;
        }

        public TechScannerContext CreateDbContext() => new(_options);
    }

    private static IDistributedCache CreateMemoryDistributedCache()
    {
        return new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()));
    }

    private static (IDbContextFactory<TechScannerContext> factory, TechScannerContext context) CreateInMemoryDatabase(string dbName)
    {
        var options = new DbContextOptionsBuilder<TechScannerContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;

        var factory = new TestDbContextFactory(options);
        var context = factory.CreateDbContext();

        if (!context.Retailers.Any())
        {
            context.Retailers.AddRange(
                new Retailer { Id = 1, Code = "MYTEK", Name = "MyTek", BaseUrl = "https://www.mytek.tn/" },
                new Retailer { Id = 2, Code = "TUNISIANET", Name = "TunisiaNet", BaseUrl = "https://www.tunisianet.com.tn/" },
                new Retailer { Id = 3, Code = "SPACENET", Name = "SpaceNet", BaseUrl = "https://spacenet.tn/" }
            );
            context.SaveChanges();
        }

        return (factory, context);
    }

    [Fact]
    public async Task GetOrSetAsync_CachesSerializedValueAndSkipsFactoryOnSecondCall()
    {
        var distributedCache = CreateMemoryDistributedCache();
        var cacheService = new RedisCacheService(distributedCache, NullLogger<RedisCacheService>.Instance);
        var factoryCalls = 0;

        var first = await cacheService.GetOrSetAsync(
            "test:product:1",
            _ =>
            {
                factoryCalls++;
                return Task.FromResult(new ProductComparisonDto
                {
                    Id = 1,
                    Title = "Cached Laptop",
                    CheapestPrice = 1499m,
                    HighestPrice = 1599m
                });
            },
            TimeSpan.FromMinutes(5));

        var second = await cacheService.GetOrSetAsync(
            "test:product:1",
            _ =>
            {
                factoryCalls++;
                return Task.FromResult(new ProductComparisonDto
                {
                    Id = 1,
                    Title = "Should Not Be Called",
                    CheapestPrice = 9999m
                });
            },
            TimeSpan.FromMinutes(5));

        Assert.Equal(1, factoryCalls);
        Assert.Equal("Cached Laptop", first.Title);
        Assert.Equal("Cached Laptop", second.Title);
        Assert.Equal(1499m, second.CheapestPrice);
    }

    [Fact]
    public async Task InvalidateScopeAsync_RotatesVersionToken()
    {
        var distributedCache = CreateMemoryDistributedCache();
        var cacheService = new RedisCacheService(distributedCache, NullLogger<RedisCacheService>.Instance);

        var initialVersion = await cacheService.GetVersionTokenAsync("catalog");
        await cacheService.InvalidateScopeAsync("catalog");
        var rotatedVersion = await cacheService.GetVersionTokenAsync("catalog");

        Assert.False(string.IsNullOrWhiteSpace(initialVersion));
        Assert.False(string.IsNullOrWhiteSpace(rotatedVersion));
        Assert.NotEqual(initialVersion, rotatedVersion);
    }

    [Fact]
    public async Task RedisCacheService_FailsOpen_WhenDistributedCacheThrows()
    {
        var mockCache = new Mock<IDistributedCache>();
        mockCache
            .Setup(c => c.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Redis connection unavailable"));
        mockCache
            .Setup(c => c.SetAsync(
                It.IsAny<string>(),
                It.IsAny<byte[]>(),
                It.IsAny<DistributedCacheEntryOptions>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Redis connection unavailable"));

        var cacheService = new RedisCacheService(mockCache.Object, NullLogger<RedisCacheService>.Instance);

        var result = await cacheService.GetOrSetAsync(
            "test:failopen",
            _ => Task.FromResult("fallback-db-result"));

        var version = await cacheService.GetVersionTokenAsync("catalog");
        await cacheService.InvalidateScopeAsync("catalog");

        Assert.Equal("fallback-db-result", result);
        Assert.Equal("fallback", version);
    }

    [Fact]
    public async Task ProductCatalogService_CachesQueries_AndProductIngestionInvalidatesOnPriceChange()
    {
        var distributedCache = CreateMemoryDistributedCache();
        var cacheService = new RedisCacheService(distributedCache, NullLogger<RedisCacheService>.Instance);
        var (factory, context) = CreateInMemoryDatabase(nameof(ProductCatalogService_CachesQueries_AndProductIngestionInvalidatesOnPriceChange));
        await using var _ = context;

        var ingestionService = new ProductIngestionService(
            factory,
            NullLogger<ProductIngestionService>.Instance,
            cacheService);

        await ingestionService.IngestProductAsync(new ScrapedProductItem
        {
            RetailerCode = "MYTEK",
            RetailerSku = "CACHE-SKU-1",
            Title = "ThinkPad X1 Carbon",
            ProductUrl = "https://www.mytek.tn/x1.html",
            RegularPrice = 4200m,
            FinalPrice = 4000m,
            IsInStock = true
        });

        var catalogService = new ProductCatalogService(context, cacheService);
        var filter = new ProductFilterRequest { SearchTerm = "ThinkPad" };

        // 1. First read populates the cache
        var firstQuery = await catalogService.GetProductsAsync(filter);
        var productId = firstQuery.Items.Single().Id;
        var firstDetails = await catalogService.GetProductDetailsAsync(productId);

        Assert.Single(firstQuery.Items);
        Assert.Equal(4000m, firstQuery.Items[0].CheapestPrice);
        Assert.NotNull(firstDetails);
        Assert.Equal(4000m, firstDetails!.CheapestPrice);

        // 2. Mutate database directly outside ingestion to prove second read is served from cache
        await using (var directContext = factory.CreateDbContext())
        {
            var dbProduct = await directContext.Products.FirstAsync(p => p.Id == productId);
            dbProduct.MinPrice = 3999m;
            await directContext.SaveChangesAsync();
        }

        var cachedQuery = await catalogService.GetProductsAsync(filter);
        var cachedDetails = await catalogService.GetProductDetailsAsync(productId);
        Assert.Equal(4000m, cachedQuery.Items[0].CheapestPrice);
        Assert.Equal(4000m, cachedDetails!.CheapestPrice);

        // 3. Price drop via ingestion service invalidates the catalog cache scope
        await ingestionService.IngestProductAsync(new ScrapedProductItem
        {
            RetailerCode = "MYTEK",
            RetailerSku = "CACHE-SKU-1",
            Title = "ThinkPad X1 Carbon",
            ProductUrl = "https://www.mytek.tn/x1.html",
            RegularPrice = 4200m,
            FinalPrice = 3750m,
            IsInStock = true
        });

        var refreshedQuery = await catalogService.GetProductsAsync(filter);
        var refreshedDetails = await catalogService.GetProductDetailsAsync(productId);

        Assert.Equal(3750m, refreshedQuery.Items[0].CheapestPrice);
        Assert.NotNull(refreshedDetails);
        Assert.Equal(3750m, refreshedDetails!.CheapestPrice);
    }
}
