using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Hi_Trade.DAL;
using Hi_Trade.Models;

namespace Hi_Trade.Services;

public class WebScraperHostedService : BackgroundService
{
    private readonly ILogger<WebScraperHostedService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private Timer? _timer;
    private readonly TimeSpan _scrapeInterval;

    public WebScraperHostedService(
        ILogger<WebScraperHostedService> logger,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _scrapeInterval = TimeSpan.FromHours(6);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("WebScraperHostedService starting");

        // Run scraper immediately on startup
        await PerformScrapeAsync(stoppingToken);

        // Then schedule periodic runs
        _timer = new Timer(
            callback: async _ => await PerformScrapeAsync(stoppingToken),
            state: null,
            dueTime: _scrapeInterval,
            period: _scrapeInterval);

        await Task.CompletedTask;
    }

    private async Task PerformScrapeAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting scheduled web scraping");

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<TechScannerContext>();
            var scrapers = scope.ServiceProvider.GetRequiredService<IEnumerable<IWebScraper>>();
            foreach (var scraper in scrapers)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }

                _logger.LogInformation("Scraping {ProviderName}", scraper.ProviderName);

                try
                {
                    var plans = await scraper.ScrapeAsync();

                    if (scraper.ProviderName is "Mytek" or "TunisiaNet")
                    {
                        _logger.LogInformation("{ProviderName} categories were scraped and stored.", scraper.ProviderName);
                    }
                    else
                    {
                        _logger.LogError("No plans found for {ProviderName}", scraper.ProviderName);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error scraping {ProviderName}", scraper.ProviderName);
                }

                // Small delay between provider scrapes
                await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
            }

            _logger.LogInformation("Scheduled web scraping completed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in PerformScrapeAsync");
        }
    }
    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("WebScraperHostedService stopping");
        _timer?.Dispose();
        await base.StopAsync(cancellationToken);
    }

    public override void Dispose()
    {
        _timer?.Dispose();
        base.Dispose();
    }
}
