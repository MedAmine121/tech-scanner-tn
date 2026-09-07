using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Hi_Trade.Models;
using Hi_Trade.Models.DTOs;

namespace Hi_Trade.Services;

public interface IProductIngestionService
{
    /// <summary>
    /// Ingests a single scraped product: handles retailer lookup, SKU normalization,
    /// canonical product matching/creation, idempotent listing upsert, price change detection,
    /// price history logging, and precomputed price aggregate updates.
    /// </summary>
    Task<ProductListing> IngestProductAsync(ScrapedProductItem item, CancellationToken cancellationToken = default);

    /// <summary>
    /// Ingests a collection of scraped items within a single database transaction/batch.
    /// </summary>
    Task<int> IngestBatchAsync(IEnumerable<ScrapedProductItem> items, CancellationToken cancellationToken = default);

    /// <summary>
    /// Persists store categories for a given retailer without duplication.
    /// </summary>
    Task StoreRetailerCategoriesAsync(string retailerCode, IEnumerable<CategoryDto> categories, CancellationToken cancellationToken = default);

    /// <summary>
    /// Starts a scrape audit session for tracking scraper health and metrics.
    /// </summary>
    Task<ScrapeSession> StartScrapeSessionAsync(string retailerCode, CancellationToken cancellationToken = default);

    /// <summary>
    /// Completes a scrape session recording metrics and potential errors.
    /// </summary>
    Task CompleteScrapeSessionAsync(
        int sessionId,
        int itemsScraped,
        int itemsUpdated,
        int newItemsCount,
        int priceChangesCount,
        string? errorMessage = null,
        CancellationToken cancellationToken = default);
}

