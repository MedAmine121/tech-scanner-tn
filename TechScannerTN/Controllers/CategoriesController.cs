using Hi_Trade.Services;
using Hi_Trade.DAL;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Hi_Trade.Controllers;

[ApiController]
[Route("api/categories")]
public sealed class CategoriesController : ControllerBase
{
    private readonly TechScannerContext _context;

    public CategoriesController(
        TechScannerContext context)
    {
        _context = context;
    }

    [HttpGet("mytek")]
    [ProducesResponseType<IEnumerable<CategoryDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    public async Task<ActionResult<IEnumerable<CategoryDto>>> GetMytekCategories(CancellationToken cancellationToken)
    {
        var categories = await _context.Categories
            .AsNoTracking()
            .OrderBy(category => category.ParentCategory)
            .ThenBy(category => category.Title)
            .Select(category => new CategoryDto(category.ParentCategory, category.Title, category.Url))
            .ToListAsync(cancellationToken);

        return Ok(categories);
    }
}
