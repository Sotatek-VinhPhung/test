# Phase 04 — DI Registration + API Wiring

## Context Links

- [Plan overview](plan.md)
- [Phase 01 — NuGet + Settings](phase-01-nuget-and-settings.md)
- [Phase 02 — Domain interface](phase-02-domain-interface.md)
- [Phase 03 — Implementations](phase-03-infrastructure-implementations.md)
- Existing pattern: [KafkaServiceRegistration.cs](../../src/CleanArchitecture.Infrastructure/Messaging/KafkaServiceRegistration.cs)
- Existing pattern: [DependencyInjection.cs](../../src/CleanArchitecture.Infrastructure/DependencyInjection.cs)
- Existing pattern: [Program.cs](../../src/CleanArchitecture.Api/Program.cs)

## Overview

- **Priority:** High — final wiring phase
- **Status:** Pending
- **Description:** Create `AddCacheServices()` extension method in Infrastructure that reads `CacheSettings.Provider` and registers the correct `ICacheService` implementation. Wire into `Program.cs`.

## Key Insights

- Existing DI pattern: static class with `IServiceCollection` extension method accepting `IConfiguration`
- Kafka uses a separate registration file (`KafkaServiceRegistration.cs`) — cache should follow same pattern
- Provider selection is a simple string comparison at startup — no runtime switching needed
- Both cache services are **Singleton** (thread-safe, no scoped deps)
- Must call `services.AddMemoryCache()` for Memory provider, `services.AddStackExchangeRedisCache(...)` for Redis

## Requirements

### Functional
- `AddCacheServices(IConfiguration)` extension method
- Reads `CacheSettings.Provider` to determine which implementation to register
- Registers `ICacheService` as Singleton with the chosen implementation
- Binds `CacheSettings` via `services.Configure<CacheSettings>(...)`
- Invalid provider value throws `InvalidOperationException` at startup (fail-fast)

### Non-Functional
- Registration file under 60 lines
- One-line addition to Program.cs
- Follows existing naming conventions exactly

## Architecture

### Registration Flow
```
AddCacheServices(config)
├── Configure<CacheSettings>(section)
├── Read Provider value
├── Switch:
│   ├── "Memory" → AddMemoryCache() + AddSingleton<ICacheService, MemoryCacheService>()
│   └── "Redis"  → AddStackExchangeRedisCache(opts) + AddSingleton<ICacheService, RedisCacheService>()
│   └── _        → throw InvalidOperationException
└── return services
```

### Program.cs Addition
```
builder.Services.AddCacheServices(builder.Configuration);  // after AddKafkaServices
```

## Related Code Files

### Create
- `src/CleanArchitecture.Infrastructure/Caching/CacheServiceRegistration.cs`

### Modify
- `src/CleanArchitecture.Api/Program.cs` — add `AddCacheServices` call

## Implementation Steps

### Step 1: Create CacheServiceRegistration.cs

File: `src/CleanArchitecture.Infrastructure/Caching/CacheServiceRegistration.cs`

```csharp
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
        // Bind CacheSettings from appsettings.json
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
```

### Step 2: Add to Program.cs

After the `AddKafkaServices` line (line 20 in current Program.cs), add:

```csharp
builder.Services.AddCacheServices(builder.Configuration);
```

Also add the using at the top:

```csharp
using CleanArchitecture.Infrastructure.Caching;
```

### Step 3: Verify the full call chain

Program.cs registration order:
```csharp
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);
builder.Services.AddKafkaServices(builder.Configuration);
builder.Services.AddCacheServices(builder.Configuration);   // ← NEW
```

## Todo List

- [ ] Create `Caching/CacheServiceRegistration.cs` in Infrastructure
- [ ] Add `using CleanArchitecture.Infrastructure.Caching;` to Program.cs
- [ ] Add `builder.Services.AddCacheServices(builder.Configuration);` to Program.cs
- [ ] Run `dotnet build` on full solution
- [ ] Verify app starts with Memory provider (default)
- [ ] Verify app starts with Redis provider (when Redis available)

## Success Criteria

- `dotnet build` passes for entire solution
- App starts without errors using Memory provider (no Redis needed)
- Switching to `"Redis"` in appsettings and restarting works (when Redis is running)
- Invalid provider value causes clear startup error
- `CacheServiceRegistration.cs` under 60 lines
- Registration follows same style as `KafkaServiceRegistration`

## Risk Assessment

| Risk | Impact | Mitigation |
|------|--------|------------|
| Redis not available at startup | App crash | Default provider is Memory; Redis only when explicitly configured |
| Typo in Provider value | Silent failure | Fail-fast with `InvalidOperationException` in default case |
| Singleton lifecycle mismatch | Memory leaks | Both IMemoryCache and IDistributedCache are designed for singleton use |

## Security Considerations

- `RedisConnectionString` may contain passwords — use environment variables or User Secrets in production
- Do not log the full connection string

## Next Steps

After all 4 phases complete:
- Run full `dotnet build` verification
- Test Memory provider locally
- Test Redis provider with local Redis instance
- Consider adding cache usage to `UserService` or `PermissionService` as follow-up work
