using Hi_Trade.Models.DTOs;
using HtmlAgilityPack;
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

public sealed class MytekScraperService : IMytekScraperService, IWebScraper
{
    private const string ClientName = "Mytek";
    private const int MaxDegreeOfParallelism = 3;
    private static readonly Uri MytekBaseUri = new("https://www.mytek.tn/");
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<MytekScraperService> _logger;
    private readonly IProductIngestionService _ingestionService;

    public string ProviderName { get; } = "MyTek";

    public MytekScraperService(
        IHttpClientFactory httpClientFactory,
        ILogger<MytekScraperService> logger,
        IProductIngestionService ingestionService)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _ingestionService = ingestionService;
    }

    public async Task<int> ScrapeAsync(CancellationToken cancellationToken = default)
    {
        var session = await _ingestionService.StartScrapeSessionAsync("MYTEK", cancellationToken);
        var totalIngested = 0;

        try
        {
            var categories = (await GetCategoriesAsync(cancellationToken)).ToArray();
            await _ingestionService.StoreRetailerCategoriesAsync("MYTEK", categories, cancellationToken);

            var processedReferences = new ConcurrentDictionary<string, byte>(StringComparer.OrdinalIgnoreCase);

            await Parallel.ForEachAsync(
                categories.Where(category => !string.IsNullOrWhiteSpace(category.Url)),
                new ParallelOptions { MaxDegreeOfParallelism = MaxDegreeOfParallelism, CancellationToken = cancellationToken },
                async (category, ct) =>
                {
                    await ScrapeCategoryAsync(category, processedReferences, () => Interlocked.Increment(ref totalIngested), ct);
                });

            _logger.LogInformation("Mytek scraping completed. Ingested {ProductCount} unique products.", totalIngested);
            await _ingestionService.CompleteScrapeSessionAsync(session.Id, totalIngested, 0, 0, 0, null, cancellationToken);
            return totalIngested;
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Error scraping Mytek categories and products.");
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
            using var response = await client.GetAsync("https://mytek.tn", HttpCompletionOption.ResponseHeadersRead, cancellationToken);
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
            _logger.LogError(exception, "Could not retrieve the Mytek menu.");
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Could not parse the Mytek menu.");
            throw new InvalidOperationException("The Mytek menu could not be parsed.", exception);
        }
    }

    private static IReadOnlyList<CategoryDto> ParseCategories(string html)
    {
        var document = new HtmlDocument();
        document.LoadHtml(html);

        var categories = new List<CategoryDto>();
        var roots = document.DocumentNode.SelectNodes(
            "//li[contains(concat(' ', normalize-space(@class), ' '), ' rootverticalnav ')]")
            ?? Enumerable.Empty<HtmlNode>();

        foreach (var root in roots)
        {
            var parentCategory = CleanText(root.SelectSingleNode(
                ".//span[contains(concat(' ', normalize-space(@class), ' '), ' main-category-name ')]/em")?.InnerText);

            if (string.IsNullOrWhiteSpace(parentCategory))
            {
                continue;
            }

            AddLinkedItems(
                root.SelectNodes(".//div[contains(concat(' ', normalize-space(@class), ' '), ' title_normal ')]//a[@href]")
                    ?? Enumerable.Empty<HtmlNode>(),
                parentCategory,
                categories);

            var levelThreeItems = root.SelectNodes(
                ".//ul[contains(concat(' ', normalize-space(@class), ' '), ' level3-popup ')]//span[contains(concat(' ', normalize-space(@class), ' '), ' level3-name ')]")
                ?? Enumerable.Empty<HtmlNode>();

            foreach (var item in levelThreeItems)
            {
                var link = item.Ancestors("a").FirstOrDefault(node => node.Attributes["href"] is not null)
                    ?? item.SelectSingleNode("ancestor::li[1]//a[@href]");

                AddCategory(parentCategory, CleanText(item.InnerText), link?.GetAttributeValue("href", string.Empty), categories);
            }
        }

        return categories
            .DistinctBy(category => new { category.ParentCategory, category.Title, category.Url })
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
                        "Failed to fetch Mytek category page {PageUrl}. Status code: {StatusCode}",
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
                _logger.LogWarning(exception, "Failed to fetch Mytek category page {PageUrl}", pageUrl);
                return;
            }

            var document = new HtmlDocument();
            document.LoadHtml(html);

            foreach (var card in document.DocumentNode.SelectNodes("//div[@data-id]") ?? Enumerable.Empty<HtmlNode>())
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
        var productId = card.GetAttributeValue("data-id", string.Empty);
        var reference = card.GetAttributeValue("data-sku", string.Empty);

        var link = card.SelectSingleNode(".//a[@href]");

        reference = FirstNonEmpty(
            reference,
            card.GetAttributeValue("data-product-sku", string.Empty),
            link?.GetAttributeValue("data-product-sku", string.Empty));

        if (string.IsNullOrWhiteSpace(reference))
        {
            return null;
        }

        var title = FirstNonEmpty(
            card.GetAttributeValue("data-name", string.Empty),
            link?.InnerText);

        var productUrl = FirstNonEmpty(
            card.GetAttributeValue("data-url", string.Empty),
            link?.GetAttributeValue("href", string.Empty));

        var imageUrl = card.GetAttributeValue("data-image", string.Empty);
        var erpStock = card.GetAttributeValue("data-erpstock", string.Empty);
        var manufacturer = card.GetAttributeValue("data-manufacturer", string.Empty);
        var description = card.GetAttributeValue("data-description", string.Empty);

        var priceStr = card.GetAttributeValue("data-price", string.Empty);
        var finalPriceStr = card.GetAttributeValue("data-final-price", string.Empty);

        if (string.IsNullOrWhiteSpace(finalPriceStr))
        {
            var priceNode = card.SelectSingleNode(".//*[@data-price-amount]")
                ?? card.SelectSingleNode(".//*[@itemprop='price']");

            finalPriceStr = priceNode?.GetAttributeValue("data-price-amount", string.Empty);

            if (string.IsNullOrWhiteSpace(finalPriceStr))
            {
                finalPriceStr = priceNode?.InnerText;
            }
        }

        var stockClass = card.SelectSingleNode(
            ".//*[contains(concat(' ', normalize-space(@class), ' '), ' stock ')]")
            ?.GetAttributeValue("class", string.Empty) ?? string.Empty;

        var isInStock =
            !stockClass.Contains("unavailable", StringComparison.OrdinalIgnoreCase)
            && !stockClass.Contains("out-of-stock", StringComparison.OrdinalIgnoreCase);

        if (!string.IsNullOrWhiteSpace(erpStock))
        {
            isInStock = !erpStock.Equals("Rupture de stock", StringComparison.OrdinalIgnoreCase)
                && !erpStock.Equals("Out of stock", StringComparison.OrdinalIgnoreCase)
                && !erpStock.Equals("Indisponible", StringComparison.OrdinalIgnoreCase);
        }

        var regularPrice = ParsePrice(priceStr);
        var finalPrice = ParsePrice(finalPriceStr);

        return new ScrapedProductItem
        {
            RetailerCode = "MYTEK",
            RetailerProductId = CleanText(productId),
            RetailerSku = CleanText(reference),
            Title = CleanText(title),
            ProductUrl = ToAbsoluteUrl(productUrl),
            ImageUrl = ToAbsoluteUrl(imageUrl),
            RegularPrice = regularPrice > 0 ? regularPrice : finalPrice,
            FinalPrice = finalPrice,
            IsInStock = isInStock,
            StockStatusText = CleanText(erpStock),
            Manufacturer = CleanText(manufacturer),
            Description = CleanText(description),
            RawCategory = category.Title,
            CategoryUrl = category.Url
        };
    }

    private static string? GetNextPageUrl(HtmlDocument document)
    {
        var next = document.DocumentNode.SelectSingleNode(
            "//a[contains(concat(' ', normalize-space(@class), ' '), ' action ') and contains(concat(' ', normalize-space(@class), ' '), ' next ')][@href]");

        return next is null ? null : ToAbsoluteUrl(next.GetAttributeValue("href", string.Empty));
    }

    private static decimal ParsePrice(string? value)
    {
        var normalized = HtmlEntity.DeEntitize(value ?? string.Empty)
            .Replace('\u00a0', ' ')
            .Replace(" ", string.Empty);
        var match = Regex.Match(normalized, @"\d+(?:[.,]\d+)?");

        return match.Success && decimal.TryParse(
            match.Value.Replace(',', '.'),
            NumberStyles.Number,
            CultureInfo.InvariantCulture,
            out var price)
            ? price
            : 0;
    }

    private static string ToAbsoluteUrl(string? url) =>
        string.IsNullOrWhiteSpace(url) ? string.Empty : new Uri(MytekBaseUri, url).AbsoluteUri;

    private static string FirstNonEmpty(params string?[] values) =>
        values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value)) ?? string.Empty;

    private static void AddLinkedItems(
        IEnumerable<HtmlNode> links,
        string parentCategory,
        ICollection<CategoryDto> categories)
    {
        foreach (var link in links)
        {
            AddCategory(parentCategory, CleanText(link.InnerText), link.GetAttributeValue("href", string.Empty), categories);
        }
    }

    private static void AddCategory(string parentCategory, string title, string? url, ICollection<CategoryDto> categories)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            return;
        }

        categories.Add(new CategoryDto(
            parentCategory,
            title,
            string.IsNullOrWhiteSpace(url) ? string.Empty : new Uri(new Uri("https://www.mytek.tn/"), url).AbsoluteUri));
    }

    private static string CleanText(string? value) => HtmlEntity.DeEntitize(value ?? string.Empty).Trim();
}

