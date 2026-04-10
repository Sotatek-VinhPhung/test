using System.Text.Json;
using CleanArchitecture.Domain.Interfaces.Caching;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CleanArchitecture.Infrastructure.Caching;

/// <summary>
/// Redis cache implementation using IDistributedCache + System.Text.Json.
/// All operations are wrapped in try-catch for graceful degradation on Redis downtime.
/// </summary>
public sealed class RedisCacheService : ICacheService
{
    private readonly IDistributedCache _cache;
    private readonly CacheSettings _settings;
    private readonly ILogger<RedisCacheService> _logger;

    public RedisCacheService(
        IDistributedCache cache,
        IOptions<CacheSettings> settings,
        ILogger<RedisCacheService> logger)
    {
        _cache = cache;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
        where T : class
    {
        try
        {
            var json = await _cache.GetStringAsync(key, cancellationToken);
            if (json is null)
            {
                _logger.LogDebug("Cache MISS for key: {Key}", key);
                return null;
            }
            _logger.LogDebug("Cache HIT for key: {Key}", key);
            return JsonSerializer.Deserialize<T>(json);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Redis GET failed for key: {Key}", key);
            return null;
        }
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? ttl = null,
        CancellationToken cancellationToken = default) where T : class
    {
        try
        {
            var expiration = ttl ?? TimeSpan.FromMinutes(_settings.DefaultTtlMinutes);
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = expiration
            };
            var json = JsonSerializer.Serialize(value);
            await _cache.SetStringAsync(key, json, options, cancellationToken);
            _logger.LogDebug("Cache SET key: {Key}, TTL: {Ttl}", key, expiration);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Redis SET failed for key: {Key}", key);
        }
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            await _cache.RemoveAsync(key, cancellationToken);
            _logger.LogDebug("Cache REMOVE key: {Key}", key);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Redis REMOVE failed for key: {Key}", key);
        }
    }

    public async Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            var value = await _cache.GetStringAsync(key, cancellationToken);
            return value is not null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Redis EXISTS failed for key: {Key}", key);
            return false;
        }
    }
}
