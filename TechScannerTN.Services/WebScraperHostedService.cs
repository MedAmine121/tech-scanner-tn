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

                    if (scraper.ProviderName == "Mytek")
                    {
                        _logger.LogInformation("Mytek categories were scraped and stored.");
                    }
                    else if (plans.Count > 0)
                    {
                        await StorePlansAsync(context, scraper.ProviderName, plans);
                    }
                    else
                    {
                        _logger.LogWarning("No plans found for {ProviderName}", scraper.ProviderName);
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

    private async Task StorePlansAsync(TechScannerContext context, string providerName, List<Plan> plans)
    {
        try
        {
            // Get or create provider
            var provider = await context.InternetProviders
                .FirstOrDefaultAsync(p => p.Name == providerName);

            if (provider == null)
            {
                provider = new InternetProvider
                {
                    Name = providerName,
                    Website = GetProviderWebsite(providerName),
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                context.InternetProviders.Add(provider);
                await context.SaveChangesAsync();
                _logger.LogInformation("Created new provider: {ProviderName}", providerName);
            }

            // Remove old plans for this provider
            var oldPlans = context.Plans.Where(p => p.InternetProviderId == provider.Id).ToList();
            if (oldPlans.Count > 0)
            {
                context.Plans.RemoveRange(oldPlans);
                _logger.LogInformation("Removed {OldPlanCount} old plans for {ProviderName}", oldPlans.Count, providerName);
            }

            // Add new plans
            foreach (var plan in plans)
            {
                plan.InternetProviderId = provider.Id;
                plan.ScrapedAt = DateTime.UtcNow;
                context.Plans.Add(plan);
            }

            // Update provider timestamp
            provider.UpdatedAt = DateTime.UtcNow;
            await context.SaveChangesAsync();

            _logger.LogInformation("Stored {PlanCount} plans for {ProviderName}", plans.Count, providerName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error storing plans for {ProviderName}", providerName);
            throw;
        }
    }

    private string GetProviderWebsite(string providerName)
    {
        return providerName switch
        {
            "MyTek" => "https://www.mytek.tn",
            "TunisiaNet" => "https://www.tunisianet.tn",
            "SpaceNet" => "https://www.spacenet.tn",
            _ => string.Empty
        };
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
