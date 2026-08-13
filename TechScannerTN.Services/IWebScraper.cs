using Hi_Trade.Models;

namespace Hi_Trade.Services;

public interface IWebScraper
{
    string ProviderName { get; }

    Task<List<Plan>> ScrapeAsync();
}
