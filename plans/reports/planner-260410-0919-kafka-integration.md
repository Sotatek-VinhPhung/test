# Planner Report — Kafka Messaging Integration

**Date:** 2026-04-10
**Plan:** `plans/260410-0845-kafka-integration/`

## What Was Done

Analyzed the existing .NET 8 Clean Architecture codebase and created a 4-phase implementation plan for Kafka messaging integration using Confluent.Kafka.

### Codebase Analysis

- Read 12 source files to understand DI patterns, project references, config layout, naming conventions
- Confirmed: extension-method DI (`AddInfrastructureServices`), `IOptions<T>` config binding (`JwtSettings`), file-scoped namespaces, XML doc comments, CancellationToken defaults
- Domain has zero NuGet deps — plan preserves this
- Infrastructure refs Application; Api refs both Infrastructure + Application

### Plan Structure (5 files)

| File | Content |
|------|---------|
| `plan.md` | Overview, phase table, file inventory (14 new/modified files) |
| `phase-01-nuget-and-config-setup.md` | Confluent.Kafka package + KafkaSettings POCO + appsettings |
| `phase-02-domain-interfaces.md` | 4 files: KafkaMessage<T>, IKafkaPublisher, IMessageHandler<T>, IRequestReplyClient |
| `phase-03-infrastructure-kafka-implementations.md` | 5 files: KafkaClient, Publisher, ConsumerService, DlqHandler, RequestReplyClient |
| `phase-04-di-registration-and-api-wiring.md` | AddKafkaServices() extension + Program.cs one-liner |

### Key Design Decisions

1. **Separate `AddKafkaServices()` vs extending `AddInfrastructureServices()`** — chose separate to keep Kafka opt-in and self-contained
2. **Handler auto-discovery via assembly scanning** — no manual registration per handler
3. **`KafkaConsumerService<T>` as BackgroundService** — one hosted service per IMessageHandler<T>
4. **Request/Reply via ConcurrentDictionary<correlationId, TaskCompletionSource>** — standard pattern
5. **Manual offset commit** — no auto-commit, commit after successful handling
6. **DLQ routing** — after MaxRetryAttempts, publish to `{topic}.dlq` with error headers

### File Naming

All `.cs` files use **PascalCase** per C# ecosystem convention, matching existing codebase (`JwtSettings.cs`, `UserRepository.cs`, etc.).

## Unresolved Questions

1. **Confluent.Kafka exact version** — plan uses `2.6.1`; implementer should verify latest stable compatible with .NET 8
2. **SASL/SSL auth** — not in scope but KafkaSettings POCO should be designed for easy extension
3. **Scoped dependencies in handlers** — handlers are registered as Singleton; if a handler needs scoped services (e.g., DbContext), it must inject `IServiceScopeFactory`. Plan notes this but doesn't enforce it
4. **Topic auto-creation** — plan assumes topics exist; production may need admin client or documentation
