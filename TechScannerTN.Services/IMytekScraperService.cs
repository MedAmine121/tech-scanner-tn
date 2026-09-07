using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Hi_Trade.Services;

public interface IMytekScraperService
{
    Task<int> ScrapeAsync(CancellationToken cancellationToken = default);

    Task<IEnumerable<CategoryDto>> GetCategoriesAsync(CancellationToken cancellationToken = default);
}

