using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Hi_Trade.Models.DTOs;
using Hi_Trade.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Hi_Trade.Controllers;

[ApiController]
[Route("api/products")]
public class ProductsController : ControllerBase
{
    private readonly IProductCatalogService _catalogService;

    public ProductsController(IProductCatalogService catalogService)
    {
        _catalogService = catalogService;
    }

    /// <summary>
    /// Searches and filters products across all scraped websites.
    /// Orders by cheapest price by default.
    /// </summary>
    [HttpGet]
    [ProducesResponseType<PagedResult<ProductComparisonDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<ProductComparisonDto>>> GetProducts(
        [FromQuery] ProductFilterRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _catalogService.GetProductsAsync(request, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Gets side-by-side price comparison and difference breakdown for a specific product across all retailers.
    /// </summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType<ProductComparisonDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductComparisonDto>> GetProductDetails(
        int id,
        CancellationToken cancellationToken)
    {
        var product = await _catalogService.GetProductDetailsAsync(id, cancellationToken);
        if (product == null)
        {
            return NotFound(new { message = $"Product with ID {id} not found." });
        }

        return Ok(product);
    }

    /// <summary>
    /// Retrieves historical price points for charting price trends across retailers over time.
    /// </summary>
    [HttpGet("{id:int}/price-history")]
    [ProducesResponseType<IReadOnlyList<PriceHistoryDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<PriceHistoryDto>>> GetPriceHistory(
        int id,
        CancellationToken cancellationToken)
    {
        var history = await _catalogService.GetPriceHistoryAsync(id, cancellationToken);
        return Ok(history);
    }

    /// <summary>
    /// Gets products currently at or near their all-time lowest recorded price.
    /// </summary>
    [HttpGet("price-drops")]
    [ProducesResponseType<IReadOnlyList<ProductComparisonDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ProductComparisonDto>>> GetPriceDrops(
        [FromQuery] int limit = 20,
        CancellationToken cancellationToken = default)
    {
        var drops = await _catalogService.GetPriceDropsAsync(limit, cancellationToken);
        return Ok(drops);
    }
}

