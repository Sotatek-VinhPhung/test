# Kafka Producer-Consumer Flow Diagram

## Complete Message Flow

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                        PRODUCER SIDE (AuthService)                          │
└─────────────────────────────────────────────────────────────────────────────┘

    Client Request (Login)
            ↓
    ┌──────────────────────────┐
    │   AuthController         │
    │   POST /auth/login       │
    └──────────┬───────────────┘
               ↓
    ┌──────────────────────────────────────────┐
    │   AuthService.LoginAsync()               │
    │  • Validate credentials                  │
    │  • Generate tokens                       │
    │  • Save refresh token                    │
    └──────────┬───────────────────────────────┘
               ↓
    ┌──────────────────────────────────────────┐
    │   Create UserLoginEvent                  │
    │  • UserId: user.Id                       │
    │  • Email: user.Email                     │
    │  • Role: user.Role                       │
    │  • LoginAt: DateTime.UtcNow              │
    └──────────┬───────────────────────────────┘
               ↓
    ┌──────────────────────────────────────────┐
    │   Create KafkaMessage<UserLoginEvent>    │
    │  • Topic: "user-login-events"            │
    │  • Key: user.Id.ToString()               │
    │  • Value: UserLoginEvent instance        │
    │  • Headers: { event-type, user-id }     │
    └──────────┬───────────────────────────────┘
               ↓
    ┌──────────────────────────────────────────┐
    │   IKafkaPublisher.PublishAsync()         │
    │   (KafkaPublisher implementation)        │
    └──────────┬───────────────────────────────┘
               ↓
    ┌──────────────────────────────────────────┐
    │   Confluent.Kafka Producer               │
    │   Serialize to JSON                      │
    │   Send to Kafka Broker                   │
    └──────────┬───────────────────────────────┘
               ↓
    ┌──────────────────────────────────────────┐
    │   KAFKA TOPIC: "user-login-events"       │
    │   Partition: Based on Key (UserId)       │
    │   Message stored with offset             │
    └──────────────────────────────────────────┘


┌─────────────────────────────────────────────────────────────────────────────┐
│                    TRANSPORT LAYER (Kafka Broker)                           │
└─────────────────────────────────────────────────────────────────────────────┘

    Zookeeper (Coordination)
           ↕
    Kafka Broker (localhost:9092)
        ↓
    Topic: user-login-events
    ├─ Partition 0 [msgs for userId-x...]
    ├─ Partition 1 [msgs for userId-y...]
    └─ Partition N [msgs for userId-z...]


┌─────────────────────────────────────────────────────────────────────────────┐
│                    CONSUMER SIDE (UserService)                              │
└─────────────────────────────────────────────────────────────────────────────┘

    Application Startup
            ↓
    ┌────────────────────────────────────────────┐
    │   Program.cs                               │
    │   builder.Services.AddKafkaServices(...)   │
    │   Scans Application assembly               │
    └──────────┬─────────────────────────────────┘
               ↓
    ┌────────────────────────────────────────────┐
    │   KafkaServiceRegistration.RegisterHandlers│
    │   Discovers IMessageHandler<T> impls       │
    └──────────┬─────────────────────────────────┘
               ↓
    ┌────────────────────────────────────────────┐
    │   Found: UserLoginEventHandler             │
    │  • Topic: "user-login-events"              │
    │  • MessageType: UserLoginEvent             │
    └──────────┬─────────────────────────────────┘
               ↓
    ┌──────────────────────────────────────────────────────┐
    │   Register as Scoped Service                         │
    │   IMessageHandler<UserLoginEvent> → 
    │   UserLoginEventHandler                              │
    └──────────┬───────────────────────────────────────────┘
               ↓
    ┌──────────────────────────────────────────────────────┐
    │   Register Hosted Background Service                 │
    │   KafkaConsumerService<UserLoginEvent>               │
    └──────────┬───────────────────────────────────────────┘
               ↓
    ┌──────────────────────────────────────────────────────┐
    │   IHostedService.StartAsync()                        │
    │   Starts consumer loop on dedicated thread           │
    └──────────┬───────────────────────────────────────────┘
               ↓
    ┌──────────────────────────────────────────────────────┐
    │   KafkaConsumerService<UserLoginEvent>.ConsumeLoop() │
    │  • Build consumer with group: "clean-arch-group"    │
    │  • Subscribe to topic: "user-login-events"          │
    │  • Listen for messages...                           │
    └──────────┬───────────────────────────────────────────┘
               ↓ (When message arrives)
    ┌──────────────────────────────────────────────────────┐
    │   Deserialize JSON → UserLoginEvent                 │
    │   Success? Continue : Send to DLQ immediately       │
    └──────────┬───────────────────────────────────────────┘
               ↓
    ┌──────────────────────────────────────────────────────┐
    │   Create DI Scope                                    │
    │   Resolve IMessageHandler<UserLoginEvent>           │
    │   (Gets UserLoginEventHandler instance)             │
    └──────────┬───────────────────────────────────────────┘
               ↓
    ┌──────────────────────────────────────────────────────┐
    │   UserLoginEventHandler.HandleAsync(message)         │
    │  • Log: "Processing login event for user {UserId}"  │
    │  • Get user from database                           │
    │  • Log: "Successfully handled login event"          │
    │  • Success? Commit offset : Retry                   │
    └──────────┬───────────────────────────────────────────┘
               ↓ (Success)
    ┌──────────────────────────────────────────────────────┐
    │   consumer.Commit(result)                            │
    │   Offset committed to Kafka broker                  │
    │   Message considered processed                      │
    └──────────┬───────────────────────────────────────────┘
               ↓
    ┌──────────────────────────────────────────────────────┐
    │   Wait for next message or timeout                  │
    │   Loop continues...                                 │
    └──────────────────────────────────────────────────────┘

    ❌ (If error during handler execution)
               ↓
    ┌──────────────────────────────────────────────────────┐
    │   Retry Logic (MaxRetryAttempts)                     │
    │   Attempt 1 → Delay 1000ms                          │
    │   Attempt 2 → Delay 1000ms                          │
    │   Attempt 3 → Failed                                │
    └──────────┬───────────────────────────────────────────┘
               ↓
    ┌──────────────────────────────────────────────────────┐
    │   Send to DLQ Topic: "user-login-events.dlq"         │
    │   Log error with context                            │
    │   Commit offset (move past failed message)          │
    └──────────────────────────────────────────────────────┘
```

## Concurrency Model

```
Application Host
    ↓
IHostedService instances:
    ├─ KafkaConsumerService<UserLoginEvent>
    │   └─ Dedicated Thread (Consume Loop)
    │       ├─ consumer.Consume() [Blocking]
    │       └─ HandleWithRetryAsync()
    │           └─ Creates DI Scope per message
    │               └─ UserLoginEventHandler
    │                   └─ Database operations (scoped DbContext)
    │
    └─ (Other hosted services)
```

## Message Partitioning by UserId

```
user-login-events Topic
├─ Partition 0:  UserId-AAA login events
│   [msg1] [msg3] [msg7] ...
│
├─ Partition 1:  UserId-BBB login events
│   [msg2] [msg5] [msg8] ...
│
├─ Partition 2:  UserId-CCC login events
│   [msg4] [msg6] [msg9] ...

Same user's events always go to same partition
→ Preserves ordering for single user
→ Enables parallel processing across users
```

## DLQ (Dead Letter Queue) Routing

```
Successfully processed:
  message → user-login-events → [COMMIT]

Deserialization error:
  malformed JSON → user-login-events.dlq → [COMMIT]

Handler exception (max retries):
  UserNotFoundException → [Retry 1,2,3] → user-login-events.dlq → [COMMIT]
```

## Logging Output Example

```
[INF] Starting consumer for topic user-login-events (handler: UserLoginEvent)
[INF] [Kafka] Discovered 1 message handler(s): UserLoginEventHandler

(User logs in via API)

[INF] Published to user-login-events [partition:0, offset:42]
[INF] Processing login event for user 550e8400-e29b-41d4-a716-446655440000 (john@example.com) with role User at 2026-04-13 10:30:45
[INF] Successfully handled login event for user 550e8400-e29b-41d4-a716-446655440000
[DBG] Handled message from user-login-events [offset:42]
```

## Key Components

```
┌─────────────────────────────────────────────────────────────┐
│ Component              │ Responsibility                      │
├─────────────────────────────────────────────────────────────┤
│ IKafkaPublisher       │ Publish messages to Kafka           │
│ KafkaPublisher        │ Impl: JSON serialization, error hnd │
│ IMessageHandler<T>    │ Define topic + handle logic         │
│ UserLoginEventHandler │ Impl: Process UserLoginEvent        │
│ KafkaConsumerService  │ Background thread consumer loop    │
│ KafkaClient           │ Manage producer/consumer clients   │
│ KafkaDlqHandler       │ Route failures to DLQ topic        │
│ KafkaServiceRegistry  │ Auto-discover handlers + register  │
└─────────────────────────────────────────────────────────────┘
```
