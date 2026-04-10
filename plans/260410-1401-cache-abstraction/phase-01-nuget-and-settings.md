# Phase 01 — NuGet + CacheSettings POCO + appsettings Config

## Context Links

- [Plan overview](plan.md)
- Existing pattern: [JwtSettings.cs](../../src/CleanArchitecture.Infrastructure/Auth/JwtSettings.cs)
- Existing pattern: [KafkaSettings.cs](../../src/CleanArchitecture.Infrastructure/Messaging/KafkaSettings.cs)
- Existing config: [appsettings.json](../../src/CleanArchitecture.Api/appsettings.json)

## Overview

- **Priority:** High — all other phases depend on this
- **Status:** Pending
- **Description:** Add StackExchangeRedis NuGet to Infrastructure, create `CacheSettings` POCO following existing settings pattern, and add `CacheSettings` section to appsettings.json.

## Key Insights

- Existing settings POCOs use `public const string SectionName` + public properties with defaults
- `Microsoft.Extensions.Caching.Memory` is part of the shared framework — no NuGet needed
- `Microsoft.Extensions.Caching.StackExchangeRedis` must be added to Infrastructure.csproj
- Version should use `8.0.*` wildcard pattern consistent with existing EF Core references

## Requirements

### Functional
- `CacheSettings` POCO with: `Provider` (string: "Memory"/"Redis"), `DefaultTtlMinutes` (int), `RedisConnectionString` (string)
- appsettings.json must include `CacheSettings` section with sensible defaults

### Non-Functional
- POCO under 30 lines
- Defaults allow app to start without Redis (Memory is default)

## Architecture

```
CacheSettings
├── Provider: string             → "Memory" (default) or "Redis"
├── DefaultTtlMinutes: int       → 60 (default)
└── RedisConnectionString: string → "localhost:6379" (default)
```

No enum in Domain — keep it as a simple string in the POCO; DI registration handles the switch logic.

## Related Code Files

### Create
- `src/CleanArchitecture.Infrastructure/Caching/CacheSettings.cs`

### Modify
- `src/CleanArchitecture.Infrastructure/CleanArchitecture.Infrastructure.csproj` — add StackExchangeRedis NuGet
- `src/CleanArchitecture.Api/appsettings.json` — add CacheSettings section
- `src/CleanArchitecture.Api/appsettings.Development.json` — add CacheSettings section (Memory default)

## Implementation Steps

### Step 1: Add NuGet to Infrastructure.csproj

Add to the `<ItemGroup>` containing `<PackageReference>`:

```xml
<PackageReference Include="Microsoft.Extensions.Caching.StackExchangeRedis" Version="8.0.*" />
```

### Step 2: Create CacheSettings.cs

File: `src/CleanArchitecture.Infrastructure/Caching/CacheSettings.cs`

```csharp
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
```

### Step 3: Add CacheSettings to appsettings.json

Add after the `KafkaSettings` section:

```json
"CacheSettings": {
  "Provider": "Memory",
  "DefaultTtlMinutes": 60,
  "RedisConnectionString": "localhost:6379"
}
```

### Step 4: Add CacheSettings to appsettings.Development.json

Same section, Memory provider for local dev.

## Todo List

- [ ] Add `Microsoft.Extensions.Caching.StackExchangeRedis` to Infrastructure.csproj
- [ ] Create `Caching/CacheSettings.cs` in Infrastructure
- [ ] Add `CacheSettings` section to `appsettings.json`
- [ ] Add `CacheSettings` section to `appsettings.Development.json`
- [ ] Run `dotnet build` to verify no compilation errors

## Success Criteria

- `dotnet build` passes for Infrastructure and Api projects
- `CacheSettings` follows identical pattern to `JwtSettings` / `KafkaSettings`
- NuGet restore succeeds for StackExchangeRedis

## Risk Assessment

| Risk | Impact | Mitigation |
|------|--------|------------|
| StackExchangeRedis version mismatch with .NET 8 | Build failure | Use `8.0.*` wildcard |
| Misconfigured Redis connection string | Runtime error | Default to Memory; Redis only used when explicitly configured |

## Next Steps

→ Phase 02: Define `ICacheService` interface in Domain
