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

public sealed class TunisiaNetScraper : IWebScraper
{
    private const string ClientName = "TunisiaNet";
    private const int MaxDegreeOfParallelism = 3;
    private static readonly Uri TunisiaNetBaseUri = new("https://www.tunisianet.com.tn/");
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<TunisiaNetScraper> _logger;
    private readonly IProductIngestionService _ingestionService;

    public TunisiaNetScraper(
        IHttpClientFactory httpClientFactory,
        ILogger<TunisiaNetScraper> logger,
        IProductIngestionService ingestionService)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _ingestionService = ingestionService;
    }

    public string ProviderName => "TunisiaNet";

    public async Task<int> ScrapeAsync(CancellationToken cancellationToken = default)
    {
        var session = await _ingestionService.StartScrapeSessionAsync("TUNISIANET", cancellationToken);
        var totalIngested = 0;

        try
        {
            var categories = (await GetCategoriesAsync(cancellationToken)).ToArray();
            await _ingestionService.StoreRetailerCategoriesAsync("TUNISIANET", categories, cancellationToken);

            var processedReferences = new ConcurrentDictionary<string, byte>(StringComparer.OrdinalIgnoreCase);

            await Parallel.ForEachAsync(
                categories,
                new ParallelOptions { MaxDegreeOfParallelism = MaxDegreeOfParallelism, CancellationToken = cancellationToken },
                async (category, ct) =>
                {
                    await ScrapeCategoryAsync(category, processedReferences, () => Interlocked.Increment(ref totalIngested), ct);
                });

            _logger.LogInformation(
                "TunisiaNet scraping completed. Stored {CategoryCount} categories and ingested {ProductCount} unique products.",
                categories.Length,
                totalIngested);

            await _ingestionService.CompleteScrapeSessionAsync(session.Id, totalIngested, 0, 0, 0, null, cancellationToken);
            return totalIngested;
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Error scraping and storing TunisiaNet products.");
            await _ingestionService.CompleteScrapeSessionAsync(session.Id, totalIngested, 0, 0, 0, exception.Message, cancellationToken);
            return totalIngested;
        }
    }

    public async Task<IEnumerable<CategoryDto>> GetCategoriesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var client = _httpClientFactory.CreateClient(ClientName);
            using var response = await client.GetAsync("/", HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            response.EnsureSuccessStatusCode();

            var html = await response.Content.ReadAsStringAsync(cancellationToken);
            return ParseCategories(html);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Could not retrieve or parse the TunisiaNet menu.");
            throw;
        }
    }

    private static IReadOnlyList<CategoryDto> ParseCategories(string html)
    {
        var document = new HtmlDocument();
        document.LoadHtml(html);

        var menu = document.DocumentNode.SelectSingleNode(
            "//div[contains(concat(' ', normalize-space(@class), ' '), ' menu-vertical ')]");
        if (menu is null)
        {
            return [];
        }

        var categories = new List<CategoryDto>();
        var topLevelItems = menu.SelectNodes(
            "./ul[contains(concat(' ', normalize-space(@class), ' '), ' menu-content ')]/li[contains(concat(' ', normalize-space(@class), ' '), ' level-1 ')]")
            ?? Enumerable.Empty<HtmlNode>();

        foreach (var item in topLevelItems)
        {
            var menuLabel = CleanText(item.SelectSingleNode(
                "./div[contains(concat(' ', normalize-space(@class), ' '), ' icon-drop-mobile ')]")?.InnerText);
            if (string.IsNullOrWhiteSpace(menuLabel))
            {
                continue;
            }

            var childLinks = item.SelectNodes(
                "./div[contains(concat(' ', normalize-space(@class), ' '), ' wb-sub-menu ')]//a[@href]")
                ?? Enumerable.Empty<HtmlNode>();
            var foundChild = false;

            foreach (var childLink in childLinks)
            {
                foundChild |= AddCategory(menuLabel, CleanText(childLink.InnerText), childLink.GetAttributeValue("href", string.Empty), categories);
            }

            if (!foundChild)
            {
                var standaloneLink = item.SelectSingleNode(
                    "./div[contains(concat(' ', normalize-space(@class), ' '), ' icon-drop-mobile ')]//a[@href]");
                AddCategory(menuLabel, menuLabel, standaloneLink?.GetAttributeValue("href", string.Empty), categories);
            }
        }

        return categories
            .DistinctBy(category => category.Url, StringComparer.OrdinalIgnoreCase)
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
            string html;
            try
            {
                using var response = await client.GetAsync(pageUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning(
                        "Failed to fetch TunisiaNet category page {PageUrl}. Status code: {StatusCode}",
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
                _logger.LogWarning(exception, "Failed to fetch TunisiaNet category page {PageUrl}", pageUrl);
                return;
            }

            var document = new HtmlDocument();
            document.LoadHtml(html);

            var productCards = document.DocumentNode.SelectNodes(
                "//div[contains(concat(' ', normalize-space(@class), ' '), ' products ') and " +
                "contains(concat(' ', normalize-space(@class), ' '), ' wb-product-list ')]" +
                "//div[contains(concat(' ', normalize-space(@class), ' '), ' item-product ')]")
                ?? Enumerable.Empty<HtmlNode>();

            foreach (var card in productCards)
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
        var article = card.SelectSingleNode(".//article[contains(concat(' ', normalize-space(@class), ' '), ' product-miniature ')]");
        var productId = article?.GetAttributeValue("data-id-product", string.Empty) ?? string.Empty;
        var reference = CleanText(card.SelectSingleNode(".//*[contains(concat(' ', normalize-space(@class), ' '), ' product-reference ')]")?.InnerText)
            .Trim('[', ']');
        var titleLink = card.SelectSingleNode(
            ".//h2[contains(concat(' ', normalize-space(@class), ' '), ' product-title ')]//a[@href]");
        var image = card.SelectSingleNode(
            ".//a[contains(concat(' ', normalize-space(@class), ' '), ' product-thumbnail ')]//img[contains(concat(' ', normalize-space(@class), ' '), ' thumbnail-img ')]");

        if (string.IsNullOrWhiteSpace(productId) || string.IsNullOrWhiteSpace(reference) || titleLink is null)
        {
            return null;
        }

        var priceNode = card.SelectSingleNode(".//span[contains(concat(' ', normalize-space(@class), ' '), ' price ')]");
        var regularPriceNode = card.SelectSingleNode(".//span[contains(concat(' ', normalize-space(@class), ' '), ' regular-price ')]");

        var description = CleanText(card.SelectSingleNode(
            ".//*[contains(concat(' ', normalize-space(@class), ' '), ' descrip ')]")?.InnerText);
        var manufacturer = CleanText(card.SelectSingleNode(
            ".//*[contains(concat(' ', normalize-space(@class), ' '), ' manufacturer-logo ')]")?.GetAttributeValue("alt", string.Empty));

        var finalPrice = ParsePrice(priceNode?.InnerText);
        var regularPrice = regularPriceNode != null ? ParsePrice(regularPriceNode.InnerText) : finalPrice;
        if (regularPrice < finalPrice) regularPrice = finalPrice;

        var isInStock = card.SelectSingleNode(".//*[contains(concat(' ', normalize-space(@class), ' '), ' in-stock ')]") is not null;

        return new ScrapedProductItem
        {
            RetailerCode = "TUNISIANET",
            RetailerProductId = productId,
            RetailerSku = reference,
            Title = CleanText(titleLink.InnerText),
            ProductUrl = ToAbsoluteUrl(titleLink.GetAttributeValue("href", string.Empty)),
            ImageUrl = ToAbsoluteUrl(image?.GetAttributeValue("src", string.Empty)),
            RegularPrice = regularPrice,
            FinalPrice = finalPrice,
            IsInStock = isInStock,
            StockStatusText = isInStock ? "En stock" : "Hors stock",
            Manufacturer = manufacturer,
            Description = description,
            RawCategory = category.Title,
            CategoryUrl = category.Url
        };
    }

    private static string? GetNextPageUrl(HtmlDocument document)
    {
        var nextLink = document.DocumentNode.SelectSingleNode(
            "//*[contains(concat(' ', normalize-space(@class), ' '), ' pagination ')]//a[@rel='next' and @href]");
        return nextLink is null ? null : ToAbsoluteUrl(nextLink.GetAttributeValue("href", string.Empty));
    }

    private static decimal ParsePrice(string? value)
    {
        var normalized = HtmlEntity.DeEntitize(value ?? string.Empty)
            .Replace('\u00a0', ' ')
            .Replace('\u202f', ' ')
            .Replace(" ", string.Empty)
            .Replace("DT", string.Empty, StringComparison.OrdinalIgnoreCase);
        var match = Regex.Match(normalized, @"\d+(?:[.,]\d+)?");

        return match.Success && decimal.TryParse(
            match.Value.Replace(',', '.'),
            NumberStyles.Number,
            CultureInfo.InvariantCulture,
            out var price)
            ? price
            : 0;
    }

    private static bool AddCategory(string parentCategory, string title, string? url, ICollection<CategoryDto> categories)
    {
        if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(url)
            || url.StartsWith("javascript:", StringComparison.OrdinalIgnoreCase)
            || url.StartsWith('#'))
        {
            return false;
        }

        categories.Add(new CategoryDto(parentCategory, title, new Uri(TunisiaNetBaseUri, url).AbsoluteUri));
        return true;
    }

    private static string ToAbsoluteUrl(string? url) =>
        string.IsNullOrWhiteSpace(url) ? string.Empty : new Uri(TunisiaNetBaseUri, url).AbsoluteUri;

    private static string CleanText(string? value) => HtmlEntity.DeEntitize(value ?? string.Empty).Trim();
}

