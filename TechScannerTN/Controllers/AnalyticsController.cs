using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Hi_Trade.DAL;
using Hi_Trade.Models;
using Hi_Trade.Models.DTOs;
using Hi_Trade.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Hi_Trade.Controllers;

[ApiController]
[Route("api/analytics")]
public class AnalyticsController : ControllerBase
{
    private readonly IProductCatalogService _catalogService;
    private readonly TechScannerContext _context;

    public AnalyticsController(
        IProductCatalogService catalogService,
        TechScannerContext context)
    {
        _catalogService = catalogService;
        _context = context;
    }

    /// <summary>
    /// Returns market overview metrics, price changes in the last 24h, and retailer price competitiveness comparisons.
    /// </summary>
    [HttpGet("overview")]
    [ProducesResponseType<MarketOverviewDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<MarketOverviewDto>> GetMarketOverview(CancellationToken cancellationToken)
    {
        var overview = await _catalogService.GetMarketOverviewAsync(cancellationToken);
        return Ok(overview);
    }

    /// <summary>
    /// Returns recent scrape runs, duration, items ingested, and error logs for all retailer scrapers.
    /// </summary>
    [HttpGet("scrapers")]
    [ProducesResponseType<IEnumerable<object>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<object>>> GetScrapeSessions(
        [FromQuery] int limit = 50,
        CancellationToken cancellationToken = default)
    {
        var sessions = await _context.ScrapeSessions
            .AsNoTracking()
            .Include(s => s.Retailer)
            .OrderByDescending(s => s.StartedAt)
            .Take(Math.Clamp(limit, 1, 100))
            .Select(s => new
            {
                s.Id,
                RetailerCode = s.Retailer.Code,
                RetailerName = s.Retailer.Name,
                s.StartedAt,
                s.FinishedAt,
                DurationSeconds = s.FinishedAt.HasValue ? (int?)(s.FinishedAt.Value - s.StartedAt).TotalSeconds : null,
                Status = s.Status.ToString(),
                s.ItemsScraped,
                s.ItemsUpdated,
                s.NewItemsCount,
                s.PriceChangesCount,
                s.ErrorMessage
            })
            .ToListAsync(cancellationToken);

        return Ok(sessions);
    }
}

