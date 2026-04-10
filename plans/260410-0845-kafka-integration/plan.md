# Kafka Messaging Integration — Implementation Plan

**Date:** 2026-04-10
**Project:** CleanArchitecture (.NET 8)
**Library:** Confluent.Kafka
**Status:** Planned

## Summary

Add Kafka messaging to the existing Clean Architecture project: publisher, consumer with handler-per-topic, request/reply, and dead letter queue (DLQ). All wired through existing DI extension-method pattern.

## Architecture Fit

```
Domain          → Interfaces (IKafkaPublisher, IMessageHandler<T>, IRequestReplyClient)
Application     → Unchanged (consumes Domain interfaces only)
Infrastructure  → Implementations (KafkaClient, KafkaPublisher, KafkaConsumerService, DLQ, RequestReply)
Api             → DI registration (AddKafkaServices), appsettings config
```

## Phases

| # | Phase | Files | Status |
|---|-------|-------|--------|
| 1 | [NuGet + Config Setup](phase-01-nuget-and-config-setup.md) | 3 | ✅ Complete |
| 2 | [Domain Interfaces](phase-02-domain-interfaces.md) | 4 | ✅ Complete |
| 3 | [Infrastructure Implementations](phase-03-infrastructure-kafka-implementations.md) | 7 | ✅ Complete |
| 4 | [DI Registration + API Wiring](phase-04-di-registration-and-api-wiring.md) | 3 | ✅ Complete |

## Key Dependencies

- Confluent.Kafka 2.x NuGet package
- Running Kafka broker (localhost:9092 for dev)
- Existing: Serilog, System.Text.Json, IHostedService

## File Naming Convention

All new `.cs` files use **PascalCase** per C# ecosystem convention (matching existing codebase: `JwtSettings.cs`, `UserRepository.cs`). Each file stays under 200 lines.

## Estimated New Files (14 total)

```
src/CleanArchitecture.Domain/Interfaces/Messaging/
├── IKafkaPublisher.cs
├── IMessageHandler.cs
├── IRequestReplyClient.cs
└── KafkaMessage.cs

src/CleanArchitecture.Infrastructure/Messaging/
├── KafkaSettings.cs
├── KafkaClient.cs
├── KafkaPublisher.cs
├── KafkaConsumerService.cs
├── KafkaDlqHandler.cs
├── KafkaRequestReplyClient.cs
└── KafkaServiceRegistration.cs

src/CleanArchitecture.Api/
├── appsettings.json              (modify — add KafkaSettings)
├── appsettings.Development.json  (modify — add KafkaSettings)
└── Program.cs                    (modify — add AddKafkaServices())
```

## Code Review Fixes Applied

All critical issues resolved:
- **C1, C2**: Critical design issues — addressed in infrastructure implementations
- **M1, M2, M3**: Major functional requirements — validated across all phases
- **m2**: Minor optimization — included in KafkaClient and request/reply patterns
