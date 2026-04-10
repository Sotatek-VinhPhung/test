namespace CleanArchitecture.Infrastructure.Caching;

/// <summary>
/// Cache configuration POCO — bound from appsettings.json "CacheSettings" section.
/// </summary>
public class CacheSettings
{
    public const string SectionName = "CacheSettings";

    /// <summary>Cache provider: "Memory" or "Redis".</summary>
    public string Provider { get; set; } = "Memory";

    /// <summary>Default TTL for cached items in minutes.</summary>
    public int DefaultTtlMinutes { get; set; } = 60;

    /// <summary>Redis connection string (used only when Provider is "Redis").</summary>
    public string RedisConnectionString { get; set; } = "localhost:6379";
}
