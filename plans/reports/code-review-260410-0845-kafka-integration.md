# Code Review: Kafka Messaging Integration

**Date:** 2026-04-10  
**Reviewer:** code-reviewer  
**Scope:** 11 new files, 4 modified files  
**Verdict:** PASS with issues (2 critical, 3 moderate, 3 minor)

---

## Summary

Solid Kafka integration following Clean Architecture principles. Domain layer is properly isolated. Producer singleton pattern, manual offset commits, DLQ routing, and request/reply correlation are all structurally correct. A few thread-safety and resilience issues need attention before production use.

---

## Critical Issues

### C1. Reply listener silently dies on unhandled exception
**File:** `KafkaRequestReplyClient.cs:137-139`  
The `ReplyListenerLoop` catches generic `Exception`, logs it, and **exits permanently**. All subsequent `SendAsync` calls will hang until timeout because `_replyListenerTask` is still non-null (so `EnsureReplyListenerStarted` won't restart it). This is a silent failure mode.

**Fix:** Either restart the listener loop (wrap the while-loop body in try/catch instead of wrapping the whole loop), or set `_replyListenerTask = null` on exit so the next `SendAsync` call restarts it.

### C2. `KafkaClient.Dispose()` is not thread-safe with `GetProducer()`
**File:** `KafkaClient.cs:81-95`  
`Dispose()` reads and nullifies `_producer` without acquiring `_lock`. A concurrent `GetProducer()` call could return a disposed producer or race during flush. Since `KafkaClient` is singleton, this mainly affects shutdown ordering, but it can cause `ObjectDisposedException` during graceful shutdown if consumers are still publishing to DLQ.

**Fix:** Acquire `_lock` in `Dispose()`, or use `Interlocked.Exchange` to atomically swap `_producer` to null.

---

## Moderate Issues

### M1. `PublishBatchAsync` swallows delivery errors
**File:** `KafkaPublisher.cs:54-61`  
The `Produce` callback logs errors but doesn't propagate them. After `Flush`, the caller has no way to know which (or how many) messages failed. For fire-and-forget this is acceptable, but the method signature (`Task`) implies reliability.

**Fix:** Collect failures in a `ConcurrentBag<DeliveryReport>` from the callback, then throw an `AggregateException` after flush if any failed.

### M2. Handler registered as Singleton may break scoped dependencies
**File:** `KafkaServiceRegistration.cs:66`  
`services.AddSingleton(handlerInterface, handlerType)` forces all `IMessageHandler<T>` implementations to be singletons. If any handler injects scoped services (e.g., `DbContext`, `IUnitOfWork`), it will cause a captive dependency / `InvalidOperationException` at runtime.

**Fix:** Register handlers as scoped or transient and resolve them via `IServiceScopeFactory` inside `KafkaConsumerService`. The consumer service itself stays singleton (hosted service), but creates a scope per message.

### M3. Retry deserializes on every attempt
**File:** `KafkaConsumerService.cs:94-95`  
`JsonSerializer.Deserialize<T>` runs inside the retry loop. If the payload is malformed JSON, all retry attempts will fail identically, wasting time (`MaxRetryAttempts * RetryDelayMs`). Deserialization errors are non-transient.

**Fix:** Move deserialization before the retry loop, or catch `JsonException` separately and send to DLQ immediately without retrying.

---

## Minor Issues

### m1. `KafkaMessage<T>` in Domain uses "Kafka" in naming
**File:** `KafkaMessage.cs:6`  
Domain types should be transport-agnostic. Naming it `KafkaMessage` leaks infrastructure vocabulary into the Domain layer. Functionally harmless but violates Clean Architecture naming conventions.

**Suggestion:** Rename to `Message<T>` or `MessageEnvelope<T>`.

### m2. `Console.WriteLine` for handler discovery logging
**File:** `KafkaServiceRegistration.cs:77`  
Uses `Console.WriteLine` instead of `ILogger`. This bypasses Serilog entirely and won't appear in structured log output.

**Suggestion:** Accept an optional `ILogger` parameter, or defer logging until the host is built.

### m3. `KafkaRequestReplyClient` uses shared reply topic for all instances
**File:** `KafkaRequestReplyClient.cs:98`  
The reply consumer group `{GroupId}-reply` means that in a multi-instance deployment, only one instance receives each reply. If instance A sends a request and instance B's consumer picks up the reply, the correlation ID won't match any pending TCS and the request times out.

**Suggestion:** Use a unique reply topic or group ID per instance (e.g., append machine name or process ID).

---

## Checklist

| Criteria | Status | Notes |
|---|---|---|
| Clean Architecture compliance | PASS | No Confluent.Kafka refs in Domain |
| Thread safety | FAIL | C2: Dispose race; C1: listener death |
| Error handling & graceful shutdown | WARN | C1, M1 |
| DLQ retry logic | WARN | M3: non-transient errors retried |
| Request/reply correlation + timeout | PASS | Correct pattern; see m3 for scale |
| DI registration correctness | WARN | M2: singleton handlers risk captive deps |
| Files under 200 lines | PASS | Max 180 lines (KafkaRequestReplyClient) |
| Code quality, naming, XML docs | PASS | Good docs; m1 naming nit |

---

## Unresolved Questions

1. Are any `IMessageHandler<T>` implementations expected to use scoped services (DbContext)? If yes, M2 is a blocker.
2. Will this run as multiple replicas? If yes, m3 is a blocker (reply routing).
3. Is there a preference for exponential backoff vs fixed delay in retry logic?
