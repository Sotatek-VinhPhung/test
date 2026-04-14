# Kafka Message Consumer Implementation

## Overview
Added a Kafka message consumer in `UserService` layer to consume and process `UserLoginEvent` messages published by `AuthService`.

## Files Created/Modified

### 1. **UserLoginEventHandler.cs** (NEW)
**Path:** `src\CleanArchitecture.Application\Users\Services\UserLoginEventHandler.cs`

This is the message handler that processes login events from Kafka:

```csharp
public class UserLoginEventHandler : IMessageHandler<UserLoginEvent>
{
    public string Topic => "user-login-events";
    
    public async Task HandleAsync(UserLoginEvent message, CancellationToken cancellationToken)
    {
        // Logs user login events
        // Can be extended for analytics, notifications, etc.
    }
}
```

**Key Features:**
- Implements `IMessageHandler<UserLoginEvent>` interface
- Subscribes to `user-login-events` topic
- Logs login activity with user details
- Error handling with automatic retry/DLQ routing
- Can be extended for:
  - Updating last login timestamp
  - Analytics/statistics
  - Email notifications
  - Security monitoring

### 2. **Program.cs** (UPDATED)
**Path:** `src\CleanArchitecture.Api\Program.cs`

Updated Kafka service registration to scan Application assembly for handlers:

```csharp
// Before:
builder.Services.AddKafkaServices(builder.Configuration);

// After:
builder.Services.AddKafkaServices(
    builder.Configuration, 
    typeof(CleanArchitecture.Application.Auth.Services.AuthService).Assembly
);
```

This enables auto-discovery of the `UserLoginEventHandler` and registers it as a background service.

### 3. **CleanArchitecture.Application.csproj** (UPDATED)
**Path:** `src\CleanArchitecture.Application\CleanArchitecture.Application.csproj`

Added logging abstractions for handler implementation:

```xml
<PackageReference Include="Microsoft.Extensions.Logging.Abstractions" Version="10.0.5" />
```

## Architecture Flow

```
AuthService.LoginAsync()
    ↓
Publishes UserLoginEvent to Kafka topic "user-login-events"
    ↓
KafkaConsumerService<UserLoginEvent> (Background Service)
    ↓
UserLoginEventHandler.HandleAsync()
    ↓
Logs event + Optional business logic (analytics, notifications, etc.)
```

## How It Works

### 1. **Message Flow**
- User logs in via API
- `AuthService.LoginAsync()` publishes `UserLoginEvent` to Kafka
- `UserLoginEventHandler` receives and processes the event asynchronously
- Logs are written with user details and timestamp

### 2. **Automatic Discovery**
The `KafkaServiceRegistration.AddKafkaServices()` method:
- Scans the Application assembly for `IMessageHandler<T>` implementations
- Finds `UserLoginEventHandler`
- Registers it as a scoped service
- Creates a `KafkaConsumerService<UserLoginEvent>` hosted background service
- Logs all discovered handlers at startup

### 3. **Consumer Behavior**
- Runs as a background service on a dedicated thread
- Subscribes to the `user-login-events` topic
- Processes messages one at a time
- Retries failed messages up to `MaxRetryAttempts` (configured in appsettings.json)
- Sends non-recoverable failures to DLQ (Dead Letter Queue)
- Manually commits offsets after successful processing

### 4. **Error Handling**
- **Deserialization errors**: Immediately sent to DLQ
- **Handler exceptions**: Retried with exponential backoff
- **Max retries exceeded**: Moved to DLQ topic (`user-login-events.dlq`)
- **Cancellation**: Gracefully stops on application shutdown

## Configuration (appsettings.json)

```json
{
  "KafkaSettings": {
    "BootstrapServers": "localhost:9092",
    "GroupId": "clean-architecture-group",
    "MaxRetryAttempts": 3,
    "RetryDelayMs": 1000,
    "DlqTopicSuffix": ".dlq"
  }
}
```

## Testing the Implementation

### 1. **Start Docker Services**
```powershell
docker-compose up -d kafka zookeeper
```

### 2. **Run Application**
```powershell
dotnet run
```

### 3. **Call Login Endpoint**
```bash
POST /api/auth/login
{
  "email": "user@example.com",
  "password": "password123"
}
```

### 4. **Monitor Kafka Messages**
```bash
# View published messages
docker exec -it kafka kafka-console-consumer \
  --bootstrap-server localhost:9092 \
  --topic user-login-events \
  --from-beginning

# View DLQ messages (if any errors occur)
docker exec -it kafka kafka-console-consumer \
  --bootstrap-server localhost:9092 \
  --topic user-login-events.dlq \
  --from-beginning
```

### 5. **Check Logs**
Look for messages like:
```
[Information] Processing login event for user {UserId} ({Email}) with role {Role} at {LoginAt}
[Information] Successfully handled login event for user {UserId}
```

## Extending the Handler

Example extensions you can add to `UserLoginEventHandler`:

### 1. **Update Last Login Timestamp**
```csharp
if (user is not null)
{
    user.LastLoginAt = message.LoginAt;
    _unitOfWork.Users.Update(user);
    await _unitOfWork.SaveChangesAsync(cancellationToken);
}
```

### 2. **Send Email Notification**
```csharp
// Inject IEmailService via DI
var emailService = _scopeFactory.ServiceProvider.GetRequiredService<IEmailService>();
await emailService.SendLoginNotificationAsync(message.Email, message.LoginAt);
```

### 3. **Update Analytics**
```csharp
// Track user activity
var analyticsService = _scopeFactory.ServiceProvider.GetRequiredService<IAnalyticsService>();
await analyticsService.TrackUserLogin(message.UserId, message.LoginAt);
```

### 4. **Security Monitoring**
```csharp
// Check for suspicious activity
var securityService = _scopeFactory.ServiceProvider.GetRequiredService<ISecurityService>();
if (message.SourceIp != null)
{
    await securityService.CheckUnusualLoginAsync(message.UserId, message.SourceIp);
}
```

## Build Status
✅ **Build Successful** - All code compiles without errors

## Next Steps
1. Create additional message handlers for other events (Register, PasswordReset, etc.)
2. Add more business logic to the login handler (notifications, analytics)
3. Create consumer groups for distributed processing
4. Monitor DLQ for any failed messages
5. Add metrics/observability for message processing
