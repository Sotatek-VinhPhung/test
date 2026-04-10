# Phase 02 — Domain Interfaces

**Priority:** High
**Status:** ✅ Complete
**Depends on:** Phase 01 (NuGet + Config)

## Context Links

- [Plan overview](plan.md)
- [Existing IRepository pattern](../../src/CleanArchitecture.Domain/Interfaces/IRepository.cs)
- [Domain .csproj](../../src/CleanArchitecture.Domain/CleanArchitecture.Domain.csproj)

## Overview

Define messaging abstractions in the Domain layer. These interfaces have zero dependency on Confluent.Kafka — they are pure contracts that Application layer can consume and Infrastructure layer implements.

## Key Insights

- Domain project has **no NuGet dependencies** — keep it that way. No Confluent.Kafka types leak here.
- Existing interface convention: file-scoped namespace, XML doc comments, CancellationToken with default.
- Use a `Messaging` subfolder under `Interfaces` to group all Kafka-related contracts.
- `KafkaMessage<T>` envelope lives in Domain as a simple record — no external dependencies.

## Requirements

### Functional
- `IKafkaPublisher` — publish typed messages to a topic
- `IMessageHandler<T>` — handle a single message type from a topic
- `IRequestReplyClient` — send a request and await a typed reply
- `KafkaMessage<T>` — envelope with Key, Value, Topic, Headers, Timestamp

### Non-Functional
- Zero external dependencies in Domain
- Each file under 50 lines
- Generic type parameters where appropriate

## Architecture

```
CleanArchitecture.Domain/
└── Interfaces/
    └── Messaging/
        ├── KafkaMessage.cs            (KafkaMessage<T> record)
        ├── IKafkaPublisher.cs         (IKafkaPublisher)
        ├── IMessageHandler.cs         (IMessageHandler<T>)
        └── IRequestReplyClient.cs     (IRequestReplyClient)
```

Application layer references these interfaces via constructor injection. Infrastructure provides implementations.

## Related Code Files

### Create
- `src/CleanArchitecture.Domain/Interfaces/Messaging/KafkaMessage.cs`
- `src/CleanArchitecture.Domain/Interfaces/Messaging/IKafkaPublisher.cs`
- `src/CleanArchitecture.Domain/Interfaces/Messaging/IMessageHandler.cs`
- `src/CleanArchitecture.Domain/Interfaces/Messaging/IRequestReplyClient.cs`

### No modifications to existing files

## Implementation Steps

### 1. Create KafkaMessage envelope

File: `src/CleanArchitecture.Domain/Interfaces/Messaging/KafkaMessage.cs`

```csharp
namespace CleanArchitecture.Domain.Interfaces.Messaging;

/// <summary>
/// Envelope for Kafka messages. Carries key, value, topic, and optional headers.
/// </summary>
public record KafkaMessage<T>
{
    /// <summary>Message key for partitioning. Null uses round-robin.</summary>
    public string? Key { get; init; }

    /// <summary>The strongly-typed message payload.</summary>
    public required T Value { get; init; }

    /// <summary>Target topic name.</summary>
    public required string Topic { get; init; }

    /// <summary>Optional headers (correlation IDs, metadata).</summary>
    public IDictionary<string, string>? Headers { get; init; }

    /// <summary>Message timestamp. Defaults to UtcNow if not set.</summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}
```

### 2. Create IKafkaPublisher interface

File: `src/CleanArchitecture.Domain/Interfaces/Messaging/IKafkaPublisher.cs`

```csharp
namespace CleanArchitecture.Domain.Interfaces.Messaging;

/// <summary>
/// Publishes strongly-typed messages to Kafka topics.
/// </summary>
public interface IKafkaPublisher
{
    /// <summary>Publish a single message to the specified topic.</summary>
    Task PublishAsync<T>(KafkaMessage<T> message, CancellationToken cancellationToken = default)
        where T : class;

    /// <summary>Publish a batch of messages to the specified topic.</summary>
    Task PublishBatchAsync<T>(IEnumerable<KafkaMessage<T>> messages, CancellationToken cancellationToken = default)
        where T : class;
}
```

### 3. Create IMessageHandler interface

File: `src/CleanArchitecture.Domain/Interfaces/Messaging/IMessageHandler.cs`

```csharp
namespace CleanArchitecture.Domain.Interfaces.Messaging;

/// <summary>
/// Handles messages of type <typeparamref name="T"/> consumed from a Kafka topic.
/// Implement one handler per topic/message type and register via DI.
/// </summary>
public interface IMessageHandler<in T> where T : class
{
    /// <summary>The topic this handler subscribes to.</summary>
    string Topic { get; }

    /// <summary>Process a single message.</summary>
    Task HandleAsync(T message, CancellationToken cancellationToken = default);
}
```

### 4. Create IRequestReplyClient interface

File: `src/CleanArchitecture.Domain/Interfaces/Messaging/IRequestReplyClient.cs`

```csharp
namespace CleanArchitecture.Domain.Interfaces.Messaging;

/// <summary>
/// Synchronous request/reply over Kafka. Sends a request to a topic
/// and awaits a correlated reply on a reply topic.
/// </summary>
public interface IRequestReplyClient
{
    /// <summary>
    /// Send a request and await a typed reply.
    /// </summary>
    /// <typeparam name="TRequest">Request payload type.</typeparam>
    /// <typeparam name="TReply">Expected reply payload type.</typeparam>
    /// <param name="topic">Topic to send the request to.</param>
    /// <param name="request">The request payload.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The deserialized reply.</returns>
    Task<TReply> SendAsync<TRequest, TReply>(
        string topic,
        TRequest request,
        CancellationToken cancellationToken = default)
        where TRequest : class
        where TReply : class;
}
```

## Todo List

- [ ] Create `Interfaces/Messaging/` directory in Domain project
- [ ] Create `KafkaMessage.cs` — KafkaMessage&lt;T&gt; record
- [ ] Create `IKafkaPublisher.cs` — IKafkaPublisher interface
- [ ] Create `IMessageHandler.cs` — IMessageHandler&lt;T&gt; interface
- [ ] Create `IRequestReplyClient.cs` — IRequestReplyClient interface
- [ ] Run `dotnet build` on Domain project — no external dependencies
- [ ] Verify Domain .csproj has NO new PackageReferences added

## Success Criteria

- Domain project builds with zero warnings
- Domain .csproj has no new NuGet references
- All interfaces use CancellationToken with default value
- All types are in `CleanArchitecture.Domain.Interfaces.Messaging` namespace
- Each file < 50 lines

## Risk Assessment

| Risk | Likelihood | Mitigation |
|------|-----------|------------|
| Leaking Confluent types into Domain | Low | No Confluent.Kafka reference in Domain .csproj; code review check |
| Over-abstracting (YAGNI) | Medium | Only 3 interfaces + 1 envelope — minimal surface |

## Security Considerations

- KafkaMessage Headers dict can carry correlation IDs but should never carry secrets
- Sensitive data in message payloads should be encrypted at the Application layer before publishing

## Next Steps

→ Phase 03: Infrastructure Kafka Implementations
