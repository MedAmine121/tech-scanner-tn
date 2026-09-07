using Hi_Trade.Services;
using Hi_Trade.DAL;
using Hi_Trade.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Hi_Trade.Controllers;

[ApiController]
[Route("api/categories")]
public sealed class CategoriesController : ControllerBase
{
    private readonly TechScannerContext _context;

    public CategoriesController(TechScannerContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Backward-compatible endpoint to get MyTek categories.
    /// </summary>
    [HttpGet("mytek")]
    [ProducesResponseType<IEnumerable<CategoryDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<CategoryDto>>> GetMytekCategories(CancellationToken cancellationToken)
    {
        var categories = await _context.RetailerCategories
            .AsNoTracking()
            .Where(rc => rc.Retailer.Code == "MYTEK")
            .OrderBy(rc => rc.ParentCategoryName)
            .ThenBy(rc => rc.Title)
            .Select(rc => new CategoryDto(rc.ParentCategoryName, rc.Title, rc.Url))
            .ToListAsync(cancellationToken);

        return Ok(categories);
    }

    /// <summary>
    /// Gets categories for any retailer by code (e.g. MYTEK, TUNISIANET, SPACENET).
    /// </summary>
    [HttpGet("retailer/{retailerCode}")]
    [ProducesResponseType<IEnumerable<CategoryDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<CategoryDto>>> GetRetailerCategories(string retailerCode, CancellationToken cancellationToken)
    {
        var categories = await _context.RetailerCategories
            .AsNoTracking()
            .Where(rc => rc.Retailer.Code == retailerCode.ToUpperInvariant())
            .OrderBy(rc => rc.ParentCategoryName)
            .ThenBy(rc => rc.Title)
            .Select(rc => new CategoryDto(rc.ParentCategoryName, rc.Title, rc.Url))
            .ToListAsync(cancellationToken);

        return Ok(categories);
    }

    /// <summary>
    /// Gets the unified platform categories tree.
    /// </summary>
    [HttpGet]
    [ProducesResponseType<IEnumerable<Category>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<Category>>> GetUnifiedCategories(CancellationToken cancellationToken)
    {
        var categories = await _context.Categories
            .AsNoTracking()
            .Where(c => c.ParentCategoryId == null)
            .Include(c => c.SubCategories)
            .ToListAsync(cancellationToken);

        return Ok(categories);
    }
}

