# DI Scoping Violation Fix - KafkaConsumerService<T>

## Problem
**Error**: `System.InvalidOperationException: Cannot resolve scoped service 'CleanArchitecture.Domain.Interfaces.Messaging.IMessageHandler`1[CleanArchitecture.Application.Auth.Events.UserLoginEvent]' from root provider.`

**Root Cause**: 
- `KafkaConsumerService<T>` was registered as **Singleton** (IHostedService)
- Its constructor tried to inject `IMessageHandler<T>` which is **Scoped**
- ASP.NET Core DI rules forbid Singleton services from capturing Scoped dependencies

This is a **lifetime violation** — a Singleton that directly depends on a Scoped service would keep the Scoped instance alive permanently, violating scoping semantics.

---

## Solution

### Changes to KafkaConsumerService<T>

**1. Remove handler from constructor parameters**
```csharp
// BEFORE (WRONG)
public KafkaConsumerService(
    KafkaClient client,
    IServiceScopeFactory scopeFactory,
    IMessageHandler<T> handler,  // ← SCOPED — causes violation
    KafkaDlqHandler dlqHandler,
    IOptions<KafkaSettings> settings,
    ILogger<KafkaConsumerService<T>> logger)
{
    _topic = handler.Topic;  // ← Singleton captures Scoped service
}

// AFTER (CORRECT)
public KafkaConsumerService(
    KafkaClient client,
    IServiceScopeFactory scopeFactory,
    KafkaDlqHandler dlqHandler,
    IOptions<KafkaSettings> settings,
    ILogger<KafkaConsumerService<T>> logger)
{
    // Topic resolved lazily on first access (see GetTopic method)
}
```

**2. Lazy-resolve topic on first access**
```csharp
private string? _topic;

/// <summary>
/// Lazily resolves the handler topic within a DI scope.
/// Called once at consumer startup, then cached in _topic.
/// </summary>
private string GetTopic()
{
    if (_topic is not null)
        return _topic;

    using var scope = _scopeFactory.CreateScope();
    var handler = scope.ServiceProvider.GetRequiredService<IMessageHandler<T>>();
    return _topic = handler.Topic;
}
```

**3. Update all usages of `_topic` to call `GetTopic()`**
```csharp
// In ConsumeLoop:
var topic = GetTopic();
_logger.LogInformation("Starting consumer for topic {Topic}", topic);
consumer.Subscribe(topic);

// In HandleWithRetryAsync:
var topic = GetTopic();
_logger.LogError("Deserialization error on {Topic}", topic);
await _dlqHandler.SendToDlqAsync(topic, result, ex, stoppingToken);

// In finally block:
_logger.LogInformation("Consumer stopped for topic {Topic}", GetTopic());
```

---

## Why This Works

### The Pattern: IServiceScopeFactory
```csharp
// ✅ Correct pattern — used in ConsumeLoop:
using var scope = _scopeFactory.CreateScope();
var handler = scope.ServiceProvider.GetRequiredService<IMessageHandler<T>>();
await handler.HandleAsync(message, stoppingToken);
```

**Key Points**:
1. **Deferred Resolution**: Handler is NOT resolved at Singleton instantiation time
2. **Per-Message Scope**: Fresh scope created for each message (true scoped lifetime)
3. **Explicit Lifetime Management**: Scope is disposed immediately after use
4. **No Capture**: Singleton never "holds onto" a Scoped instance

### DI Container View
```
Registration:
✓ IMessageHandler<UserLoginEvent> → Scoped (supports DbContext dependency)
✓ KafkaConsumerService<UserLoginEvent> → Singleton (no scoped dependencies)
✓ IServiceScopeFactory → Built-in, always available

At Runtime:
1. Singleton KafkaConsumerService<UserLoginEvent> instantiated (no handler)
2. ConsumeLoop calls GetTopic() → creates temporary scope → resolves handler → gets topic → disposes scope
3. Per message: creates scope → resolves handler → processes message → disposes scope
4. Singleton never holds reference to Scoped instance
```

---

## Verification

✅ Build successful
✅ No compile-time errors
✅ No DI validation errors

Next steps:
1. Run application: `dotnet run`
2. Expected log: `[INF] [Kafka] Discovered 1 message handler(s): UserLoginEventHandler`
3. Test E2E: Call login endpoint → verify Kafka message published → verify handler processes event

---

## Lessons Learned

1. **Singleton + Scoped = Illegal**: Background services must use factory pattern for scoped dependencies
2. **IServiceScopeFactory**: The standard solution for Singleton-accessing-Scoped pattern
3. **Lazy Resolution**: Get data from scoped service once, cache the data (not the instance)
4. **Per-Operation Scopes**: Create new scope for each message/operation within background service
