using System;
using System.Threading;
using System.Threading.Tasks;

namespace Hi_Trade.Services.Interfaces;

/// <summary>
/// Provides distributed caching operations with scope-based invalidation.
/// </summary>
public interface ICacheService
{
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default);

    Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default);

    Task<T> GetOrSetAsync<T>(
        string key,
        Func<CancellationToken, Task<T>> factory,
        TimeSpan? expiration = null,
        CancellationToken cancellationToken = default);

    Task RemoveAsync(string key, CancellationToken cancellationToken = default);

    Task<string> GetVersionTokenAsync(string scope, CancellationToken cancellationToken = default);

    Task InvalidateScopeAsync(string scope, CancellationToken cancellationToken = default);
}
