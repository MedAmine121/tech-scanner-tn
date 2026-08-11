using Microsoft.EntityFrameworkCore;
using Hi_Trade.DAL;

namespace Hi_Trade.Endpoints;

public static class InternetProviderEndpoints
{
    public static void MapInternetProviderEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/providers").WithOpenApi();

        group.MapGet("/", GetAllProviders)
            .WithName("GetAllProviders")
            .WithDescription("Get all internet providers with their plans");

        group.MapGet("/{id}", GetProviderById)
            .WithName("GetProviderById")
            .WithDescription("Get a specific provider with its plans");

        group.MapGet("/{id}/plans", GetProviderPlans)
            .WithName("GetProviderPlans")
            .WithDescription("Get all plans for a specific provider");
    }

    private static async Task<IResult> GetAllProviders(TechScannerContext context)
    {
        var providers = await context.InternetProviders
            .Include(p => p.Plans)
            .OrderBy(p => p.Name)
            .ToListAsync();

        return Results.Ok(new
        {
            count = providers.Count,
            providers = providers.Select(p => new
            {
                p.Id,
                p.Name,
                p.Website,
                planCount = p.Plans.Count,
                lastUpdated = p.UpdatedAt,
                p.CreatedAt
            })
        });
    }

    private static async Task<IResult> GetProviderById(int id, TechScannerContext context)
    {
        var provider = await context.InternetProviders
            .Include(p => p.Plans)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (provider == null)
            return Results.NotFound();

        return Results.Ok(new
        {
            provider.Id,
            provider.Name,
            provider.Website,
            planCount = provider.Plans.Count,
            lastUpdated = provider.UpdatedAt,
            provider.CreatedAt,
            plans = provider.Plans.Select(pl => new
            {
                pl.Id,
                pl.Name,
                pl.Speed,
                pl.SpeedUnit,
                pl.Price,
                pl.Currency,
                pl.Description,
                pl.DataLimit,
                pl.ScrapedAt
            })
        });
    }

    private static async Task<IResult> GetProviderPlans(int id, TechScannerContext context)
    {
        var plans = await context.Plans
            .Where(p => p.InternetProviderId == id)
            .OrderBy(p => p.Price)
            .ToListAsync();

        if (plans.Count == 0)
            return Results.NotFound("No plans found for this provider");

        return Results.Ok(new
        {
            count = plans.Count,
            plans = plans.Select(p => new
            {
                p.Id,
                p.Name,
                p.Speed,
                p.SpeedUnit,
                p.Price,
                p.Currency,
                p.Description,
                p.DataLimit,
                p.ScrapedAt
            })
        });
    }
}
