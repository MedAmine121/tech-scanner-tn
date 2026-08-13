using HtmlAgilityPack;
using Hi_Trade.DAL;
using Hi_Trade.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Net.Http;

namespace Hi_Trade.Services;

public sealed class MytekScraperService : IMytekScraperService, IWebScraper
{
    private const string ClientName = "Mytek";
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<MytekScraperService> _logger;
    private readonly TechScannerContext _context;

    public MytekScraperService(
        IHttpClientFactory httpClientFactory,
        ILogger<MytekScraperService> logger,
        TechScannerContext context)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _context = context;
    }

    public string ProviderName => "Mytek";

    public async Task<List<Plan>> ScrapeAsync()
    {
        try
        {
            var categories = await GetCategoriesAsync();
            await StoreCategoriesAsync(categories);
            return [];
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Error scraping and storing Mytek categories.");
            return [];
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

    private async Task StoreCategoriesAsync(IEnumerable<CategoryDto> categories)
    {
        var now = DateTime.UtcNow;

        foreach (var category in categories.Where(category => !string.IsNullOrWhiteSpace(category.Url)))
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
