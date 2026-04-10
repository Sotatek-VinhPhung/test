# Phase 01 — NuGet + Config Setup

**Priority:** High
**Status:** ✅ Complete
**Depends on:** Nothing

## Context Links

- [Plan overview](plan.md)
- [Existing appsettings.json](../../src/CleanArchitecture.Api/appsettings.json)
- [Infrastructure .csproj](../../src/CleanArchitecture.Infrastructure/CleanArchitecture.Infrastructure.csproj)
- [JwtSettings pattern](../../src/CleanArchitecture.Infrastructure/Auth/JwtSettings.cs)

## Overview

Install Confluent.Kafka NuGet package on Infrastructure and add KafkaSettings configuration section to appsettings files + a strongly-typed POCO.

## Key Insights

- Project uses `PackageReference` with wildcard versions (e.g. `8.0.*`) for MS packages but pinned versions for third-party (e.g. `BCrypt.Net-Next 4.1.0`). Use pinned version for Confluent.Kafka.
- Config pattern: POCO class with `const string SectionName`, bound in DI via `IOptions<T>`. See `JwtSettings.cs`.
- `appsettings.Development.json` should override only dev-specific values (broker address).

## Requirements

### Functional
- Confluent.Kafka package available to Infrastructure project
- KafkaSettings POCO with all needed config properties
- appsettings.json contains KafkaSettings section with sensible defaults

### Non-Functional
- POCO under 50 lines
- No Kafka connection at this phase — config only

## Related Code Files

### Modify
- `src/CleanArchitecture.Infrastructure/CleanArchitecture.Infrastructure.csproj` — add PackageReference
- `src/CleanArchitecture.Api/appsettings.json` — add KafkaSettings section
- `src/CleanArchitecture.Api/appsettings.Development.json` — add dev KafkaSettings override

### Create
- `src/CleanArchitecture.Infrastructure/Messaging/KafkaSettings.cs` — KafkaSettings POCO

## Implementation Steps

### 1. Add Confluent.Kafka NuGet package

Add to `CleanArchitecture.Infrastructure.csproj`:

```xml
<PackageReference Include="Confluent.Kafka" Version="2.6.1" />
```

### 2. Create KafkaSettings POCO

Create `src/CleanArchitecture.Infrastructure/Messaging/KafkaSettings.cs`:

```csharp
namespace CleanArchitecture.Infrastructure.Messaging;

/// <summary>
/// Kafka configuration POCO — bound from appsettings.json "KafkaSettings" section.
/// </summary>
public class KafkaSettings
{
    public const string SectionName = "KafkaSettings";

    /// <summary>Comma-separated broker addresses.</summary>
    public string BootstrapServers { get; set; } = "localhost:9092";

    /// <summary>Consumer group ID for this application.</summary>
    public string GroupId { get; set; } = "clean-architecture-group";

    /// <summary>Enable auto-commit for consumer offsets.</summary>
    public bool EnableAutoCommit { get; set; } = false;

    /// <summary>Auto offset reset policy: earliest | latest.</summary>
    public string AutoOffsetReset { get; set; } = "earliest";

    /// <summary>Request timeout in milliseconds.</summary>
    public int RequestTimeoutMs { get; set; } = 30000;

    /// <summary>Max retry attempts before sending to DLQ.</summary>
    public int MaxRetryAttempts { get; set; } = 3;

    /// <summary>Delay between retries in milliseconds.</summary>
    public int RetryDelayMs { get; set; } = 1000;

    /// <summary>Suffix appended to topic name for DLQ.</summary>
    public string DlqTopicSuffix { get; set; } = ".dlq";

    /// <summary>Reply topic for request/reply pattern.</summary>
    public string ReplyTopic { get; set; } = "reply-topic";

    /// <summary>Timeout for request/reply in milliseconds.</summary>
    public int RequestReplyTimeoutMs { get; set; } = 10000;
}
```

### 3. Add KafkaSettings to appsettings.json

Add after the `JwtSettings` section:

```json
"KafkaSettings": {
    "BootstrapServers": "localhost:9092",
    "GroupId": "clean-architecture-group",
    "EnableAutoCommit": false,
    "AutoOffsetReset": "earliest",
    "RequestTimeoutMs": 30000,
    "MaxRetryAttempts": 3,
    "RetryDelayMs": 1000,
    "DlqTopicSuffix": ".dlq",
    "ReplyTopic": "reply-topic",
    "RequestReplyTimeoutMs": 10000
}
```

### 4. Add dev override in appsettings.Development.json

```json
"KafkaSettings": {
    "BootstrapServers": "localhost:9092"
}
```

## Todo List

- [ ] Add `Confluent.Kafka` PackageReference to Infrastructure .csproj
- [ ] Create `KafkaSettings.cs` POCO in `Infrastructure/Messaging/`
- [ ] Add `KafkaSettings` section to `appsettings.json`
- [ ] Add dev override to `appsettings.Development.json`
- [ ] Run `dotnet restore` to verify package resolution
- [ ] Run `dotnet build` to verify no compile errors

## Success Criteria

- `dotnet restore` succeeds with Confluent.Kafka resolved
- `dotnet build` compiles without errors
- KafkaSettings POCO follows same pattern as JwtSettings (SectionName const, public properties with defaults)
- appsettings.json contains valid KafkaSettings JSON section

## Risk Assessment

| Risk | Likelihood | Mitigation |
|------|-----------|------------|
| Confluent.Kafka version incompatible with .NET 8 | Low | v2.6.x supports .NET 8; pin version |
| Config section name collision | Very Low | Using unique `KafkaSettings` name |

## Next Steps

→ Phase 02: Domain Interfaces
