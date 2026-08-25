using Hi_Trade.DAL;
using Hi_Trade.Models;
using HtmlAgilityPack;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Globalization;
using System.Net.Http;
using System.Text.RegularExpressions;

namespace Hi_Trade.Services;

public sealed class SpacenetScraperService : IWebScraper
{
    private const string ClientName = "Spacenet";
    private const int MaxDegreeOfParallelism = 3;
    private static readonly Uri SpacenetBaseUri = new("https://spacenet.tn/");

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<SpacenetScraperService> _logger;
    private readonly TechScannerContext _context;
    private readonly IDbContextFactory<TechScannerContext> _contextFactory;

    public string ProviderName { get; } = "SpaceNet";

    public SpacenetScraperService(
        IHttpClientFactory httpClientFactory,
        ILogger<SpacenetScraperService> logger,
        TechScannerContext context,
        IDbContextFactory<TechScannerContext> contextFactory)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _context = context;
        _contextFactory = contextFactory;
    }

    public async Task<List<Product>> ScrapeAsync()
    {
        try
        {
            var categories = (await GetCategoriesAsync()).ToArray();
            await StoreCategoriesAsync(categories);

            var products = new ConcurrentBag<Product>();
            var processedReferences = new ConcurrentDictionary<string, byte>(StringComparer.OrdinalIgnoreCase);

            await Parallel.ForEachAsync(
                categories.Where(c => !string.IsNullOrWhiteSpace(c.Url)),
                new ParallelOptions { MaxDegreeOfParallelism = MaxDegreeOfParallelism },
                async (category, cancellationToken) =>
                {
                    await ScrapeCategoryAsync(category, products, processedReferences, cancellationToken);
                });

            var productList = products.ToList();
            _logger.LogInformation("Spacenet scraping completed. Found {ProductCount} unique products.", productList.Count);
            return productList;
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Error scraping Spacenet categories and products.");
            return [];
        }
    }

    public async Task<IEnumerable<CategoryDto>> GetCategoriesAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var client = _httpClientFactory.CreateClient(ClientName);
            using var response = await client.GetAsync("https://spacenet.tn/", HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            response.EnsureSuccessStatusCode();

            var html = await response.Content.ReadAsStringAsync(cancellationToken);
            return ParseCategories(html);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (HttpRequestException exception)
        {
            _logger.LogError(exception, "Could not retrieve the Spacenet menu.");
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Could not parse the Spacenet menu.");
            throw new InvalidOperationException("The Spacenet menu could not be parsed.", exception);
        }
    }

    private static IReadOnlyList<CategoryDto> ParseCategories(string html)
    {
        var document = new HtmlDocument();
        document.LoadHtml(html);

        var categories = new List<CategoryDto>();

        // The vertical menu is inside <div id="sp-vermegamenu">
        var menuNode = document.DocumentNode.SelectSingleNode("//div[@id='sp-vermegamenu']");
        if (menuNode == null)
            return categories;

        // Top-level categories are <li class="item-1 vertical-cat parent">
        var topLevelItems = menuNode.SelectNodes(".//li[contains(@class,'item-1') and contains(@class,'vertical-cat') and contains(@class,'parent')]")
            ?? Enumerable.Empty<HtmlNode>();

        foreach (var topItem in topLevelItems)
        {
            var parentLink = topItem.SelectSingleNode(".//a[contains(@class,'item-1')]");
            if (parentLink == null)
                continue;

            var parentCategory = CleanText(parentLink.InnerText);
            var parentUrl = parentLink.GetAttributeValue("href", string.Empty);

            // Add the parent category itself if it has a URL
            if (!string.IsNullOrWhiteSpace(parentUrl))
            {
                categories.Add(new CategoryDto(
                    string.Empty, // no parent for top level
                    parentCategory,
                    ToAbsoluteUrl(parentUrl)
                ));
            }

            // Look for subcategories inside the dropdown
            var subItems = topItem.SelectNodes(".//div[contains(@class,'dropdown-menu')]//li[contains(@class,'cat-child')]")
                ?? Enumerable.Empty<HtmlNode>();

            foreach (var subItem in subItems)
            {
                var subLink = subItem.SelectSingleNode(".//a");
                if (subLink == null)
                    continue;

                var subTitle = CleanText(subLink.InnerText);
                var subUrl = subLink.GetAttributeValue("href", string.Empty);

                if (!string.IsNullOrWhiteSpace(subUrl))
                {
                    categories.Add(new CategoryDto(
                        parentCategory,
                        subTitle,
                        ToAbsoluteUrl(subUrl)
                    ));
                }

                // Additionally, look for third-level categories (if any)
                var thirdLevelItems = subItem.SelectNodes(".//ul[contains(@class,'level-3')]//li[contains(@class,'item-3')]//a")
                    ?? Enumerable.Empty<HtmlNode>();

                foreach (var thirdLink in thirdLevelItems)
                {
                    var thirdTitle = CleanText(thirdLink.InnerText);
                    var thirdUrl = thirdLink.GetAttributeValue("href", string.Empty);

                    if (!string.IsNullOrWhiteSpace(thirdUrl))
                    {
                        categories.Add(new CategoryDto(
                            subTitle,
                            thirdTitle,
                            ToAbsoluteUrl(thirdUrl)
                        ));
                    }
                }
            }
        }

        return categories
            .DistinctBy(c => new { c.ParentCategory, c.Title, c.Url })
            .ToArray();
    }

    private async Task StoreCategoriesAsync(IEnumerable<CategoryDto> categories)
    {
        var now = DateTime.UtcNow;

        foreach (var category in categories.Where(c => !string.IsNullOrWhiteSpace(c.Url)))
        {
            var existingCategory = await _context.Categories.FindAsync(category.Url);

            if (existingCategory is null)
            {
                _context.Categories.Add(new Category
                {
                    Url = category.Url,
                    ParentCategory = category.ParentCategory,
                    Title = category.Title,
                    CreatedAt = now,
                    UpdatedAt = now
                });
            }
            else
            {
                existingCategory.ParentCategory = category.ParentCategory;
                existingCategory.Title = category.Title;
                existingCategory.UpdatedAt = now;
            }
        }

        await _context.SaveChangesAsync();
    }

    private async Task ScrapeCategoryAsync(
        CategoryDto category,
        ConcurrentBag<Product> products,
        ConcurrentDictionary<string, byte> processedReferences,
        CancellationToken cancellationToken)
    {
        var client = _httpClientFactory.CreateClient(ClientName);
        var pageUrl = category.Url;
        var visitedPages = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        while (!string.IsNullOrWhiteSpace(pageUrl) && visitedPages.Add(pageUrl))
        {
            string? html;
            try
            {
                using var response = await client.GetAsync(pageUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError(
                        "Failed to fetch Spacenet category page {PageUrl}. Status code: {StatusCode}",
                        pageUrl,
                        response.StatusCode);
                    return;
                }

                html = await response.Content.ReadAsStringAsync(cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                _logger.LogWarning(exception, "Failed to fetch Spacenet category page {PageUrl}", pageUrl);
                return;
            }

            var document = new HtmlDocument();
            document.LoadHtml(html);

            // Each product is inside a div with class "field-product-item item-inner product-miniature"
            var productNodes = document.DocumentNode.SelectNodes(
                "//div[contains(@class,'field-product-item') and contains(@class,'product-miniature')]")
                ?? Enumerable.Empty<HtmlNode>();

            foreach (var card in productNodes)
            {
                var product = ParseProduct(card, category);
                if (product is not null && processedReferences.TryAdd(product.ProductReference, 0))
                {
                    await StoreProductAsync(product);
                }
            }

            pageUrl = GetNextPageUrl(document);
        }
    }

    private async Task StoreProductAsync(Product product)
    {
        DateTime now = DateTime.UtcNow;
        product.ScrapedAt = new DateTime(
            now.Year,
            now.Month,
            now.Day,
            now.Hour,
            0,
            0
        );

        await using var newContext = await _contextFactory.CreateDbContextAsync();
        bool exists = await newContext.Products.AnyAsync(p =>
            p.ProductId == product.ProductId && p.ScrapedAt.Equals(product.ScrapedAt));

        if (!exists)
        {
            newContext.Products.Add(product);
            await newContext.SaveChangesAsync();
            _logger.LogInformation($"Stored Spacenet product: {product.ProductReference}");
        }
    }

    private static Product? ParseProduct(HtmlNode card, CategoryDto category)
    {
        // Get product ID from the data-id-product attribute (if present)
        var productId = card.GetAttributeValue("data-id-product", string.Empty);

        // Reference is inside <div class="product-reference"> <span>
        var referenceNode = card.SelectSingleNode(".//div[contains(@class,'product-reference')]//span");
        var reference = referenceNode?.InnerText?.Trim() ?? string.Empty;

        // Title is inside <h2 class="product_name"><a>
        var titleNode = card.SelectSingleNode(".//h2[contains(@class,'product_name')]//a");
        var title = CleanText(titleNode?.InnerText);

        // Product URL from the same anchor
        var productUrl = titleNode?.GetAttributeValue("href", string.Empty) ?? string.Empty;

        // Image: inside <a class="thumbnail">, first img with class "cover_image"
        var imageNode = card.SelectSingleNode(".//a[contains(@class,'thumbnail')]//span[contains(@class,'cover_image')]//img");
        var imageUrl = imageNode?.GetAttributeValue("src", string.Empty) ?? string.Empty;

        // Price parsing
        var priceSpan = card.SelectSingleNode(".//div[contains(@class,'product-price-and-shipping')]//span[contains(@class,'price')]");
        var regularPriceSpan = card.SelectSingleNode(".//div[contains(@class,'product-price-and-shipping')]//span[contains(@class,'regular-price')]");

        decimal price = ParsePrice(priceSpan?.InnerText);
        decimal regularPrice = ParsePrice(regularPriceSpan?.InnerText);
        decimal finalPrice = price;
        // If regular price exists and is higher, then it's a discount; final price is the price span
        if (regularPrice > 0 && regularPrice > price)
        {
            // Price is final, regular is original
        }
        else
        {
            // If no regular price, price is the standard price
            regularPrice = price;
        }

        // Stock status
        bool isInStock = true;
        var stockLabel = card.SelectSingleNode(".//div[contains(@class,'product-quantities')]//label");
        if (stockLabel != null)
        {
            string stockText = CleanText(stockLabel.InnerText);
            isInStock = stockText.Contains("En stock", StringComparison.OrdinalIgnoreCase);
        }

        // Manufacturer: from <div class="product-manufacturer"> <img alt="..."
        var manufacturerNode = card.SelectSingleNode(".//div[contains(@class,'product-manufacturer')]//img");
        var manufacturer = manufacturerNode?.GetAttributeValue("alt", string.Empty) ?? string.Empty;

        // Description: from <div class="decriptions-short">
        var descriptionNode = card.SelectSingleNode(".//div[contains(@class,'decriptions-short')]");
        var description = CleanText(descriptionNode?.InnerText);

        // For ERP stock we don't have an obvious field, leave empty
        string erpStock = string.Empty;

        // If reference is empty, fallback to productId or generate from title
        if (string.IsNullOrWhiteSpace(reference))
        {
            reference = productId;
        }

        // If we still don't have a valid reference, skip this product
        if (string.IsNullOrWhiteSpace(reference))
            return null;

        return new Product
        {
            ProductId = CleanText(productId),
            ProductReference = CleanText(reference),
            Title = CleanText(title),
            ProductUrl = ToAbsoluteUrl(productUrl),
            ImageUrl = ToAbsoluteUrl(imageUrl),
            Price = regularPrice,
            FinalPrice = finalPrice,
            IsInStock = isInStock,
            CategoryId = category.Url,
            CategoryName = category.Title,
            Provider = Providers.SpaceNet,   // enum already has SpaceNet
            Manufacturer = CleanText(manufacturer),
            Description = CleanText(description),
            ErpStock = CleanText(erpStock)
        };
    }

    private static string? GetNextPageUrl(HtmlDocument document)
    {
        // Pagination: <nav class="pagination"> -> <ul class="page-list"> -> <li><a class="next" ...>
        var nextLink = document.DocumentNode.SelectSingleNode(
            "//nav[contains(@class,'pagination')]//ul[contains(@class,'page-list')]//li//a[contains(@class,'next')][@href]");
        return nextLink?.GetAttributeValue("href", null);
    }

    private static decimal ParsePrice(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return 0;

        var normalized = HtmlEntity.DeEntitize(value)
            .Replace('\u00a0', ' ')
            .Replace(" ", string.Empty)
            .Replace("DT", string.Empty)
            .Trim();

        var match = Regex.Match(normalized, @"\d+(?:[.,]\d+)?");
        if (match.Success && decimal.TryParse(
            match.Value.Replace(',', '.'),
            NumberStyles.Number,
            CultureInfo.InvariantCulture,
            out var price))
        {
            return price;
        }
        return 0;
    }

    private static string ToAbsoluteUrl(string? url) =>
        string.IsNullOrWhiteSpace(url) ? string.Empty : new Uri(SpacenetBaseUri, url).AbsoluteUri;

    private static string CleanText(string? value) => HtmlEntity.DeEntitize(value ?? string.Empty).Trim();
}