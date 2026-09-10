using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

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

        // Run scraper on startup
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
        _logger.LogInformation("Starting scheduled multi-site product scraping");

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var scrapers = scope.ServiceProvider.GetRequiredService<IEnumerable<IWebScraper>>();

            foreach (var scraper in scrapers)
            {
                _ = ScrapeAsync(scraper, cancellationToken);
                // Friendly delay between retailer scrapes
                await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
            }

            _logger.LogInformation("All scheduled retailer scrapes completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fatal error in PerformScrapeAsync");
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
    public async Task ScrapeAsync(IWebScraper? scraper, CancellationToken ct)
    {
        if (ct.IsCancellationRequested)
        {
            return;
        }

        _logger.LogInformation("Beginning scrape for {ProviderName}", scraper.ProviderName);


        try
        {
            var count = await scraper.ScrapeAsync(ct);
            _logger.LogInformation("Completed scrape for {ProviderName}: {Count} products ingested/updated.", scraper.ProviderName, count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error scraping {ProviderName}", scraper.ProviderName);
        }
    }
}

