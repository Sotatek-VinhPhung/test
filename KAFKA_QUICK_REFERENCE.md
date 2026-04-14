# Kafka Producer-Consumer Quick Reference

## What Was Implemented

### Producer (AuthService.cs)
Publishes `UserLoginEvent` to Kafka when user logs in:

```csharp
var loginEvent = new UserLoginEvent
{
    UserId = user.Id,
    Email = user.Email,
    Role = user.Role.ToString(),
    LoginAt = DateTime.UtcNow
};

var kafkaMessage = new KafkaMessage<UserLoginEvent>
{
    Topic = "user-login-events",
    Key = user.Id.ToString(),
    Value = loginEvent,
    Headers = new Dictionary<string, string>
    {
        { "event-type", "user.login" },
        { "user-id", user.Id.ToString() }
    }
};

await _publisher.PublishAsync(kafkaMessage, cancellationToken);
```

### Consumer (UserLoginEventHandler.cs)
Processes login events from Kafka:

```csharp
public class UserLoginEventHandler : IMessageHandler<UserLoginEvent>
{
    public string Topic => "user-login-events";

    public async Task HandleAsync(UserLoginEvent message, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Processing login event for user {UserId} ({Email}) with role {Role}",
            message.UserId, message.Email, message.Role);
        
        // Add your business logic here:
        // - Update last login time
        // - Send notifications
        // - Update analytics
        // - Security monitoring
    }
}
```

## Step-by-Step Usage

### 1. Create a New Event Class
Create in `src/CleanArchitecture.Application/Auth/Events/YourEvent.cs`:

```csharp
public record YourEvent
{
    public required Guid Id { get; init; }
    public required string EventData { get; init; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
}
```

### 2. Create an Event Handler
Create in `src/CleanArchitecture.Application/Users/Services/YourEventHandler.cs`:

```csharp
public class YourEventHandler : IMessageHandler<YourEvent>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<YourEventHandler> _logger;

    public string Topic => "your-topic-name";

    public YourEventHandler(IUnitOfWork unitOfWork, ILogger<YourEventHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task HandleAsync(YourEvent message, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Processing event: {EventData}", message.EventData);
            // Add your business logic
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing event");
            throw; // Triggers retry/DLQ routing
        }
    }
}
```

### 3. Publish Events from Services
```csharp
var kafkaMessage = new KafkaMessage<YourEvent>
{
    Topic = "your-topic-name",
    Key = someId.ToString(), // For partitioning
    Value = yourEvent,
    Headers = new Dictionary<string, string>
    {
        { "event-type", "event.type.name" }
    }
};

await _publisher.PublishAsync(kafkaMessage, cancellationToken);
```

### 4. Handler is Automatically Discovered
No additional registration needed! The `AddKafkaServices()` in Program.cs scans and auto-discovers all `IMessageHandler<T>` implementations.

## Testing Locally

### Start Services
```powershell
docker-compose up -d kafka zookeeper
```

### Monitor Messages
```bash
# List topics
docker exec -it kafka kafka-topics --bootstrap-server localhost:9092 --list

# Consume messages
docker exec -it kafka kafka-console-consumer \
  --bootstrap-server localhost:9092 \
  --topic user-login-events \
  --from-beginning

# Consume DLQ messages
docker exec -it kafka kafka-console-consumer \
  --bootstrap-server localhost:9092 \
  --topic user-login-events.dlq \
  --from-beginning

# Create test topic
docker exec -it kafka kafka-topics \
  --bootstrap-server localhost:9092 \
  --topic my-topic \
  --create \
  --partitions 3 \
  --replication-factor 1

# Delete topic
docker exec -it kafka kafka-topics \
  --bootstrap-server localhost:9092 \
  --topic my-topic \
  --delete
```

## Configuration (appsettings.json)

```json
{
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
}
```

## Common Patterns

### Pattern 1: One-Way Event Publishing (Fire & Forget)
```csharp
// Publisher
await _publisher.PublishAsync(kafkaMessage, cancellationToken);

// Consumer
public async Task HandleAsync(MyEvent message, CancellationToken cancellationToken)
{
    // Process and don't respond
}
```

### Pattern 2: Batch Publishing
```csharp
var messages = new List<KafkaMessage<MyEvent>>
{
    new() { Topic = "topic", Value = event1 },
    new() { Topic = "topic", Value = event2 },
    new() { Topic = "topic", Value = event3 }
};

await _publisher.PublishBatchAsync(messages, cancellationToken);
```

### Pattern 3: Partitioned by Key (Ordered Processing)
```csharp
// Same user ID always goes to same partition → ordered
new KafkaMessage<UserLoginEvent>
{
    Topic = "user-login-events",
    Key = user.Id.ToString(), // ← This determines partition
    Value = loginEvent
}
```

### Pattern 4: Event with Metadata Headers
```csharp
new KafkaMessage<MyEvent>
{
    Topic = "my-topic",
    Value = myEvent,
    Headers = new Dictionary<string, string>
    {
        { "event-type", "my.event.type" },
        { "correlation-id", correlationId },
        { "user-id", userId },
        { "tenant-id", tenantId }
    }
}
```

## Error Handling

### Automatic Retry Logic
1. Handler throws exception
2. Retry #1 → Delay 1000ms → Retry
3. Retry #2 → Delay 1000ms → Retry
4. Retry #3 → Failed → Send to DLQ
5. Offset committed (move forward)

### Non-Recoverable Errors
- JSON deserialization errors → DLQ immediately
- Malformed messages → DLQ immediately

### Custom Error Handling in Handler
```csharp
public async Task HandleAsync(MyEvent message, CancellationToken cancellationToken)
{
    try
    {
        // Your code
    }
    catch (InvalidOperationException ex)
    {
        // Log and don't rethrow → Won't retry
        _logger.LogWarning(ex, "Non-transient error");
    }
    catch (TimeoutException ex)
    {
        // Rethrow → Will trigger retry/DLQ
        _logger.LogError(ex, "Transient error");
        throw;
    }
}
```

## Performance Tips

1. **Use appropriate partition key** to ensure message ordering
2. **Keep messages small** for better throughput
3. **Batch operations** when possible
4. **Use scoped dependencies** (DbContext available in handler scope)
5. **Set appropriate timeouts** based on workload
6. **Monitor DLQ** for failed messages

## Logging
Handlers have access to `ILogger<T>` through DI:
```csharp
_logger.LogInformation("Info message");
_logger.LogWarning("Warning message");
_logger.LogError(ex, "Error message");
_logger.LogDebug("Debug message");
```

Check application logs for consumer startup messages:
```
[INF] [Kafka] Discovered X message handler(s): ...
[INF] Starting consumer for topic X (handler: Y)
```

## Files Reference

| File | Purpose |
|------|---------|
| `UserLoginEvent.cs` | DTO for login event |
| `UserLoginEventHandler.cs` | Consumes login events |
| `AuthService.cs` | Publishes login events |
| `docker-compose.yml` | Kafka infrastructure |
| `Program.cs` | Registers Kafka services |
| `appsettings.json` | Kafka configuration |

## Build & Run

```powershell
# Build
dotnet build

# Run locally
dotnet run

# With docker
docker-compose up -d
dotnet run
```

## Troubleshooting

**No consumers found?**
- Verify handler class implements `IMessageHandler<T>`
- Check that handler assembly is passed to `AddKafkaServices()`
- Rebuild project

**Messages not being consumed?**
- Check Kafka is running: `docker-compose ps`
- Verify topic exists: `docker exec kafka kafka-topics --list`
- Check handler logs for errors

**DLQ messages accumulating?**
- Check handler logs for exception messages
- Verify database connectivity in handler scope
- Check if database migrations are applied

**How to reset consumer group?**
```bash
docker exec kafka kafka-consumer-groups \
  --bootstrap-server localhost:9092 \
  --group clean-architecture-group \
  --reset-offsets --to-earliest --all-topics \
  --execute
```
