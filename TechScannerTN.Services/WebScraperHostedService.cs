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

        // Then schedule periodic runs using PeriodicTimer
        using var timer = new PeriodicTimer(_scrapeInterval);
        try
        {
            while (!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync(stoppingToken))
            {
                await PerformScrapeAsync(stoppingToken);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("WebScraperHostedService stopping due to cancellation.");
        }
    }

    private async Task PerformScrapeAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting scheduled multi-site product scraping in parallel");

        try
        {
            Type[] scraperTypes;
            using (var initScope = _serviceProvider.CreateScope())
            {
                scraperTypes = initScope.ServiceProvider
                    .GetRequiredService<IEnumerable<IWebScraper>>()
                    .Select(s => s.GetType())
                    .Distinct()
                    .ToArray();
            }

            var tasks = new List<Task>();

            foreach (var scraperType in scraperTypes)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }

                tasks.Add(RunScraperInScopeAsync(scraperType, cancellationToken));

                // Brief stagger between launching parallel retailer scraper jobs
                if (!cancellationToken.IsCancellationRequested)
                {
                    await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
                }
            }

            await Task.WhenAll(tasks);

            _logger.LogInformation("All scheduled retailer scrapes completed successfully");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogInformation("Product scraping cancelled.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fatal error in PerformScrapeAsync");
        }
    }

    private async Task RunScraperInScopeAsync(Type scraperType, CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var scraper = (IWebScraper)ActivatorUtilities.CreateInstance(scope.ServiceProvider, scraperType);
        await ScrapeAsync(scraper, cancellationToken);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("WebScraperHostedService stopping");
        await base.StopAsync(cancellationToken);
    }

    public async Task ScrapeAsync(IWebScraper? scraper, CancellationToken ct)
    {
        if (scraper is null || ct.IsCancellationRequested)
        {
            return;
        }

        _logger.LogInformation("Beginning scrape for {ProviderName}", scraper.ProviderName);

        try
        {
            var count = await scraper.ScrapeAsync(ct);
            _logger.LogInformation("Completed scrape for {ProviderName}: {Count} products ingested/updated.", scraper.ProviderName, count);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            _logger.LogInformation("Scraping cancelled for {ProviderName}", scraper.ProviderName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error scraping {ProviderName}", scraper.ProviderName);
        }
    }
}
