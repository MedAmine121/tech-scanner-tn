using Hi_Trade.Services;
using Hi_Trade.Services.Interfaces;
using Hi_Trade.DAL;
using Hi_Trade.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Hi_Trade.Controllers;

[ApiController]
[Route("api/categories")]
public sealed class CategoriesController : ControllerBase
{
    private const string CategoriesCacheScope = "categories";
    private static readonly TimeSpan CategoriesCacheTtl = TimeSpan.FromMinutes(60);

    private readonly TechScannerContext _context;
    private readonly ICacheService? _cacheService;

    public CategoriesController(TechScannerContext context, ICacheService? cacheService = null)
    {
        _context = context;
        _cacheService = cacheService;
    }

    /// <summary>
    /// Backward-compatible endpoint to get MyTek categories.
    /// </summary>
    [HttpGet("mytek")]
    [ProducesResponseType<IEnumerable<CategoryDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<CategoryDto>>> GetMytekCategories(CancellationToken cancellationToken)
    {
        var categories = await GetRetailerCategoriesByCodeAsync("MYTEK", cancellationToken);
        return Ok(categories);
    }

    /// <summary>
    /// Gets categories for any retailer by code (e.g. MYTEK, TUNISIANET, SPACENET).
    /// </summary>
    [HttpGet("retailer/{retailerCode}")]
    [ProducesResponseType<IEnumerable<CategoryDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<CategoryDto>>> GetRetailerCategories(string retailerCode, CancellationToken cancellationToken)
    {
        var categories = await GetRetailerCategoriesByCodeAsync(retailerCode, cancellationToken);
        return Ok(categories);
    }

    /// <summary>
    /// Gets the unified platform categories tree.
    /// </summary>
    [HttpGet]
    [ProducesResponseType<IEnumerable<Category>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<Category>>> GetUnifiedCategories(CancellationToken cancellationToken)
    {
        if (_cacheService == null)
        {
            return Ok(await FetchUnifiedCategoriesAsync(cancellationToken));
        }

        var version = await _cacheService.GetVersionTokenAsync(CategoriesCacheScope, cancellationToken);
        var cacheKey = $"categories:v={version}:unified";

        var categories = await _cacheService.GetOrSetAsync(
            cacheKey,
            FetchUnifiedCategoriesAsync,
            CategoriesCacheTtl,
            cancellationToken);

        return Ok(categories);
    }

    private async Task<List<CategoryDto>> GetRetailerCategoriesByCodeAsync(
        string retailerCode,
        CancellationToken cancellationToken)
    {
        var code = retailerCode.ToUpperInvariant();
        if (_cacheService == null)
        {
            return await FetchRetailerCategoriesAsync(code, cancellationToken);
        }

        var version = await _cacheService.GetVersionTokenAsync(CategoriesCacheScope, cancellationToken);
        var cacheKey = $"categories:v={version}:retailer:{code}";

        return await _cacheService.GetOrSetAsync(
            cacheKey,
            ct => FetchRetailerCategoriesAsync(code, ct),
            CategoriesCacheTtl,
            cancellationToken);
    }

    private Task<List<CategoryDto>> FetchRetailerCategoriesAsync(
        string normalizedCode,
        CancellationToken cancellationToken)
    {
        return _context.RetailerCategories
            .AsNoTracking()
            .Where(rc => rc.Retailer.Code == normalizedCode)
            .OrderBy(rc => rc.ParentCategoryName)
            .ThenBy(rc => rc.Title)
            .Select(rc => new CategoryDto(rc.ParentCategoryName, rc.Title, rc.Url))
            .ToListAsync(cancellationToken);
    }

    private Task<List<Category>> FetchUnifiedCategoriesAsync(CancellationToken cancellationToken)
    {
        return _context.Categories
            .AsNoTracking()
            .Where(c => c.ParentCategoryId == null)
            .Include(c => c.SubCategories)
            .ToListAsync(cancellationToken);
    }
}

