using HtmlAgilityPack;
using Hi_Trade.Models;
using Microsoft.Extensions.Logging;

namespace Hi_Trade.Services;

public class TunisiaNetScraper : IWebScraper
{
    private readonly ILogger<TunisiaNetScraper> _logger;
    private readonly HttpClient _httpClient;

    public string ProviderName => "TunisiaNet";

    public TunisiaNetScraper(ILogger<TunisiaNetScraper> logger)
    {
        _logger = logger;
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
    }

    public async Task<List<Plan>> ScrapeAsync()
    {
        var plans = new List<Plan>();

        try
        {
            _logger.LogInformation("Starting TunisiaNet scraping");
            
            var response = await _httpClient.GetAsync("https://www.tunisianet.tn");
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to fetch TunisiaNet page. Status code: {StatusCode}", response.StatusCode);
                return plans;
            }

            var html = await response.Content.ReadAsStringAsync();
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            // Extract plans using CSS selectors - adjust selectors based on actual HTML structure
            var planNodes = doc.DocumentNode.SelectNodes("//div[@class='plan'] | //div[contains(@class, 'package')] | //div[contains(@class, 'offer')]");

            if (planNodes == null || planNodes.Count == 0)
            {
                _logger.LogWarning("No plan nodes found on TunisiaNet page");
                return plans;
            }

            foreach (var node in planNodes)
            {
                try
                {
                    var plan = ExtractPlanFromNode(node);
                    if (plan != null)
                    {
                        plans.Add(plan);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error extracting plan from TunisiaNet node");
                }
            }

            _logger.LogInformation("TunisiaNet scraping completed. Found {PlanCount} plans", plans.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error scraping TunisiaNet");
        }

        return plans;
    }

    private Plan? ExtractPlanFromNode(HtmlNode node)
    {
        var plan = new Plan
        {
            Name = node.SelectSingleNode(".//h3 | .//h2 | .//div[@class='plan-name']")?.InnerText?.Trim() ?? "Unknown",
            Speed = ParseSpeed(node.SelectSingleNode(".//div[@class='speed'] | .//span[contains(@class, 'speed')]")?.InnerText ?? "0"),
            Price = ParsePrice(node.SelectSingleNode(".//div[@class='price'] | .//span[contains(@class, 'price')]")?.InnerText ?? "0"),
            Description = node.SelectSingleNode(".//div[@class='description'] | .//p")?.InnerText?.Trim(),
            DataLimit = node.SelectSingleNode(".//div[@class='data-limit'] | .//span[contains(@class, 'data')]")?.InnerText?.Trim(),
            Currency = "TND",
            SpeedUnit = "Mbps",
            ScrapedAt = DateTime.UtcNow
        };

        return !string.IsNullOrEmpty(plan.Name) && plan.Speed > 0 ? plan : null;
    }

    private double ParseSpeed(string speedText)
    {
        var match = System.Text.RegularExpressions.Regex.Match(speedText, @"(\d+(?:,\d+)?)");
        if (match.Success && double.TryParse(match.Groups[1].Value.Replace(",", "."), out var speed))
        {
            return speed;
        }
        return 0;
    }

    private decimal ParsePrice(string priceText)
    {
        var match = System.Text.RegularExpressions.Regex.Match(priceText, @"(\d+(?:[.,]\d+)?)");
        if (match.Success && decimal.TryParse(match.Groups[1].Value.Replace(",", "."), out var price))
        {
            return price;
        }
        return 0;
    }
}
