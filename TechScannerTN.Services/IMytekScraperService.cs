namespace Hi_Trade.Services;

public interface IMytekScraperService
{
    Task<IEnumerable<CategoryDto>> GetCategoriesAsync(CancellationToken cancellationToken = default);
}
