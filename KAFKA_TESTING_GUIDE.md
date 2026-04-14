# Testing Kafka Producer-Consumer Implementation

## Prerequisites

1. **Docker Running**
   ```powershell
   docker-compose up -d kafka zookeeper
   ```

2. **Application Built**
   ```powershell
   dotnet build
   ```

3. **Database Initialized**
   - SQLite database (`cleanarchitecture.db`) auto-created
   - Migrations auto-applied on first run

## Test Scenario 1: Full E2E Login Flow

### Step 1: Start Infrastructure
```powershell
# Start Kafka and Zookeeper
docker-compose up -d kafka zookeeper

# Verify services are healthy
docker-compose ps
```

Expected output:
```
NAME       STATUS              PORTS
zookeeper  Up (healthy)        2181
kafka      Up (healthy)        9092
```

### Step 2: Start Application
```powershell
dotnet run --project src/CleanArchitecture.Api
```

Expected output:
```
[INF] [Kafka] Discovered 1 message handler(s): UserLoginEventHandler
[INF] Starting consumer for topic user-login-events (handler: UserLoginEvent)
[INF] Application started.
```

### Step 3: Monitor Messages
Open a new terminal and create consumer:
```bash
docker exec -it kafka kafka-console-consumer \
  --bootstrap-server localhost:9092 \
  --topic user-login-events \
  --from-beginning
```

### Step 4: Register a New User
Using Postman or curl:
```bash
curl -X POST http://localhost:5000/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{
    "firstName": "John",
    "lastName": "Doe",
    "email": "john@example.com",
    "password": "Password@123"
  }'
```

Response:
```json
{
  "accessToken": "eyJhbGci...",
  "refreshToken": "eyJhbGci...",
  "expiresAt": "2026-04-13T11:15:00Z"
}
```

### Step 5: Login User
```bash
curl -X POST http://localhost:5000/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "email": "john@example.com",
    "password": "Password@123"
  }'
```

### Step 6: Observe Kafka Message
In the consumer terminal, you should see:
```json
{
  "userId": "550e8400-e29b-41d4-a716-446655440000",
  "email": "john@example.com",
  "role": "User",
  "loginAt": "2026-04-13T11:14:00Z",
  "sourceIp": null
}
```

### Step 7: Check Application Logs
In the application terminal:
```
[INF] Published to user-login-events [partition:0, offset:0]
[INF] Processing login event for user 550e8400-e29b-41d4-a716-446655440000 (john@example.com) with role User at 2026-04-13T11:14:00Z
[INF] Successfully handled login event for user 550e8400-e29b-41d4-a716-446655440000
[DBG] Handled message from user-login-events [offset:0]
```

## Test Scenario 2: Multiple Sequential Logins

### Objective: Verify message ordering by userId

```powershell
# Login same user 3 times
for ($i = 0; $i -lt 3; $i++) {
    curl -X POST http://localhost:5000/api/auth/login `
      -H "Content-Type: application/json" `
      -d '{"email": "john@example.com", "password": "Password@123"}'
    Start-Sleep -Seconds 1
}
```

### Verify in Kafka
```bash
docker exec -it kafka kafka-console-consumer \
  --bootstrap-server localhost:9092 \
  --topic user-login-events \
  --property print.key=true \
  --property key.separator=" = "
```

Expected output:
```
550e8400-e29b-41d4-a716-446655440000 = {...login event 1...}
550e8400-e29b-41d4-a716-446655440000 = {...login event 2...}
550e8400-e29b-41d4-a716-446655440000 = {...login event 3...}
```

**Note:** All three messages have the same key (userId), ensuring they go to the same partition in order.

## Test Scenario 3: Error Handling - User Not Found

### Simulate Handler Error
Temporarily modify `UserLoginEventHandler.cs`:

```csharp
public async Task HandleAsync(UserLoginEvent message, CancellationToken cancellationToken)
{
    throw new Exception("Simulated error"); // ← Add this
}
```

### Run Test
1. Rebuild: `dotnet build`
2. Restart app: `dotnet run`
3. Login: `curl POST .../auth/login`

### Observe Behavior
Application logs:
```
[WRN] Handler failed for user-login-events (attempt 1/3): Simulated error
[WRN] Handler failed for user-login-events (attempt 2/3): Simulated error
[WRN] Handler failed for user-login-events (attempt 3/3): Simulated error
[ERR] Non-transient deserialization error on user-login-events, sending to DLQ
```

### Check DLQ Topic
```bash
docker exec -it kafka kafka-console-consumer \
  --bootstrap-server localhost:9092 \
  --topic user-login-events.dlq \
  --from-beginning
```

You should see the failed message in DLQ.

### Fix and Recover
1. Remove the `throw` statement
2. Rebuild: `dotnet build`
3. Restart app
4. Consumer automatically continues from where it left off (offset tracking)

**Note:** DLQ messages won't be reprocessed automatically. You'd need a separate DLQ handler to process them.

## Test Scenario 4: Message Partitioning

### Create Multiple Users and Login
```powershell
# Create and login 5 different users
$emails = @("alice@example.com", "bob@example.com", "charlie@example.com", "diana@example.com", "eve@example.com")

foreach ($email in $emails) {
    # Register
    curl -X POST http://localhost:5000/api/auth/register `
      -H "Content-Type: application/json" `
      -d @"
    {
      "firstName": "$($email.Split('@')[0])",
      "lastName": "Test",
      "email": "$email",
      "password": "Password@123"
    }
"@
    
    # Login
    curl -X POST http://localhost:5000/api/auth/login `
      -H "Content-Type: application/json" `
      -d "{`"email`": `"$email`", `"password`": `"Password@123`"}"
    
    Start-Sleep -Seconds 1
}
```

### Analyze Partitions
```bash
docker exec -it kafka kafka-topics \
  --bootstrap-server localhost:9092 \
  --topic user-login-events \
  --describe
```

Output:
```
Topic: user-login-events
Topic: user-login-events        Partition: 0    Leader: 1       Replicas: [1]   Isr: [1]
Topic: user-login-events        Partition: 1    Leader: 1       Replicas: [1]   Isr: [1]
```

### View with Partition Info
```bash
docker exec -it kafka kafka-console-consumer \
  --bootstrap-server localhost:9092 \
  --topic user-login-events \
  --property print.partition=true \
  --from-beginning
```

## Test Scenario 5: Consumer Group Offset Management

### Create New Consumer Group
```bash
docker exec -it kafka kafka-console-consumer \
  --bootstrap-server localhost:9092 \
  --topic user-login-events \
  --group test-group-1 \
  --from-beginning
```

### List All Offsets
```bash
docker exec -it kafka kafka-consumer-groups \
  --bootstrap-server localhost:9092 \
  --group clean-architecture-group \
  --describe
```

Output:
```
GROUP                       TOPIC               PARTITION  CURRENT-OFFSET  LOG-END-OFFSET  LAG
clean-architecture-group    user-login-events   0          5               5               0
```

**LAG = 0:** Consumer is caught up (no messages pending)
**LAG > 0:** Consumer is behind (unprocessed messages)

### Reset Offset
```bash
# Reset to beginning
docker exec -it kafka kafka-consumer-groups \
  --bootstrap-server localhost:9092 \
  --group clean-architecture-group \
  --reset-offsets --to-earliest --all-topics --execute

# Reset to end
docker exec -it kafka kafka-consumer-groups \
  --bootstrap-server localhost:9092 \
  --group clean-architecture-group \
  --reset-offsets --to-latest --all-topics --execute
```

## Test Scenario 6: Performance - Batch Operations

### Send 100 Logins
```powershell
$stopwatch = [System.Diagnostics.Stopwatch]::StartNew()

for ($i = 0; $i -lt 100; $i++) {
    curl -X POST http://localhost:5000/api/auth/login `
      -H "Content-Type: application/json" `
      -d '{"email": "john@example.com", "password": "Password@123"}' | Out-Null
}

$stopwatch.Stop()
Write-Host "Time taken: $($stopwatch.ElapsedMilliseconds)ms"
```

### Monitor Consumer Performance
```bash
# Watch offset progression
watch -n 1 'docker exec -i kafka kafka-consumer-groups \
  --bootstrap-server localhost:9092 \
  --group clean-architecture-group \
  --describe'
```

Expected: Consumer should process ~10-20 messages/second depending on handler complexity.

## Test Scenario 7: Handler Timeout

### Simulate Slow Handler
Modify `UserLoginEventHandler.cs`:

```csharp
public async Task HandleAsync(UserLoginEvent message, CancellationToken cancellationToken)
{
    _logger.LogInformation("Processing event, waiting 10 seconds...");
    await Task.Delay(10000, cancellationToken); // ← Simulate slow operation
    _logger.LogInformation("Processing complete");
}
```

### Test Behavior
1. Restart app
2. Send login request
3. Observer: Application logs should show 10-second delay
4. Message still processes successfully (no timeout configured)

## Cleanup

### Stop Infrastructure
```powershell
docker-compose down
```

### Delete Messages and Topics (Careful!)
```bash
# Delete topic (stops all consumers)
docker exec -it kafka kafka-topics \
  --bootstrap-server localhost:9092 \
  --topic user-login-events \
  --delete
```

### Clean Database
```powershell
# Remove SQLite file
Remove-Item cleanarchitecture.db -Force
```

## Troubleshooting

### No Messages in Consumer
1. Check Kafka is running: `docker-compose ps`
2. Check topic exists: `docker exec kafka kafka-topics --list`
3. Check application logs for publish errors
4. Verify handler is registered (look for "Discovered" log message)

### Consumer Lagging
1. Check handler logs for errors
2. Monitor CPU/memory usage
3. Check for slow database queries
4. View DLQ for failed messages

### Messages in DLQ
1. Check application logs for error messages
2. Fix the underlying issue
3. Manually republish messages or use DLQ handler
4. Monitor for new failures

### Consumer Group Not Found
```bash
# Create new consumer group (automatic)
docker exec kafka kafka-console-consumer \
  --bootstrap-server localhost:9092 \
  --topic user-login-events \
  --group my-group
```

## Performance Expectations

| Metric | Value |
|--------|-------|
| Producer throughput | 100+ messages/sec |
| Consumer throughput | 10-20 messages/sec (with DB operations) |
| Partition rebalancing | < 5 seconds |
| Message serialization | < 1ms |
| Average handler time | 10-50ms (database dependent) |

## Next Steps

1. **Create more event types** (UserRegistered, PasswordReset, etc.)
2. **Add consumer groups** for different processing workflows
3. **Implement DLQ handler** for automatic dead-letter processing
4. **Add metrics** (Prometheus) for monitoring
5. **Set up alerting** for lagging consumers
6. **Test failure scenarios** (Kafka broker down, handler exceptions, etc.)
