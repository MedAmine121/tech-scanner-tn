using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Hi_Trade.DAL;
using Hi_Trade.Models;
using Hi_Trade.Models.DTOs;
using Microsoft.EntityFrameworkCore;

namespace Hi_Trade.Services;

public class ProductCatalogService : IProductCatalogService
{
    private readonly TechScannerContext _context;

    public ProductCatalogService(TechScannerContext context)
    {
        _context = context;
    }

    public async Task<PagedResult<ProductComparisonDto>> GetProductsAsync(
        ProductFilterRequest request,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Products
            .AsNoTracking()
            .Include(p => p.Brand)
            .Include(p => p.Category)
            .Include(p => p.CheapestRetailer)
            .Include(p => p.Listings)
                .ThenInclude(l => l.Retailer)
            .AsQueryable();

        // Search
        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var term = request.SearchTerm.Trim();
            query = query.Where(p => p.Title.Contains(term) || (p.NormalizedSku != null && p.NormalizedSku.Contains(term)));
        }

        // Filters
        if (request.BrandId.HasValue)
        {
            query = query.Where(p => p.BrandId == request.BrandId.Value);
        }

        if (request.CategoryId.HasValue)
        {
            query = query.Where(p => p.CategoryId == request.CategoryId.Value);
        }

        if (request.RetailerId.HasValue)
        {
            query = query.Where(p => p.Listings.Any(l => l.RetailerId == request.RetailerId.Value && l.IsActive));
        }

        if (request.MinPrice.HasValue)
        {
            query = query.Where(p => p.MinPrice >= request.MinPrice.Value);
        }

        if (request.MaxPrice.HasValue)
        {
            query = query.Where(p => p.MinPrice <= request.MaxPrice.Value);
        }

        if (request.OnlyInStock == true)
        {
            query = query.Where(p => p.Listings.Any(l => l.IsInStock && l.IsActive));
        }

        if (request.OnlyHistoricalLows == true)
        {
            query = query.Where(p => p.MinPrice > 0 && p.MinPrice <= p.HistoricalLowPrice);
        }

        // Sorting
        query = request.SortBy?.ToLowerInvariant() switch
        {
            "expensive" => query.OrderByDescending(p => p.MinPrice),
            "savings" => query.OrderByDescending(p => p.MaxPrice - p.MinPrice),
            "newest" => query.OrderByDescending(p => p.CreatedAt),
            "name" => query.OrderBy(p => p.Title),
            _ => query.OrderBy(p => p.MinPrice) // Default to cheapest first
        };

        var totalCount = await query.CountAsync(cancellationToken);
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);

        var products = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var items = products.Select(MapToComparisonDto).ToList();

        return new PagedResult<ProductComparisonDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<ProductComparisonDto?> GetProductDetailsAsync(
        int productId,
        CancellationToken cancellationToken = default)
    {
        var product = await _context.Products
            .AsNoTracking()
            .Include(p => p.Brand)
            .Include(p => p.Category)
            .Include(p => p.CheapestRetailer)
            .Include(p => p.Listings)
                .ThenInclude(l => l.Retailer)
            .FirstOrDefaultAsync(p => p.Id == productId, cancellationToken);

        return product == null ? null : MapToComparisonDto(product);
    }

    public async Task<IReadOnlyList<PriceHistoryDto>> GetPriceHistoryAsync(
        int productId,
        CancellationToken cancellationToken = default)
    {
        return await _context.PriceHistories
            .AsNoTracking()
            .Where(ph => ph.ProductId == productId)
            .OrderBy(ph => ph.RecordedAt)
            .Select(ph => new PriceHistoryDto
            {
                Id = ph.Id,
                RetailerId = ph.RetailerId,
                RetailerName = ph.Retailer.Name,
                RetailerCode = ph.Retailer.Code,
                RegularPrice = ph.RegularPrice,
                FinalPrice = ph.FinalPrice,
                IsInStock = ph.IsInStock,
                RecordedAt = ph.RecordedAt
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ProductComparisonDto>> GetPriceDropsAsync(
        int limit = 20,
        CancellationToken cancellationToken = default)
    {
        var products = await _context.Products
            .AsNoTracking()
            .Where(p => p.MinPrice > 0 && p.MinPrice <= p.HistoricalLowPrice && p.Listings.Any(l => l.IsInStock && l.IsActive))
            .Include(p => p.Brand)
            .Include(p => p.Category)
            .Include(p => p.CheapestRetailer)
            .Include(p => p.Listings)
                .ThenInclude(l => l.Retailer)
            .OrderByDescending(p => p.MaxPrice - p.MinPrice)
            .Take(Math.Clamp(limit, 1, 100))
            .ToListAsync(cancellationToken);

        return products.Select(MapToComparisonDto).ToList();
    }

    public async Task<MarketOverviewDto> GetMarketOverviewAsync(CancellationToken cancellationToken = default)
    {
        var totalCanonicalProducts = await _context.Products.CountAsync(cancellationToken);
        var totalListings = await _context.ProductListings.CountAsync(l => l.IsActive, cancellationToken);

        var past24h = DateTime.UtcNow.AddHours(-24);
        var priceChangesLast24h = await _context.PriceHistories.CountAsync(h => h.RecordedAt >= past24h, cancellationToken);

        var atAllTimeLow = await _context.Products
            .CountAsync(p => p.MinPrice > 0 && p.MinPrice <= p.HistoricalLowPrice, cancellationToken);

        var retailers = await _context.Retailers.Where(r => r.IsActive).ToListAsync(cancellationToken);
        var statsList = new List<RetailerCompetitivenessDto>();

        foreach (var retailer in retailers)
        {
            var totalOffers = await _context.ProductListings.CountAsync(l => l.RetailerId == retailer.Id && l.IsActive, cancellationToken);
            var inStockOffers = await _context.ProductListings.CountAsync(l => l.RetailerId == retailer.Id && l.IsActive && l.IsInStock, cancellationToken);
            var cheapestCount = await _context.Products.CountAsync(p => p.CheapestRetailerId == retailer.Id, cancellationToken);

            var avgDiscount = totalOffers > 0
                ? await _context.ProductListings
                    .Where(l => l.RetailerId == retailer.Id && l.IsActive && l.DiscountPercentage > 0)
                    .AverageAsync(l => (decimal?)l.DiscountPercentage, cancellationToken) ?? 0
                : 0;

            statsList.Add(new RetailerCompetitivenessDto
            {
                RetailerId = retailer.Id,
                RetailerCode = retailer.Code,
                RetailerName = retailer.Name,
                TotalOffersCount = totalOffers,
                InStockOffersCount = inStockOffers,
                CheapestOffersCount = cheapestCount,
                CheapestMarketSharePercentage = totalCanonicalProducts > 0 ? Math.Round(((decimal)cheapestCount / totalCanonicalProducts) * 100, 1) : 0,
                OutOfStockRatePercentage = totalOffers > 0 ? Math.Round(((decimal)(totalOffers - inStockOffers) / totalOffers) * 100, 1) : 0,
                AverageDiscountPercentage = Math.Round(avgDiscount, 1)
            });
        }

        return new MarketOverviewDto
        {
            TotalCanonicalProducts = totalCanonicalProducts,
            TotalListingsTracked = totalListings,
            PriceChangesRecordedLast24h = priceChangesLast24h,
            ProductsAtAllTimeLow = atAllTimeLow,
            RetailerStats = statsList
        };
    }

    private static ProductComparisonDto MapToComparisonDto(Product product)
    {
        var activeListings = product.Listings.Where(l => l.IsActive).ToList();
        var lowestPrice = product.MinPrice;

        var offers = activeListings
            .OrderBy(l => l.FinalPrice)
            .Select(l =>
            {
                var diff = lowestPrice > 0 ? l.FinalPrice - lowestPrice : 0;
                var diffPct = lowestPrice > 0 && diff > 0 ? Math.Round((diff / lowestPrice) * 100, 2) : 0;

                return new RetailerOfferDto
                {
                    ListingId = l.Id,
                    RetailerId = l.RetailerId,
                    RetailerCode = l.Retailer?.Code ?? string.Empty,
                    RetailerName = l.Retailer?.Name ?? string.Empty,
                    RetailerLogoUrl = l.Retailer?.LogoUrl,
                    RetailerSku = l.RetailerSku,
                    Title = l.Title,
                    ProductUrl = l.ProductUrl,
                    ImageUrl = l.ImageUrl,
                    RegularPrice = l.RegularPrice,
                    FinalPrice = l.FinalPrice,
                    DiscountPercentage = l.DiscountPercentage,
                    IsInStock = l.IsInStock,
                    StockStatusText = l.StockStatusText,
                    IsCheapest = lowestPrice > 0 && l.FinalPrice == lowestPrice,
                    DifferenceVsCheapest = diff,
                    DifferencePercentage = diffPct,
                    LastScrapedAt = l.LastScrapedAt
                };
            })
            .ToList();

        return new ProductComparisonDto
        {
            Id = product.Id,
            Title = product.Title,
            NormalizedSku = product.NormalizedSku,
            Ean = product.Ean,
            BrandName = product.Brand?.Name,
            CategoryName = product.Category?.Name,
            PrimaryImageUrl = product.PrimaryImageUrl,
            Description = product.Description,
            Specifications = product.Specifications,
            CheapestPrice = product.MinPrice,
            HighestPrice = product.MaxPrice,
            CheapestRetailerName = product.CheapestRetailer?.Name,
            CheapestRetailerId = product.CheapestRetailerId,
            HistoricalLowPrice = product.HistoricalLowPrice,
            HistoricalHighPrice = product.HistoricalHighPrice,
            OffersCount = product.OffersCount,
            Offers = offers
        };
    }
}

