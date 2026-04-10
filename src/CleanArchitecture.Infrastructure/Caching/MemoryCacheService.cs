using CleanArchitecture.Domain.Interfaces.Caching;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CleanArchitecture.Infrastructure.Caching;

/// <summary>
/// In-memory cache implementation using IMemoryCache.
/// </summary>
public sealed class MemoryCacheService : ICacheService
{
    private readonly IMemoryCache _cache;
    private readonly CacheSettings _settings;
    private readonly ILogger<MemoryCacheService> _logger;

    public MemoryCacheService(
        IMemoryCache cache,
        IOptions<CacheSettings> settings,
        ILogger<MemoryCacheService> logger)
    {
        _cache = cache;
        _settings = settings.Value;
        _logger = logger;
    }

    public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
        where T : class
    {
        var found = _cache.TryGetValue(key, out T? value);
        _logger.LogDebug("Cache {Result} for key: {Key}", found ? "HIT" : "MISS", key);
        return Task.FromResult(value);
    }

    public Task SetAsync<T>(string key, T value, TimeSpan? ttl = null,
        CancellationToken cancellationToken = default) where T : class
    {
        var expiration = ttl ?? TimeSpan.FromMinutes(_settings.DefaultTtlMinutes);
        var options = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = expiration
        };
        _cache.Set(key, value, options);
        _logger.LogDebug("Cache SET key: {Key}, TTL: {Ttl}", key, expiration);
        return Task.CompletedTask;
    }

    public Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        _cache.Remove(key);
        _logger.LogDebug("Cache REMOVE key: {Key}", key);
        return Task.CompletedTask;
    }

    public Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_cache.TryGetValue(key, out _));
    }
}
