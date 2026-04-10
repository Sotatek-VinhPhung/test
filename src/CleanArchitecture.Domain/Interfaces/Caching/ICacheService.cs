namespace CleanArchitecture.Domain.Interfaces.Caching;

/// <summary>
/// Provider-agnostic cache service. Implementations live in Infrastructure.
/// </summary>
public interface ICacheService
{
    /// <summary>Retrieve a cached value. Returns null on cache miss.</summary>
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
        where T : class;

    /// <summary>Store a value. Pass null TTL to use the configured default.</summary>
    Task SetAsync<T>(string key, T value, TimeSpan? ttl = null,
        CancellationToken cancellationToken = default) where T : class;

    /// <summary>Remove a cached entry by key.</summary>
    Task RemoveAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>Check whether a key exists in the cache.</summary>
    Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default);
}
