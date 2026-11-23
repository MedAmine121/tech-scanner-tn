using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Hi_Trade.Services.Interfaces;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace Hi_Trade.Services.Services;

/// <summary>
/// Redis-backed distributed cache implementation that fails open if the cache store is unavailable.
/// </summary>
public class RedisCacheService : ICacheService
{
    private static readonly TimeSpan DefaultExpiration = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan VersionTokenExpiration = TimeSpan.FromHours(24);
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReferenceHandler = ReferenceHandler.IgnoreCycles
    };

    private readonly IDistributedCache _cache;
    private readonly ILogger<RedisCacheService> _logger;

    public RedisCacheService(IDistributedCache cache, ILogger<RedisCacheService> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            var cachedJson = await _cache.GetStringAsync(key, cancellationToken);
            if (string.IsNullOrEmpty(cachedJson))
            {
                return default;
            }

            return JsonSerializer.Deserialize<T>(cachedJson, JsonOptions);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Redis GET failed for key {CacheKey}. Failing open.", key);
            return default;
        }
    }

    public async Task SetAsync<T>(
        string key,
        T value,
        TimeSpan? expiration = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var payload = JsonSerializer.Serialize(value, JsonOptions);
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = expiration ?? DefaultExpiration
            };

            await _cache.SetStringAsync(key, payload, options, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Redis SET failed for key {CacheKey}. Continuing without caching.", key);
        }
    }

    public async Task<T> GetOrSetAsync<T>(
        string key,
        Func<CancellationToken, Task<T>> factory,
        TimeSpan? expiration = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var cachedJson = await _cache.GetStringAsync(key, cancellationToken);
            if (!string.IsNullOrEmpty(cachedJson))
            {
                var cachedValue = JsonSerializer.Deserialize<T>(cachedJson, JsonOptions);
                if (cachedValue is not null)
                {
                    return cachedValue;
                }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Redis GET failed in GetOrSetAsync for key {CacheKey}. Falling back to data source.", key);
        }

        var value = await factory(cancellationToken);

        if (value is not null)
        {
            await SetAsync(key, value, expiration, cancellationToken);
        }

        return value;
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            await _cache.RemoveAsync(key, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Redis REMOVE failed for key {CacheKey}.", key);
        }
    }

    public async Task<string> GetVersionTokenAsync(string scope, CancellationToken cancellationToken = default)
    {
        var versionKey = GetScopeVersionKey(scope);

        try
        {
            var token = await _cache.GetStringAsync(versionKey, cancellationToken);
            if (!string.IsNullOrWhiteSpace(token))
            {
                return token;
            }

            token = "1";
            await _cache.SetStringAsync(
                versionKey,
                token,
                new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = VersionTokenExpiration },
                cancellationToken);

            return token;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Redis GetVersionTokenAsync failed for scope {CacheScope}. Using fallback token.", scope);
            return "fallback";
        }
    }

    public async Task InvalidateScopeAsync(string scope, CancellationToken cancellationToken = default)
    {
        var versionKey = GetScopeVersionKey(scope);
        var newToken = Guid.NewGuid().ToString("N")[..12];

        try
        {
            await _cache.SetStringAsync(
                versionKey,
                newToken,
                new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = VersionTokenExpiration },
                cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Redis InvalidateScopeAsync failed for scope {CacheScope}.", scope);
        }
    }

    private static string GetScopeVersionKey(string scope) => $"scope:version:{scope}";
}
