# Phase 02 — Domain Interface (ICacheService)

## Context Links

- [Plan overview](plan.md)
- [Phase 01 — NuGet + Settings](phase-01-nuget-and-settings.md)
- Existing pattern: [IRepository.cs](../../src/CleanArchitecture.Domain/Interfaces/IRepository.cs)
- Existing pattern: [IKafkaPublisher.cs](../../src/CleanArchitecture.Domain/Interfaces/Messaging/IKafkaPublisher.cs)

## Overview

- **Priority:** High — implementations in Phase 03 depend on this
- **Status:** Pending
- **Description:** Define the `ICacheService` interface in `Domain/Interfaces/Caching/`. Generic get/set/remove with TTL support and CancellationToken.

## Key Insights

- Domain interfaces follow pattern: namespace matches folder path, XML doc comments, `CancellationToken` with default
- Messaging interfaces are grouped in `Interfaces/Messaging/` subfolder — caching should use `Interfaces/Caching/`
- Domain project has zero NuGet dependencies — interface must use only BCL types
- `GetAsync<T>` returns `T?` to signal cache miss (not exceptions)
- `SetAsync` accepts optional `TimeSpan?` TTL override (null = use default from settings)

## Requirements

### Functional
- `GetAsync<T>(string key)` — retrieve cached value by key, return `null` on miss
- `SetAsync<T>(string key, T value, TimeSpan? ttl)` — store value with optional TTL override
- `RemoveAsync(string key)` — evict a specific key
- `ExistsAsync(string key)` — check if key exists without deserializing

### Non-Functional
- Interface under 30 lines
- No dependencies on Infrastructure or external packages
- All methods async with CancellationToken

## Architecture

```
Domain/
└── Interfaces/
    └── Caching/
        └── ICacheService.cs
```

Interface contract:
```
ICacheService
├── GetAsync<T>(key, ct) → T?
├── SetAsync<T>(key, value, ttl?, ct) → Task
├── RemoveAsync(key, ct) → Task
└── ExistsAsync(key, ct) → Task<bool>
```

## Related Code Files

### Create
- `src/CleanArchitecture.Domain/Interfaces/Caching/ICacheService.cs`

### Modify
- None

## Implementation Steps

### Step 1: Create directory

Create `src/CleanArchitecture.Domain/Interfaces/Caching/` directory.

### Step 2: Create ICacheService.cs

File: `src/CleanArchitecture.Domain/Interfaces/Caching/ICacheService.cs`

```csharp
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
```

### Design Notes

- `where T : class` constraint keeps it simple — no value-type boxing concerns
- Returning `T?` (nullable reference) instead of throwing `KeyNotFoundException` on miss — callers do a null check
- `TimeSpan?` TTL parameter: `null` = implementation uses `CacheSettings.DefaultTtlMinutes`
- `ExistsAsync` avoids deserialization cost when caller only needs existence check

## Todo List

- [ ] Create `Interfaces/Caching/` directory in Domain
- [ ] Create `ICacheService.cs`
- [ ] Run `dotnet build` on Domain project to verify

## Success Criteria

- `dotnet build` passes for Domain project
- Interface has no external dependencies
- Follows same code style as `IRepository<T>` and `IKafkaPublisher`

## Risk Assessment

| Risk | Impact | Mitigation |
|------|--------|------------|
| Interface too broad (YAGNI) | Unnecessary complexity | Only 4 methods — minimal viable surface |
| Missing `where T : class` constraint | Serialization issues in Redis impl | Constraint enforced at interface level |

## Next Steps

→ Phase 03: Implement `MemoryCacheService` and `RedisCacheService` in Infrastructure
