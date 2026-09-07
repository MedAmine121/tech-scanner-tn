using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Hi_Trade.DAL;
using Hi_Trade.Models;
using Hi_Trade.Models.DTOs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Hi_Trade.Services;

public partial class ProductIngestionService : IProductIngestionService
{
    private readonly IDbContextFactory<TechScannerContext> _contextFactory;
    private readonly ILogger<ProductIngestionService> _logger;
    private static readonly ConcurrentDictionary<string, int> RetailerIdCache = new(StringComparer.OrdinalIgnoreCase);

    public ProductIngestionService(
        IDbContextFactory<TechScannerContext> contextFactory,
        ILogger<ProductIngestionService> logger)
    {
        _contextFactory = contextFactory;
        _logger = logger;
    }

    public async Task<ProductListing> IngestProductAsync(ScrapedProductItem item, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var now = DateTime.UtcNow;

        var retailerId = await GetRetailerIdAsync(context, item.RetailerCode, cancellationToken);
        var cleanSku = NormalizeSku(item.RetailerSku);

        // 1. Resolve Brand if provided
        int? brandId = null;
        if (!string.IsNullOrWhiteSpace(item.Manufacturer))
        {
            brandId = await GetOrCreateBrandIdAsync(context, item.Manufacturer, cancellationToken);
        }

        // 2. Lookup existing listing for this retailer & SKU
        var listing = await context.ProductListings
            .Include(l => l.Product)
            .FirstOrDefaultAsync(l => l.RetailerId == retailerId && l.RetailerSku == item.RetailerSku, cancellationToken);

        var isNewListing = listing == null;
        var hasPriceChanged = false;

        if (isNewListing)
        {
            listing = new ProductListing
            {
                RetailerId = retailerId,
                RetailerSku = item.RetailerSku,
                RetailerProductId = item.RetailerProductId,
                Title = item.Title,
                ProductUrl = item.ProductUrl,
                ImageUrl = item.ImageUrl,
                RegularPrice = item.RegularPrice > 0 ? item.RegularPrice : item.FinalPrice,
                FinalPrice = item.FinalPrice,
                DiscountPercentage = CalculateDiscount(item.RegularPrice, item.FinalPrice),
                IsInStock = item.IsInStock,
                StockStatusText = item.StockStatusText,
                RawManufacturer = item.Manufacturer,
                RawCategory = item.RawCategory,
                FirstScrapedAt = now,
                LastScrapedAt = now,
                LastPriceChangedAt = now,
                IsActive = true
            };

            context.ProductListings.Add(listing);
            hasPriceChanged = true;
        }
        else
        {
            // Detect if price or stock changed
            hasPriceChanged = listing!.FinalPrice != item.FinalPrice
                || listing.RegularPrice != item.RegularPrice
                || listing.IsInStock != item.IsInStock;

            listing.Title = item.Title;
            listing.ProductUrl = item.ProductUrl;
            if (!string.IsNullOrWhiteSpace(item.ImageUrl))
            {
                listing.ImageUrl = item.ImageUrl;
            }
            listing.RetailerProductId = !string.IsNullOrWhiteSpace(item.RetailerProductId) ? item.RetailerProductId : listing.RetailerProductId;
            listing.RegularPrice = item.RegularPrice > 0 ? item.RegularPrice : item.FinalPrice;
            listing.FinalPrice = item.FinalPrice;
            listing.DiscountPercentage = CalculateDiscount(item.RegularPrice, item.FinalPrice);
            listing.IsInStock = item.IsInStock;
            listing.StockStatusText = item.StockStatusText;
            listing.RawManufacturer = item.Manufacturer ?? listing.RawManufacturer;
            listing.RawCategory = item.RawCategory ?? listing.RawCategory;
            listing.LastScrapedAt = now;
            listing.IsActive = true;

            if (hasPriceChanged)
            {
                listing.LastPriceChangedAt = now;
            }
        }

        // 3. Resolve or match Canonical Product
        Product? canonicalProduct = listing.Product;

        if (canonicalProduct == null && listing.ProductId.HasValue)
        {
            canonicalProduct = await context.Products.FindAsync(new object[] { listing.ProductId.Value }, cancellationToken);
        }

        if (canonicalProduct == null && !string.IsNullOrWhiteSpace(cleanSku))
        {
            // Auto-match across websites by Normalized SKU
            canonicalProduct = await context.Products
                .FirstOrDefaultAsync(p => p.NormalizedSku == cleanSku, cancellationToken);
        }

        if (canonicalProduct == null)
        {
            // Create new canonical product
            canonicalProduct = new Product
            {
                Title = CleanProductTitle(item.Title),
                NormalizedSku = cleanSku,
                BrandId = brandId,
                PrimaryImageUrl = item.ImageUrl,
                Description = item.Description,
                MinPrice = item.FinalPrice,
                MaxPrice = item.FinalPrice,
                CheapestRetailerId = retailerId,
                HistoricalLowPrice = item.FinalPrice,
                HistoricalHighPrice = item.FinalPrice,
                OffersCount = 1,
                LastPriceCheckAt = now,
                CreatedAt = now,
                UpdatedAt = now
            };

            context.Products.Add(canonicalProduct);
            await context.SaveChangesAsync(cancellationToken);
        }

        listing.ProductId = canonicalProduct.Id;

        // 4. Record Price History ONLY when price/stock changed or for a brand new listing
        if (hasPriceChanged)
        {
            var history = new PriceHistory
            {
                ProductListing = listing,
                ProductId = canonicalProduct.Id,
                RetailerId = retailerId,
                RegularPrice = listing.RegularPrice,
                FinalPrice = listing.FinalPrice,
                IsInStock = listing.IsInStock,
                RecordedAt = now
            };

            context.PriceHistories.Add(history);
        }

        await context.SaveChangesAsync(cancellationToken);

        // 5. Recalculate canonical product aggregates
        await RecalculateProductAggregatesAsync(context, canonicalProduct.Id, cancellationToken);

        return listing;
    }

    public async Task<int> IngestBatchAsync(IEnumerable<ScrapedProductItem> items, CancellationToken cancellationToken = default)
    {
        var count = 0;
        foreach (var item in items)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }

            try
            {
                await IngestProductAsync(item, cancellationToken);
                count++;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to ingest item {Sku} for {Retailer}", item.RetailerSku, item.RetailerCode);
            }
        }

        return count;
    }

    public async Task StoreRetailerCategoriesAsync(
        string retailerCode,
        IEnumerable<CategoryDto> categories,
        CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var retailerId = await GetRetailerIdAsync(context, retailerCode, cancellationToken);
        var now = DateTime.UtcNow;

        var existingCategories = await context.RetailerCategories
            .Where(rc => rc.RetailerId == retailerId)
            .ToDictionaryAsync(rc => rc.Url, StringComparer.OrdinalIgnoreCase, cancellationToken);

        foreach (var category in categories.Where(c => !string.IsNullOrWhiteSpace(c.Url)))
        {
            if (existingCategories.TryGetValue(category.Url, out var existing))
            {
                existing.Title = category.Title;
                existing.ParentCategoryName = category.ParentCategory;
                existing.LastScrapedAt = now;
                existing.UpdatedAt = now;
            }
            else
            {
                context.RetailerCategories.Add(new RetailerCategory
                {
                    RetailerId = retailerId,
                    Url = category.Url,
                    Title = category.Title,
                    ParentCategoryName = category.ParentCategory,
                    LastScrapedAt = now,
                    CreatedAt = now,
                    UpdatedAt = now
                });
            }
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<ScrapeSession> StartScrapeSessionAsync(string retailerCode, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var retailerId = await GetRetailerIdAsync(context, retailerCode, cancellationToken);

        var session = new ScrapeSession
        {
            RetailerId = retailerId,
            StartedAt = DateTime.UtcNow,
            Status = ScrapeSessionStatus.Running
        };

        context.ScrapeSessions.Add(session);
        await context.SaveChangesAsync(cancellationToken);
        return session;
    }

    public async Task CompleteScrapeSessionAsync(
        int sessionId,
        int itemsScraped,
        int itemsUpdated,
        int newItemsCount,
        int priceChangesCount,
        string? errorMessage = null,
        CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var session = await context.ScrapeSessions.FindAsync(new object[] { sessionId }, cancellationToken);
        if (session != null)
        {
            session.FinishedAt = DateTime.UtcNow;
            session.Status = string.IsNullOrWhiteSpace(errorMessage)
                ? ScrapeSessionStatus.Completed
                : (itemsScraped > 0 ? ScrapeSessionStatus.Partial : ScrapeSessionStatus.Failed);
            session.ItemsScraped = itemsScraped;
            session.ItemsUpdated = itemsUpdated;
            session.NewItemsCount = newItemsCount;
            session.PriceChangesCount = priceChangesCount;
            session.ErrorMessage = errorMessage;

            await context.SaveChangesAsync(cancellationToken);
        }
    }

    private static async Task RecalculateProductAggregatesAsync(
        TechScannerContext context,
        int productId,
        CancellationToken cancellationToken)
    {
        var product = await context.Products.FindAsync(new object[] { productId }, cancellationToken);
        if (product == null) return;

        var activeOffers = await context.ProductListings
            .Where(l => l.ProductId == productId && l.IsActive)
            .ToListAsync(cancellationToken);

        if (activeOffers.Count == 0)
        {
            product.OffersCount = 0;
            await context.SaveChangesAsync(cancellationToken);
            return;
        }

        product.OffersCount = activeOffers.Count;

        // In-stock offers have priority for cheapest price selection
        var inStockOffers = activeOffers.Where(o => o.IsInStock && o.FinalPrice > 0).ToList();
        var poolForCheapest = inStockOffers.Count > 0 ? inStockOffers : activeOffers.Where(o => o.FinalPrice > 0).ToList();

        if (poolForCheapest.Count > 0)
        {
            var cheapest = poolForCheapest.OrderBy(o => o.FinalPrice).First();
            product.MinPrice = cheapest.FinalPrice;
            product.CheapestRetailerId = cheapest.RetailerId;
            product.MaxPrice = poolForCheapest.Max(o => o.FinalPrice);

            // Historical lowest tracking
            if (product.HistoricalLowPrice == 0 || product.MinPrice < product.HistoricalLowPrice)
            {
                product.HistoricalLowPrice = product.MinPrice;
            }

            // Historical highest tracking
            if (product.MaxPrice > product.HistoricalHighPrice)
            {
                product.HistoricalHighPrice = product.MaxPrice;
            }
        }

        product.LastPriceCheckAt = DateTime.UtcNow;
        product.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync(cancellationToken);
    }

    private static async Task<int> GetRetailerIdAsync(
        TechScannerContext context,
        string retailerCode,
        CancellationToken cancellationToken)
    {
        var code = retailerCode.ToUpperInvariant();
        if (RetailerIdCache.TryGetValue(code, out var cachedId))
        {
            return cachedId;
        }

        var retailer = await context.Retailers.FirstOrDefaultAsync(r => r.Code == code, cancellationToken);
        if (retailer == null)
        {
            retailer = new Retailer
            {
                Code = code,
                Name = code,
                BaseUrl = string.Empty,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            context.Retailers.Add(retailer);
            await context.SaveChangesAsync(cancellationToken);
        }

        RetailerIdCache[code] = retailer.Id;
        return retailer.Id;
    }

    private static async Task<int> GetOrCreateBrandIdAsync(
        TechScannerContext context,
        string manufacturer,
        CancellationToken cancellationToken)
    {
        var cleanName = manufacturer.Trim();
        var slug = GenerateSlug(cleanName);

        var brand = await context.Brands.FirstOrDefaultAsync(b => b.Slug == slug, cancellationToken);
        if (brand == null)
        {
            brand = new Brand
            {
                Name = cleanName,
                Slug = slug,
                CreatedAt = DateTime.UtcNow
            };
            context.Brands.Add(brand);
            await context.SaveChangesAsync(cancellationToken);
        }

        return brand.Id;
    }

    public static string NormalizeSku(string? rawSku)
    {
        if (string.IsNullOrWhiteSpace(rawSku)) return string.Empty;

        // Remove brackets, dashes, underscores, spaces for consistent comparison
        var cleaned = rawSku.Trim('[', ']', ' ', '\t', '\r', '\n');
        // Strip common store prefixes or suffixes
        cleaned = Regex.Replace(cleaned, @"\s+", "");
        return cleaned.ToUpperInvariant();
    }

    private static string CleanProductTitle(string title)
    {
        return Regex.Replace(title, @"\s+", " ").Trim();
    }

    private static decimal CalculateDiscount(decimal regularPrice, decimal finalPrice)
    {
        if (regularPrice > finalPrice && regularPrice > 0)
        {
            return Math.Round(((regularPrice - finalPrice) / regularPrice) * 100m, 2);
        }
        return 0;
    }

    private static string GenerateSlug(string text)
    {
        var str = text.ToLowerInvariant();
        str = Regex.Replace(str, @"[^a-z0-9\s-]", "");
        str = Regex.Replace(str, @"\s+", "-").Trim('-');
        return string.IsNullOrWhiteSpace(str) ? "brand-" + Guid.NewGuid().ToString("N")[..6] : str;
    }
}

