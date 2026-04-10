# Phase 03 — Infrastructure Kafka Implementations

**Priority:** High
**Status:** ✅ Complete
**Depends on:** Phase 01 (NuGet), Phase 02 (Domain Interfaces)

## Context Links

- [Plan overview](plan.md)
- [Phase 01 — KafkaSettings](phase-01-nuget-and-config-setup.md)
- [Phase 02 — Domain Interfaces](phase-02-domain-interfaces.md)
- [Existing DI pattern](../../src/CleanArchitecture.Infrastructure/DependencyInjection.cs)
- [Existing ExceptionHandlingMiddleware](../../src/CleanArchitecture.Api/Middleware/ExceptionHandlingMiddleware.cs)

## Overview

Implement all Kafka infrastructure: shared client wrapper, publisher, hosted consumer service, dead letter queue handler, and request/reply client. All in `Infrastructure/Messaging/` folder.

## Key Insights

- Use `IProducer<string, string>` and `IConsumer<string, string>` from Confluent.Kafka — serialize to JSON string.
- KafkaClient is a singleton that builds and caches producer/consumer instances.
- Consumer runs as `BackgroundService` (inherits `IHostedService`), one per registered handler.
- DLQ handler is a simple publisher wrapper that re-routes failed messages to `{topic}.dlq`.
- Request/Reply uses correlation ID header + `TaskCompletionSource` pending map.
- All classes use `ILogger<T>` (Serilog) and `IOptions<KafkaSettings>`.
- Keep each file ≤ 200 lines. The consumer is the largest — split handler dispatch logic if needed.

## Requirements

### Functional
- **KafkaClient**: Build and cache `IProducer` / `IConsumer` instances from KafkaSettings
- **KafkaPublisher**: Serialize `KafkaMessage<T>` → JSON, send via producer, set headers
- **KafkaConsumerService**: Background worker per handler, subscribe to topic, deserialize, dispatch to `IMessageHandler<T>`, commit offsets manually
- **KafkaDlqHandler**: On handler failure after max retries, publish original message + error info to DLQ topic
- **KafkaRequestReplyClient**: Publish request with correlation ID, consume reply topic filtered by correlation ID, return deserialized reply with timeout

### Non-Functional
- Graceful shutdown via CancellationToken / StoppingToken
- Manual offset commit (no auto-commit)
- System.Text.Json for serialization
- Each file ≤ 200 lines
- Thread-safe producer (singleton), consumer per handler (scoped to BackgroundService)

## Architecture

```
Infrastructure/Messaging/
├── KafkaSettings.cs                (Phase 01 — already created)
├── KafkaClient.cs                  (Singleton — builds producer/consumer)
├── KafkaPublisher.cs               (IKafkaPublisher impl)
├── KafkaConsumerService.cs         (BackgroundService — one per handler)
├── KafkaDlqHandler.cs              (DLQ routing)
├── KafkaRequestReplyClient.cs      (IRequestReplyClient impl)
└── KafkaServiceRegistration.cs     (DI extension — Phase 04 uses this)
```

### Data Flow

```
[Publisher] → Kafka Topic → [ConsumerService] → [IMessageHandler<T>]
                                    ↓ (on failure after retries)
                              [DlqHandler] → {topic}.dlq

[RequestReplyClient] → Request Topic → [External Service]
                    ← Reply Topic (filtered by CorrelationId) ←
```

## Related Code Files

### Create
1. `src/CleanArchitecture.Infrastructure/Messaging/KafkaClient.cs`
2. `src/CleanArchitecture.Infrastructure/Messaging/KafkaPublisher.cs`
3. `src/CleanArchitecture.Infrastructure/Messaging/KafkaConsumerService.cs`
4. `src/CleanArchitecture.Infrastructure/Messaging/KafkaDlqHandler.cs`
5. `src/CleanArchitecture.Infrastructure/Messaging/KafkaRequestReplyClient.cs`

### Modify
- None (DI registration is Phase 04)

## Implementation Steps

### 1. KafkaClient — Connection & Instance Management

File: `src/CleanArchitecture.Infrastructure/Messaging/KafkaClient.cs`

**Responsibilities:**
- Registered as **Singleton**
- Lazy-builds one `IProducer<string, string>` shared across the app
- Provides a factory method to build `IConsumer<string, string>` (one per consumer service instance)
- Implements `IDisposable` to flush/dispose producer on shutdown

```csharp
namespace CleanArchitecture.Infrastructure.Messaging;

public class KafkaClient : IDisposable
{
    private readonly KafkaSettings _settings;
    private readonly ILogger<KafkaClient> _logger;
    private IProducer<string, string>? _producer;
    private readonly object _lock = new();

    // Constructor: IOptions<KafkaSettings>, ILogger<KafkaClient>

    // GetProducer(): lazy-init with lock, ProducerConfig from _settings
    //   - BootstrapServers, Acks = Acks.All, MessageTimeoutMs
    //   - SetErrorHandler to log errors

    // BuildConsumer(): creates new IConsumer<string, string> each call
    //   - ConsumerConfig: BootstrapServers, GroupId, AutoOffsetReset, EnableAutoCommit=false
    //   - SetErrorHandler, SetPartitionsAssignedHandler (log)

    // Dispose(): flush + dispose producer
}
```

**Key config mapping:**
| KafkaSettings Property | Confluent Config |
|----------------------|-----------------|
| BootstrapServers | BootstrapServers |
| GroupId | GroupId |
| EnableAutoCommit | EnableAutoCommit |
| AutoOffsetReset | AutoOffsetReset (parsed to enum) |
| RequestTimeoutMs | MessageTimeoutMs (producer) |

### 2. KafkaPublisher — Message Publishing

File: `src/CleanArchitecture.Infrastructure/Messaging/KafkaPublisher.cs`

**Responsibilities:**
- Implements `IKafkaPublisher` from Domain
- Uses `KafkaClient.GetProducer()` to get the shared producer
- Serializes `KafkaMessage<T>.Value` to JSON string via `System.Text.Json`
- Maps `KafkaMessage<T>.Key` → Kafka message key
- Maps `KafkaMessage<T>.Headers` → `Confluent.Kafka.Headers`
- `PublishBatchAsync` iterates and produces each message, then flushes

```csharp
namespace CleanArchitecture.Infrastructure.Messaging;

public class KafkaPublisher : IKafkaPublisher
{
    private readonly KafkaClient _client;
    private readonly ILogger<KafkaPublisher> _logger;

    // PublishAsync<T>:
    //   1. Serialize message.Value to JSON
    //   2. Build Message<string, string> { Key, Value, Headers, Timestamp }
    //   3. ProduceAsync to message.Topic
    //   4. Log delivery result (partition, offset)

    // PublishBatchAsync<T>:
    //   1. Loop through messages, call Produce (fire-and-forget callback)
    //   2. Flush producer after batch
    //   3. Log batch count
}
```

**Header mapping helper** (private method):
- Iterate `KafkaMessage.Headers` dictionary
- Convert each to `Confluent.Kafka.Headers.Add(key, Encoding.UTF8.GetBytes(value))`

### 3. KafkaConsumerService — Background Consumer Worker

File: `src/CleanArchitecture.Infrastructure/Messaging/KafkaConsumerService.cs`

**Responsibilities:**
- Extends `BackgroundService`
- One instance per registered `IMessageHandler<T>` (created at DI time via generic factory)
- Subscribes to handler's `Topic`, polls in a loop, deserializes JSON → T, calls `HandleAsync`
- Manual offset commit after successful handling
- On failure: retry up to `MaxRetryAttempts`, then delegate to `KafkaDlqHandler`
- Graceful shutdown: `stoppingToken` breaks the poll loop, consumer closes

```csharp
namespace CleanArchitecture.Infrastructure.Messaging;

public class KafkaConsumerService<T> : BackgroundService where T : class
{
    private readonly KafkaClient _client;
    private readonly IMessageHandler<T> _handler;
    private readonly KafkaDlqHandler _dlqHandler;
    private readonly KafkaSettings _settings;
    private readonly ILogger<KafkaConsumerService<T>> _logger;

    // ExecuteAsync(CancellationToken stoppingToken):
    //   1. var consumer = _client.BuildConsumer()
    //   2. consumer.Subscribe(_handler.Topic)
    //   3. while (!stoppingToken.IsCancellationRequested)
    //      a. var result = consumer.Consume(stoppingToken)
    //      b. Deserialize result.Message.Value → T
    //      c. Call HandleWithRetryAsync(message, result, consumer)
    //   4. consumer.Close() in finally block

    // HandleWithRetryAsync:
    //   - for (attempt = 1..MaxRetryAttempts)
    //     try { await _handler.HandleAsync(message, ct); consumer.Commit(result); return; }
    //     catch { log warning, delay RetryDelayMs }
    //   - After max retries: await _dlqHandler.SendToDlqAsync(topic, result, exception)
    //   - Commit offset to move past the failed message
}
```

**Important:** Consumer runs on a dedicated thread via `Task.Run` in `ExecuteAsync` to avoid blocking the host.

### 4. KafkaDlqHandler — Dead Letter Queue

File: `src/CleanArchitecture.Infrastructure/Messaging/KafkaDlqHandler.cs`

**Responsibilities:**
- Takes a failed consume result + exception
- Publishes original message to `{originalTopic}{DlqTopicSuffix}` (e.g. `orders.dlq`)
- Adds error metadata headers: `dlq-error`, `dlq-original-topic`, `dlq-timestamp`, `dlq-retry-count`

```csharp
namespace CleanArchitecture.Infrastructure.Messaging;

public class KafkaDlqHandler
{
    private readonly KafkaClient _client;
    private readonly KafkaSettings _settings;
    private readonly ILogger<KafkaDlqHandler> _logger;

    // SendToDlqAsync(string originalTopic, ConsumeResult<string,string> result, Exception ex):
    //   1. Build DLQ topic name: originalTopic + _settings.DlqTopicSuffix
    //   2. Clone original message headers, add error headers
    //   3. Produce to DLQ topic with original key + value
    //   4. Log error with topic, key, exception message
}
```

### 5. KafkaRequestReplyClient — Request/Reply Pattern

File: `src/CleanArchitecture.Infrastructure/Messaging/KafkaRequestReplyClient.cs`

**Responsibilities:**
- Implements `IRequestReplyClient` from Domain
- Generates unique correlation ID per request
- Publishes request to specified topic with `correlation-id` header
- Consumes from `ReplyTopic`, filters by correlation ID
- Uses `ConcurrentDictionary<string, TaskCompletionSource<string>>` for pending requests
- Timeout via `CancellationTokenSource` combining caller token + timeout
- Background consumer loop started once on first call (lazy init)

```csharp
namespace CleanArchitecture.Infrastructure.Messaging;

public class KafkaRequestReplyClient : IRequestReplyClient, IDisposable
{
    private readonly KafkaClient _client;
    private readonly KafkaSettings _settings;
    private readonly ILogger<KafkaRequestReplyClient> _logger;
    private readonly ConcurrentDictionary<string, TaskCompletionSource<string>> _pending = new();
    private IConsumer<string, string>? _replyConsumer;
    private Task? _replyListenerTask;
    private CancellationTokenSource? _cts;
    private readonly object _initLock = new();

    // SendAsync<TRequest, TReply>:
    //   1. EnsureReplyListenerStarted()
    //   2. var correlationId = Guid.NewGuid().ToString()
    //   3. var tcs = new TaskCompletionSource<string>()
    //   4. _pending[correlationId] = tcs
    //   5. Publish request with correlation-id + reply-topic headers
    //   6. var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct)
    //      timeoutCts.CancelAfter(RequestReplyTimeoutMs)
    //   7. Register cancellation → tcs.TrySetCanceled()
    //   8. var json = await tcs.Task
    //   9. Deserialize json → TReply, return
    //   10. finally: _pending.TryRemove(correlationId)

    // EnsureReplyListenerStarted(): lazy start background consumer loop
    // ReplyListenerLoop(): consume reply topic, extract correlation-id,
    //   if _pending.TryGetValue → tcs.TrySetResult(message.Value)

    // Dispose: cancel CTS, close consumer, complete pending with cancellation
}
```

**Correlation flow:**
```
Producer → [Request Topic] message + header{correlation-id: "abc-123", reply-topic: "reply-topic"}
Consumer ← [Reply Topic] message + header{correlation-id: "abc-123"}
         → Match via _pending["abc-123"].TrySetResult(value)
```

## Todo List

- [ ] Create `KafkaClient.cs` — singleton producer/consumer factory
- [ ] Create `KafkaPublisher.cs` — IKafkaPublisher implementation
- [ ] Create `KafkaConsumerService.cs` — BackgroundService per handler
- [ ] Create `KafkaDlqHandler.cs` — dead letter queue routing
- [ ] Create `KafkaRequestReplyClient.cs` — IRequestReplyClient implementation
- [ ] Verify each file ≤ 200 lines
- [ ] Run `dotnet build` on Infrastructure project
- [ ] Verify all Domain interfaces are implemented

## Success Criteria

- Infrastructure project builds with zero errors
- `KafkaPublisher` implements `IKafkaPublisher`
- `KafkaRequestReplyClient` implements `IRequestReplyClient`
- `KafkaConsumerService<T>` extends `BackgroundService`
- All classes use `ILogger<T>` and `IOptions<KafkaSettings>`
- Manual offset commit — no auto-commit
- DLQ handler adds error metadata headers
- Request/Reply uses correlation ID for matching
- Each file ≤ 200 lines
- Graceful shutdown via CancellationToken / StoppingToken

## Risk Assessment

| Risk | Likelihood | Mitigation |
|------|-----------|------------|
| Consumer blocking host thread | Medium | Wrap poll loop in `Task.Run` |
| Reply listener memory leak (pending requests) | Low | Timeout + finally remove from dict |
| Serialization mismatch (producer vs consumer) | Low | Both use System.Text.Json with same options |
| Topic auto-creation disabled on broker | Medium | Document required topics in README; log clear error |

## Security Considerations

- No credentials in source code — bootstrap servers come from config/env vars
- DLQ messages retain original payload — ensure no sensitive data amplification
- Correlation IDs are random GUIDs — not guessable
- If Kafka requires SASL/SSL, extend KafkaSettings with SecurityProtocol, SaslMechanism, SaslUsername, SaslPassword (out of scope for initial implementation, but settings POCO is extensible)

## Next Steps

→ Phase 04: DI Registration + API Wiring
