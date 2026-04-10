# Phase 04 — DI Registration + API Wiring

**Priority:** High
**Status:** ✅ Complete
**Depends on:** Phase 01, 02, 03

## Context Links

- [Plan overview](plan.md)
- [Infrastructure DI pattern](../../src/CleanArchitecture.Infrastructure/DependencyInjection.cs)
- [Application DI pattern](../../src/CleanArchitecture.Application/DependencyInjection.cs)
- [Program.cs](../../src/CleanArchitecture.Api/Program.cs)

## Overview

Wire all Kafka services into the DI container following the existing extension method pattern. Create `AddKafkaServices()` in Infrastructure and call it from `Program.cs`. Auto-discover `IMessageHandler<T>` implementations and register a `KafkaConsumerService<T>` hosted service per handler.

## Key Insights

- Existing pattern: `services.AddInfrastructureServices(configuration)` in Infrastructure's `DependencyInjection.cs`.
- **Option A (chosen):** Add a separate `AddKafkaServices(configuration)` extension method in a new file `kafka-service-registration.cs` inside `Infrastructure/Messaging/`. This keeps Kafka self-contained and doesn't bloat the existing DI file.
- **Option B (rejected):** Add Kafka registrations inside existing `AddInfrastructureServices()` — rejected because it couples Kafka to Infrastructure DI, making it harder to disable Kafka.
- Handler auto-discovery: scan assembly for `IMessageHandler<T>` implementations, register each as its closed generic, then register `KafkaConsumerService<T>` as hosted service.

## Requirements

### Functional
- `AddKafkaServices(IConfiguration)` extension method registers:
  - `KafkaSettings` via `IOptions<KafkaSettings>`
  - `KafkaClient` as Singleton
  - `KafkaPublisher` as Singleton (uses singleton producer)
  - `KafkaDlqHandler` as Singleton
  - `KafkaRequestReplyClient` as Singleton
  - All `IMessageHandler<T>` implementations as Singleton
  - One `KafkaConsumerService<T>` hosted service per handler
- `Program.cs` calls `builder.Services.AddKafkaServices(builder.Configuration)`

### Non-Functional
- Handler auto-discovery via assembly scanning (no manual registration per handler)
- Extension method file ≤ 100 lines
- Minimal changes to Program.cs (one line)

## Architecture

```
Program.cs
  └── builder.Services.AddKafkaServices(builder.Configuration)
        ├── Configure<KafkaSettings>
        ├── AddSingleton<KafkaClient>
        ├── AddSingleton<IKafkaPublisher, KafkaPublisher>
        ├── AddSingleton<KafkaDlqHandler>
        ├── AddSingleton<IRequestReplyClient, KafkaRequestReplyClient>
        ├── [auto-scan] AddSingleton<IMessageHandler<OrderCreated>, OrderCreatedHandler>
        └── [auto-scan] AddHostedService<KafkaConsumerService<OrderCreated>>
```

## Related Code Files

### Create
- `src/CleanArchitecture.Infrastructure/Messaging/KafkaServiceRegistration.cs`

### Modify
- `src/CleanArchitecture.Api/Program.cs` — add `AddKafkaServices` call

## Implementation Steps

### 1. Create Kafka Service Registration Extension

File: `src/CleanArchitecture.Infrastructure/Messaging/KafkaServiceRegistration.cs`

```csharp
using System.Reflection;
using CleanArchitecture.Domain.Interfaces.Messaging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CleanArchitecture.Infrastructure.Messaging;

public static class KafkaServiceRegistration
{
    /// <summary>
    /// Registers all Kafka services: client, publisher, consumer, DLQ, request/reply.
    /// Auto-discovers IMessageHandler<T> implementations in the calling assembly.
    /// </summary>
    public static IServiceCollection AddKafkaServices(
        this IServiceCollection services,
        IConfiguration configuration,
        params Assembly[] handlerAssemblies)
    {
        // 1. Bind KafkaSettings
        services.Configure<KafkaSettings>(
            configuration.GetSection(KafkaSettings.SectionName));

        // 2. Core services
        services.AddSingleton<KafkaClient>();
        services.AddSingleton<IKafkaPublisher, KafkaPublisher>();
        services.AddSingleton<KafkaDlqHandler>();
        services.AddSingleton<IRequestReplyClient, KafkaRequestReplyClient>();

        // 3. Auto-discover and register handlers
        var assemblies = handlerAssemblies.Length > 0
            ? handlerAssemblies
            : new[] { Assembly.GetCallingAssembly() };

        foreach (var assembly in assemblies)
        {
            RegisterHandlers(services, assembly);
        }

        return services;
    }

    private static void RegisterHandlers(IServiceCollection services, Assembly assembly)
    {
        // Find all types implementing IMessageHandler<T>
        var handlerTypes = assembly.GetTypes()
            .Where(t => t is { IsAbstract: false, IsInterface: false })
            .Where(t => t.GetInterfaces().Any(i =>
                i.IsGenericType &&
                i.GetGenericTypeDefinition() == typeof(IMessageHandler<>)))
            .ToList();

        foreach (var handlerType in handlerTypes)
        {
            // Get IMessageHandler<T> interface and extract T
            var handlerInterface = handlerType.GetInterfaces()
                .First(i => i.IsGenericType &&
                            i.GetGenericTypeDefinition() == typeof(IMessageHandler<>));

            var messageType = handlerInterface.GetGenericArguments()[0];

            // Register handler: IMessageHandler<T> → HandlerImpl
            services.AddSingleton(handlerInterface, handlerType);

            // Register hosted consumer service: KafkaConsumerService<T>
            var consumerServiceType = typeof(KafkaConsumerService<>).MakeGenericType(messageType);
            services.AddSingleton(typeof(IHostedService), sp =>
                ActivatorUtilities.CreateInstance(sp, consumerServiceType));
        }
    }
}
```

**Notes on `handlerAssemblies` param:**
- Defaults to calling assembly (Infrastructure) — covers handlers defined there
- Pass `typeof(SomeHandler).Assembly` to scan Application or other assemblies
- Typical call: `services.AddKafkaServices(config)` or `services.AddKafkaServices(config, typeof(MyHandler).Assembly)`

### 2. Update Program.cs

Add one line after `AddInfrastructureServices`:

```csharp
// Before (existing):
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);

// After (add this line):
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);
builder.Services.AddKafkaServices(builder.Configuration);
```

Required using:
```csharp
using CleanArchitecture.Infrastructure.Messaging;
```

### 3. Verify DI Resolution

After wiring, the following should resolve from the container:
- `IKafkaPublisher` → `KafkaPublisher`
- `IRequestReplyClient` → `KafkaRequestReplyClient`
- `IMessageHandler<T>` → Auto-registered handler (one per topic)
- `IHostedService` → One `KafkaConsumerService<T>` per handler (starts on app boot)

## Todo List

- [ ] Create `KafkaServiceRegistration.cs` with `AddKafkaServices` extension method
- [ ] Add handler auto-discovery via assembly scanning
- [ ] Add `using CleanArchitecture.Infrastructure.Messaging;` to Program.cs
- [ ] Add `builder.Services.AddKafkaServices(builder.Configuration);` to Program.cs
- [ ] Run `dotnet build` — full solution
- [ ] Verify DI resolves all Kafka services (integration test or manual)

## Success Criteria

- Full solution builds with zero errors
- `AddKafkaServices()` follows same pattern as `AddInfrastructureServices()` (extension method on IServiceCollection)
- Handler auto-discovery works without manual registration per handler
- One `KafkaConsumerService<T>` hosted service created per `IMessageHandler<T>` found
- Program.cs has only ONE new line added (plus using)
- `KafkaServiceRegistration.cs` ≤ 100 lines

## Risk Assessment

| Risk | Likelihood | Mitigation |
|------|-----------|------------|
| Assembly scanning misses handlers in other assemblies | Medium | `handlerAssemblies` param allows explicit assembly list |
| Hosted service fails to start (no Kafka broker) | Medium | KafkaClient logs error and retries; consumer catches broker-down gracefully |
| DI lifetime conflicts (singleton consumer needing scoped handler) | Low | All Kafka services are singleton; handlers that need scoped deps must use IServiceScopeFactory |

## Security Considerations

- KafkaSettings bound from config — no secrets in code
- For production: use environment variables or Azure Key Vault for broker credentials
- Consider adding a `KafkaSettings.Enabled` bool to completely skip Kafka registration when not needed

## Next Steps

After all 4 phases complete:
1. Create a sample `IMessageHandler<T>` implementation to validate end-to-end
2. Add integration tests with Testcontainers (Kafka container)
3. Update `docs/system-architecture.md` with Kafka messaging diagrams
4. Update `docs/project-changelog.md` with Kafka integration entry
