using HtmlAgilityPack;
using Hi_Trade.Models.DTOs;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Hi_Trade.Services;

public sealed class SpacenetScraperService : IWebScraper
{
    private const string ClientName = "Spacenet";
    private const int MaxDegreeOfParallelism = 3;
    private static readonly Uri SpacenetBaseUri = new("https://spacenet.tn/");

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<SpacenetScraperService> _logger;
    private readonly IProductIngestionService _ingestionService;

    public string ProviderName { get; } = "SpaceNet";

    public SpacenetScraperService(
        IHttpClientFactory httpClientFactory,
        ILogger<SpacenetScraperService> logger,
        IProductIngestionService ingestionService)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _ingestionService = ingestionService;
    }

    public async Task<int> ScrapeAsync(CancellationToken cancellationToken = default)
    {
        var session = await _ingestionService.StartScrapeSessionAsync("SPACENET", cancellationToken);
        var totalIngested = 0;

        try
        {
            var categories = (await GetCategoriesAsync(cancellationToken)).ToArray();
            await _ingestionService.StoreRetailerCategoriesAsync("SPACENET", categories, cancellationToken);

            var processedReferences = new ConcurrentDictionary<string, byte>(StringComparer.OrdinalIgnoreCase);

            await Parallel.ForEachAsync(
                categories.Where(c => !string.IsNullOrWhiteSpace(c.Url)),
                new ParallelOptions { MaxDegreeOfParallelism = MaxDegreeOfParallelism, CancellationToken = cancellationToken },
                async (category, ct) =>
                {
                    await ScrapeCategoryAsync(category, processedReferences, () => Interlocked.Increment(ref totalIngested), ct);
                });

            _logger.LogInformation("Spacenet scraping completed. Ingested {ProductCount} unique products.", totalIngested);
            await _ingestionService.CompleteScrapeSessionAsync(session.Id, totalIngested, 0, 0, 0, null, cancellationToken);
            return totalIngested;
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Error scraping Spacenet categories and products.");
            await _ingestionService.CompleteScrapeSessionAsync(session.Id, totalIngested, 0, 0, 0, exception.Message, cancellationToken);
            return totalIngested;
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
        var menuNode = document.DocumentNode.SelectSingleNode("//div[@id='sp-vermegamenu']");
        if (menuNode == null)
            return categories;

        var topLevelItems = menuNode.SelectNodes(".//li[contains(@class,'item-1') and contains(@class,'vertical-cat') and contains(@class,'parent')]")
            ?? Enumerable.Empty<HtmlNode>();

        foreach (var topItem in topLevelItems)
        {
            var parentLink = topItem.SelectSingleNode(".//a[contains(@class,'item-1')]");
            if (parentLink == null)
                continue;

            var parentCategory = CleanText(parentLink.InnerText);
            var parentUrl = parentLink.GetAttributeValue("href", string.Empty);

            if (!string.IsNullOrWhiteSpace(parentUrl))
            {
                categories.Add(new CategoryDto(
                    string.Empty,
                    parentCategory,
                    ToAbsoluteUrl(parentUrl)
                ));
            }

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

    private async Task ScrapeCategoryAsync(
        CategoryDto category,
        ConcurrentDictionary<string, byte> processedReferences,
        Action onProductIngested,
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

            var productNodes = document.DocumentNode.SelectNodes(
                "//div[contains(@class,'field-product-item') and contains(@class,'product-miniature')]")
                ?? Enumerable.Empty<HtmlNode>();

            foreach (var card in productNodes)
            {
                var item = ParseProduct(card, category);
                if (item is not null && processedReferences.TryAdd(item.RetailerSku, 0))
                {
                    await _ingestionService.IngestProductAsync(item, cancellationToken);
                    onProductIngested();
                }
            }

            pageUrl = GetNextPageUrl(document);
        }
    }

    private static ScrapedProductItem? ParseProduct(HtmlNode card, CategoryDto category)
    {
        var productId = card.GetAttributeValue("data-id-product", string.Empty);
        var referenceNode = card.SelectSingleNode(".//div[contains(@class,'product-reference')]//span");
        var reference = referenceNode?.InnerText?.Trim() ?? string.Empty;

        var titleNode = card.SelectSingleNode(".//h2[contains(@class,'product_name')]//a");
        var title = CleanText(titleNode?.InnerText);
        var productUrl = titleNode?.GetAttributeValue("href", string.Empty) ?? string.Empty;

        var imageNode = card.SelectSingleNode(".//a[contains(@class,'thumbnail')]//span[contains(@class,'cover_image')]//img");
        var imageUrl = imageNode?.GetAttributeValue("src", string.Empty) ?? string.Empty;

        var priceSpan = card.SelectSingleNode(".//div[contains(@class,'product-price-and-shipping')]//span[contains(@class,'price')]");
        var regularPriceSpan = card.SelectSingleNode(".//div[contains(@class,'product-price-and-shipping')]//span[contains(@class,'regular-price')]");

        decimal price = ParsePrice(priceSpan?.InnerText);
        decimal regularPrice = ParsePrice(regularPriceSpan?.InnerText);
        decimal finalPrice = price;
        if (regularPrice <= 0 || regularPrice < price)
        {
            regularPrice = price;
        }

        bool isInStock = true;
        var stockLabel = card.SelectSingleNode(".//div[contains(@class,'product-quantities')]//label");
        if (stockLabel != null)
        {
            string stockText = CleanText(stockLabel.InnerText);
            isInStock = stockText.Contains("En stock", StringComparison.OrdinalIgnoreCase);
        }

        var manufacturerNode = card.SelectSingleNode(".//div[contains(@class,'product-manufacturer')]//img");
        var manufacturer = manufacturerNode?.GetAttributeValue("alt", string.Empty) ?? string.Empty;

        var descriptionNode = card.SelectSingleNode(".//div[contains(@class,'decriptions-short')]");
        var description = CleanText(descriptionNode?.InnerText);

        if (string.IsNullOrWhiteSpace(reference))
        {
            reference = productId;
        }

        if (string.IsNullOrWhiteSpace(reference) || string.IsNullOrWhiteSpace(title))
            return null;

        return new ScrapedProductItem
        {
            RetailerCode = "SPACENET",
            RetailerProductId = CleanText(productId),
            RetailerSku = CleanText(reference),
            Title = CleanText(title),
            ProductUrl = ToAbsoluteUrl(productUrl),
            ImageUrl = ToAbsoluteUrl(imageUrl),
            RegularPrice = regularPrice,
            FinalPrice = finalPrice,
            IsInStock = isInStock,
            StockStatusText = isInStock ? "En stock" : "Hors stock",
            Manufacturer = CleanText(manufacturer),
            Description = CleanText(description),
            RawCategory = category.Title,
            CategoryUrl = category.Url
        };
    }

    private static string? GetNextPageUrl(HtmlDocument document)
    {
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
            .Replace("DT", string.Empty, StringComparison.OrdinalIgnoreCase)
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