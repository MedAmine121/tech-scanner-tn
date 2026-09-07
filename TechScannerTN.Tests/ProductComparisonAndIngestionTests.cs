using System;
using System.Linq;
using System.Threading.Tasks;
using Hi_Trade.DAL;
using Hi_Trade.Models;
using Hi_Trade.Models.DTOs;
using Hi_Trade.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Hi_Trade.Tests;

public class ProductComparisonAndIngestionTests
{
    private class TestDbContextFactory : IDbContextFactory<TechScannerContext>
    {
        private readonly DbContextOptions<TechScannerContext> _options;

        public TestDbContextFactory(DbContextOptions<TechScannerContext> options)
        {
            _options = options;
        }

        public TechScannerContext CreateDbContext()
        {
            return new TechScannerContext(_options);
        }
    }

    private static (IDbContextFactory<TechScannerContext> factory, DbContextOptions<TechScannerContext> options) CreateInMemoryFactory(string dbName)
    {
        var options = new DbContextOptionsBuilder<TechScannerContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;

        var factory = new TestDbContextFactory(options);

        // Seed initial retailers
        using var context = factory.CreateDbContext();
        if (!context.Retailers.Any())
        {
            context.Retailers.AddRange(
                new Retailer { Id = 1, Code = "MYTEK", Name = "MyTek", BaseUrl = "https://www.mytek.tn/" },
                new Retailer { Id = 2, Code = "TUNISIANET", Name = "TunisiaNet", BaseUrl = "https://www.tunisianet.com.tn/" },
                new Retailer { Id = 3, Code = "SPACENET", Name = "SpaceNet", BaseUrl = "https://spacenet.tn/" }
            );
            context.SaveChanges();
        }

        return (factory, options);
    }

    [Fact]
    public async Task MultiSiteIngestion_SameProduct_MatchesCanonicalAndIdentifiesCheapestStore()
    {
        // Arrange
        var (factory, options) = CreateInMemoryFactory(nameof(MultiSiteIngestion_SameProduct_MatchesCanonicalAndIdentifiesCheapestStore));
        var ingestionService = new ProductIngestionService(factory, NullLogger<ProductIngestionService>.Instance);

        // Act - Ingest the same laptop from 3 different stores with slight formatting differences in reference
        await ingestionService.IngestProductAsync(new ScrapedProductItem
        {
            RetailerCode = "MYTEK",
            RetailerSku = "82RK009CFE",
            Title = "PC Portable Lenovo IdeaPad 3 15IAU7 - Gris",
            ProductUrl = "https://www.mytek.tn/pc-lenovo-82rk009cfe.html",
            RegularPrice = 1489m,
            FinalPrice = 1450m,
            IsInStock = true,
            Manufacturer = "Lenovo"
        });

        await ingestionService.IngestProductAsync(new ScrapedProductItem
        {
            RetailerCode = "TUNISIANET",
            RetailerSku = "[82RK009CFE]", // TunisiaNet wraps in brackets
            Title = "PC Portable Lenovo IdeaPad 3 15IAU7 / i5 12è Gén / 8 Go",
            ProductUrl = "https://www.tunisianet.com.tn/pc-portable/lenovo-82rk009cfe.html",
            RegularPrice = 1499m,
            FinalPrice = 1429m, // Cheapest!
            IsInStock = true,
            Manufacturer = "Lenovo"
        });

        await ingestionService.IngestProductAsync(new ScrapedProductItem
        {
            RetailerCode = "SPACENET",
            RetailerSku = " 82RK009CFE ", // Extra whitespace
            Title = "Pc Portable Lenovo Ideapad 3 15IAU7 I5 12è Gén 8Go",
            ProductUrl = "https://spacenet.tn/pc-portable/lenovo-ideapad-3-82rk009cfe.html",
            RegularPrice = 1499m,
            FinalPrice = 1499m,
            IsInStock = true,
            Manufacturer = "Lenovo"
        });

        // Assert
        await using var context = factory.CreateDbContext();
        var products = await context.Products
            .Include(p => p.Listings)
            .Include(p => p.CheapestRetailer)
            .ToListAsync();

        Assert.Single(products); // Perfectly matched into 1 single canonical product!
        var canonical = products.First();

        Assert.Equal("82RK009CFE", canonical.NormalizedSku);
        Assert.Equal(3, canonical.OffersCount);
        Assert.Equal(3, canonical.Listings.Count);

        // Verification of cheapest price tracking
        Assert.Equal(1429m, canonical.MinPrice);
        Assert.Equal(1499m, canonical.MaxPrice);
        Assert.NotNull(canonical.CheapestRetailer);
        Assert.Equal("TUNISIANET", canonical.CheapestRetailer!.Code);
        Assert.Equal(1429m, canonical.HistoricalLowPrice);
    }

    [Fact]
    public async Task Ingestion_IdempotentScrape_DoesNotDuplicateListingsOrPriceHistory()
    {
        // Arrange
        var (factory, options) = CreateInMemoryFactory(nameof(Ingestion_IdempotentScrape_DoesNotDuplicateListingsOrPriceHistory));
        var ingestionService = new ProductIngestionService(factory, NullLogger<ProductIngestionService>.Instance);

        var item = new ScrapedProductItem
        {
            RetailerCode = "MYTEK",
            RetailerSku = "20UD000RFE",
            Title = "ThinkPad E14 Gen 2",
            ProductUrl = "https://www.mytek.tn/thinkpad-e14.html",
            RegularPrice = 2100m,
            FinalPrice = 1999m,
            IsInStock = true,
            Manufacturer = "Lenovo"
        };

        // Act - First scrape
        await ingestionService.IngestProductAsync(item);

        // Second scrape (identical prices)
        await ingestionService.IngestProductAsync(item);

        // Assert
        await using var context = factory.CreateDbContext();
        var listings = await context.ProductListings.ToListAsync();
        var histories = await context.PriceHistories.ToListAsync();

        Assert.Single(listings); // No duplicate listing
        Assert.Single(histories); // No duplicate price history record
    }

    [Fact]
    public async Task Ingestion_PriceChange_RecordsPriceHistoryAndUpdatesAggregates()
    {
        // Arrange
        var (factory, options) = CreateInMemoryFactory(nameof(Ingestion_PriceChange_RecordsPriceHistoryAndUpdatesAggregates));
        var ingestionService = new ProductIngestionService(factory, NullLogger<ProductIngestionService>.Instance);

        // 1. Initial ingestion at 1450 DT
        await ingestionService.IngestProductAsync(new ScrapedProductItem
        {
            RetailerCode = "MYTEK",
            RetailerSku = "TEST-SKU-1",
            Title = "Test Laptop",
            ProductUrl = "https://www.mytek.tn/test-laptop.html",
            RegularPrice = 1500m,
            FinalPrice = 1450m,
            IsInStock = true
        });

        // 2. Price drop at MyTek to 1380 DT
        await ingestionService.IngestProductAsync(new ScrapedProductItem
        {
            RetailerCode = "MYTEK",
            RetailerSku = "TEST-SKU-1",
            Title = "Test Laptop",
            ProductUrl = "https://www.mytek.tn/test-laptop.html",
            RegularPrice = 1500m,
            FinalPrice = 1380m,
            IsInStock = true
        });

        // Assert
        await using var context = factory.CreateDbContext();
        var histories = await context.PriceHistories.OrderBy(h => h.RecordedAt).ToListAsync();
        var product = await context.Products.FirstAsync();

        Assert.Equal(2, histories.Count); // Price change recorded!
        Assert.Equal(1450m, histories[0].FinalPrice);
        Assert.Equal(1380m, histories[1].FinalPrice);

        Assert.Equal(1380m, product.MinPrice);
        Assert.Equal(1380m, product.HistoricalLowPrice);
    }

    [Fact]
    public async Task ProductCatalogService_ReturnsComparisonWithCorrectDifferences()
    {
        // Arrange
        var (factory, options) = CreateInMemoryFactory(nameof(ProductCatalogService_ReturnsComparisonWithCorrectDifferences));
        var ingestionService = new ProductIngestionService(factory, NullLogger<ProductIngestionService>.Instance);

        await ingestionService.IngestProductAsync(new ScrapedProductItem
        {
            RetailerCode = "MYTEK",
            RetailerSku = "DIFF-TEST-SKU",
            Title = "Dell Inspiron 15",
            ProductUrl = "https://www.mytek.tn/dell-inspiron.html",
            RegularPrice = 1600m,
            FinalPrice = 1500m,
            IsInStock = true,
            Manufacturer = "Dell"
        });

        await ingestionService.IngestProductAsync(new ScrapedProductItem
        {
            RetailerCode = "TUNISIANET",
            RetailerSku = "DIFF-TEST-SKU",
            Title = "Dell Inspiron 15",
            ProductUrl = "https://www.tunisianet.com.tn/dell-inspiron.html",
            RegularPrice = 1550m,
            FinalPrice = 1400m, // Cheapest (100 DT cheaper than MyTek)
            IsInStock = true,
            Manufacturer = "Dell"
        });

        await using var context = factory.CreateDbContext();
        var catalogService = new ProductCatalogService(context);
        var product = await context.Products.FirstAsync();

        // Act
        var details = await catalogService.GetProductDetailsAsync(product.Id);

        // Assert
        Assert.NotNull(details);
        Assert.Equal(1400m, details!.CheapestPrice);
        Assert.Equal(1500m, details.HighestPrice);
        Assert.Equal(100m, details.MaxSavingsAmount);
        Assert.Equal(6.67m, details.MaxSavingsPercentage);

        var cheapestOffer = details.Offers.First(o => o.IsCheapest);
        Assert.Equal("TUNISIANET", cheapestOffer.RetailerCode);
        Assert.Equal(0m, cheapestOffer.DifferenceVsCheapest);
        Assert.Equal(0m, cheapestOffer.DifferencePercentage);

        var expensiveOffer = details.Offers.First(o => !o.IsCheapest);
        Assert.Equal("MYTEK", expensiveOffer.RetailerCode);
        Assert.Equal(100m, expensiveOffer.DifferenceVsCheapest);
        Assert.Equal(7.14m, expensiveOffer.DifferencePercentage); // (1500 - 1400) / 1400 * 100
    }

    [Fact]
    public async Task ProductCatalogService_MarketOverview_CalculatesRetailerCompetitiveness()
    {
        // Arrange
        var (factory, options) = CreateInMemoryFactory(nameof(ProductCatalogService_MarketOverview_CalculatesRetailerCompetitiveness));
        var ingestionService = new ProductIngestionService(factory, NullLogger<ProductIngestionService>.Instance);

        // TunisiaNet cheapest for SKU-1
        await ingestionService.IngestProductAsync(new ScrapedProductItem
        {
            RetailerCode = "TUNISIANET",
            RetailerSku = "OVERVIEW-SKU-1",
            Title = "Product 1",
            ProductUrl = "https://tunisianet.tn/1",
            FinalPrice = 100m,
            IsInStock = true
        });
        await ingestionService.IngestProductAsync(new ScrapedProductItem
        {
            RetailerCode = "MYTEK",
            RetailerSku = "OVERVIEW-SKU-1",
            Title = "Product 1",
            ProductUrl = "https://mytek.tn/1",
            FinalPrice = 120m,
            IsInStock = true
        });

        // MyTek cheapest for SKU-2
        await ingestionService.IngestProductAsync(new ScrapedProductItem
        {
            RetailerCode = "MYTEK",
            RetailerSku = "OVERVIEW-SKU-2",
            Title = "Product 2",
            ProductUrl = "https://mytek.tn/2",
            FinalPrice = 200m,
            IsInStock = true
        });
        await ingestionService.IngestProductAsync(new ScrapedProductItem
        {
            RetailerCode = "TUNISIANET",
            RetailerSku = "OVERVIEW-SKU-2",
            Title = "Product 2",
            ProductUrl = "https://tunisianet.tn/2",
            FinalPrice = 250m,
            IsInStock = true
        });

        await using var context = factory.CreateDbContext();
        var catalogService = new ProductCatalogService(context);

        // Act
        var overview = await catalogService.GetMarketOverviewAsync();

        // Assert
        Assert.Equal(2, overview.TotalCanonicalProducts);
        Assert.Equal(4, overview.TotalListingsTracked);

        var mytekStats = overview.RetailerStats.First(s => s.RetailerCode == "MYTEK");
        var tunisianetStats = overview.RetailerStats.First(s => s.RetailerCode == "TUNISIANET");

        Assert.Equal(1, mytekStats.CheapestOffersCount);
        Assert.Equal(50m, mytekStats.CheapestMarketSharePercentage);

        Assert.Equal(1, tunisianetStats.CheapestOffersCount);
        Assert.Equal(50m, tunisianetStats.CheapestMarketSharePercentage);
    }
}

