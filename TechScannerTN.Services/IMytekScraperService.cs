using Hi_Trade.Models;

namespace Hi_Trade.Services;

public interface IMytekScraperService
{
    Task<List<Product>> ScrapeAsync();

    Task<IEnumerable<CategoryDto>> GetCategoriesAsync(CancellationToken cancellationToken = default);
}
