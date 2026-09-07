using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Hi_Trade.Models.DTOs;

namespace Hi_Trade.Services;

public interface IProductCatalogService
{
    Task<PagedResult<ProductComparisonDto>> GetProductsAsync(ProductFilterRequest request, CancellationToken cancellationToken = default);

    Task<ProductComparisonDto?> GetProductDetailsAsync(int productId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PriceHistoryDto>> GetPriceHistoryAsync(int productId, CancellationToken cancellationToken = default);

    Task<MarketOverviewDto> GetMarketOverviewAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ProductComparisonDto>> GetPriceDropsAsync(int limit = 20, CancellationToken cancellationToken = default);
}

