# Cache Abstraction Layer (Memory + Redis)

## Overview

Add a provider-agnostic caching layer to the .NET 8 Clean Architecture project.  
Domain defines `ICacheService`; Infrastructure provides `MemoryCacheService` and `RedisCacheService`; config selects provider at startup.

## Architecture

```
Domain/Interfaces/Caching/ICacheService.cs  ← interface only
Infrastructure/Caching/CacheSettings.cs     ← POCO (SectionName pattern)
Infrastructure/Caching/MemoryCacheService.cs
Infrastructure/Caching/RedisCacheService.cs
Infrastructure/Caching/CacheServiceRegistration.cs  ← AddCacheServices() extension
Api/appsettings.json                        ← CacheSettings section
Api/Program.cs                              ← builder.Services.AddCacheServices(...)
```

## Phases

| # | Phase | Status | File |
|---|-------|--------|------|
| 1 | NuGet + CacheSettings POCO + appsettings config | Pending | [phase-01](phase-01-nuget-and-settings.md) |
| 2 | Domain interface (ICacheService) | Pending | [phase-02](phase-02-domain-interface.md) |
| 3 | Infrastructure implementations | Pending | [phase-03](phase-03-infrastructure-implementations.md) |
| 4 | DI registration + API wiring | Pending | [phase-04](phase-04-di-registration.md) |

## Key Dependencies

- `Microsoft.Extensions.Caching.Memory` (built-in, already in shared framework)
- `Microsoft.Extensions.Caching.StackExchangeRedis` (NuGet — Infrastructure only)

## Design Decisions

- **System.Text.Json** for Redis serialization (no Newtonsoft)
- **Singleton** for both cache services (stateless, thread-safe)
- **CacheSettings.Provider** enum (`Memory` / `Redis`) selects implementation
- Interface lives in `Domain/Interfaces/Caching/` (follows Messaging pattern)
- Settings POCO lives in `Infrastructure/Caching/` (follows JwtSettings/KafkaSettings pattern)

## Success Criteria

- `dotnet build` passes for all 4 projects
- Memory cache works with zero external dependencies
- Redis cache works when Redis server is available
- Switching provider requires only an `appsettings.json` change
- All files under 200 lines
