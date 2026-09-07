using System.Threading;
using System.Threading.Tasks;

namespace Hi_Trade.Services;

public interface IWebScraper
{
    string ProviderName { get; }

    Task<int> ScrapeAsync(CancellationToken cancellationToken = default);
}

