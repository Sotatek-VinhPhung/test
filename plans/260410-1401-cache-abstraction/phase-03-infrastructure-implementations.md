# Phase 03 — Infrastructure Implementations (MemoryCacheService + RedisCacheService)

## Context Links

- [Plan overview](plan.md)
- [Phase 01 — NuGet + Settings](phase-01-nuget-and-settings.md)
- [Phase 02 — Domain interface](phase-02-domain-interface.md)
- Existing impl pattern: [KafkaPublisher.cs](../../src/CleanArchitecture.Infrastructure/Messaging/KafkaPublisher.cs)

## Overview

- **Priority:** High — core deliverable
- **Status:** Pending
- **Description:** Two implementations of `ICacheService` in `Infrastructure/Caching/`: one using `IMemoryCache`, one using `IDistributedCache` (StackExchange.Redis).

## Key Insights

- Both implementations need `IOptions<CacheSettings>` for default TTL
- `MemoryCacheService`: wraps `IMemoryCache`, stores objects directly (no serialization)
- `RedisCacheService`: wraps `IDistributedCache`, serializes with `System.Text.Json`
- Both should be registered as **Singleton** (thread-safe, no scoped dependencies)
- Serilog `ILogger<T>` for logging cache hits/misses at Debug level, errors at Warning level
- Each file should stay under 100 lines — well within the 200-line limit

## Requirements

### Functional
- Both implement `ICacheService` with identical behavior contract
- `GetAsync<T>`: return deserialized value or null
- `SetAsync<T>`: store with TTL (use default if null)
- `RemoveAsync`: evict by key
- `ExistsAsync`: check existence

### Non-Functional
- Thread-safe
- Graceful error handling — log and return null/false, don't throw
- System.Text.Json for Redis serialization (not Newtonsoft)

## Architecture

```
Infrastructure/Caching/
├── CacheSettings.cs            (Phase 01)
├── MemoryCacheService.cs       ← IMemoryCache wrapper
└── RedisCacheService.cs        ← IDistributedCache + System.Text.Json
```

### MemoryCacheService Flow
```
GetAsync → IMemoryCache.TryGetValue → return T? (already in-memory, no deserialization)
SetAsync → IMemoryCache.Set with MemoryCacheEntryOptions.AbsoluteExpirationRelativeToNow
```

### RedisCacheService Flow
```
GetAsync → IDistributedCache.GetStringAsync → JsonSerializer.Deserialize<T>
SetAsync → JsonSerializer.Serialize(value) → IDistributedCache.SetStringAsync with TTL
```

## Related Code Files

### Create
- `src/CleanArchitecture.Infrastructure/Caching/MemoryCacheService.cs`
- `src/CleanArchitecture.Infrastructure/Caching/RedisCacheService.cs`

### Modify
- None

## Implementation Steps

### Step 1: Create MemoryCacheService.cs

File: `src/CleanArchitecture.Infrastructure/Caching/MemoryCacheService.cs`

```csharp
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
```

### Step 2: Create RedisCacheService.cs

File: `src/CleanArchitecture.Infrastructure/Caching/RedisCacheService.cs`

```csharp
using System.Text.Json;
using CleanArchitecture.Domain.Interfaces.Caching;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CleanArchitecture.Infrastructure.Caching;

/// <summary>
/// Redis cache implementation using IDistributedCache + System.Text.Json.
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
```

### Design Notes

- **MemoryCacheService** returns `Task.FromResult` / `Task.CompletedTask` — sync under the hood, async interface for swap-ability
- **RedisCacheService** wraps all operations in try-catch — Redis unavailability shouldn't crash the app
- Both log at `Debug` for normal operations, `Warning` for errors
- `RedisCacheService.ExistsAsync` uses `GetStringAsync` (no dedicated EXISTS in `IDistributedCache`) — acceptable tradeoff for simplicity

## Todo List

- [ ] Create `Caching/MemoryCacheService.cs` in Infrastructure
- [ ] Create `Caching/RedisCacheService.cs` in Infrastructure
- [ ] Verify both files are under 200 lines
- [ ] Run `dotnet build` on Infrastructure project

## Success Criteria

- Both classes compile and implement `ICacheService`
- `MemoryCacheService` < 70 lines
- `RedisCacheService` < 100 lines
- Error handling in Redis impl: no unhandled exceptions on Redis downtime
- System.Text.Json used (no Newtonsoft dependency)

## Risk Assessment

| Risk | Impact | Mitigation |
|------|--------|------------|
| Redis connection failure at runtime | Cache ops silently fail | Try-catch + Warning log; app continues without cache |
| System.Text.Json can't serialize complex types | Data loss | `where T : class` constraint; document limitation |
| IDistributedCache lacks `ExistsAsync` | Extra deserialization cost | Use `GetStringAsync` + null check (KISS) |
| Memory cache grows unbounded | OOM in production | TTL-based eviction; future: add size limit if needed |

## Security Considerations

- Redis connection string in appsettings — should use environment variables or secrets in production
- No sensitive data should be cached without encryption consideration

## Next Steps

→ Phase 04: DI registration + API wiring
