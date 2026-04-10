using CleanArchitecture.Domain.Interfaces.Caching;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CleanArchitecture.Infrastructure.Caching;

/// <summary>
/// Registers cache services: settings binding + provider-based ICacheService implementation.
/// </summary>
public static class CacheServiceRegistration
{
    /// <summary>
    /// Adds cache services to the DI container based on CacheSettings.Provider configuration.
    /// </summary>
    public static IServiceCollection AddCacheServices(
        this IServiceCollection services, IConfiguration configuration)
    {
        var section = configuration.GetSection(CacheSettings.SectionName);
        services.Configure<CacheSettings>(section);

        var settings = section.Get<CacheSettings>() ?? new CacheSettings();

        switch (settings.Provider)
        {
            case "Memory":
                services.AddMemoryCache();
                services.AddSingleton<ICacheService, MemoryCacheService>();
                break;

            case "Redis":
                services.AddStackExchangeRedisCache(options =>
                {
                    options.Configuration = settings.RedisConnectionString;
                });
                services.AddSingleton<ICacheService, RedisCacheService>();
                break;

            default:
                throw new InvalidOperationException(
                    $"Invalid cache provider: '{settings.Provider}'. Use 'Memory' or 'Redis'.");
        }

        return services;
    }
}
