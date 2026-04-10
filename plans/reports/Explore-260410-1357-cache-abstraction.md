# Cache Abstraction Scout Report

## 1. DI Patterns ✓
**Files:** `DependencyInjection.cs`, `KafkaServiceRegistration.cs`, `Application/DependencyInjection.cs`

- **Extension method pattern**: `AddInfrastructureServices()` / `AddKafkaServices()` on `IServiceCollection`
- **Settings binding**: `services.Configure<T>(config.GetSection("SectionName"))`
- **Lifetime patterns**: Singleton for stateless/thread-safe (JWT, Kafka), Scoped for DbContext/handlers
- **Auto-discovery**: Reflection-based handler registration in `KafkaServiceRegistration`

## 2. Settings POCO Pattern ✓
**Files:** `JwtSettings.cs`, `KafkaSettings.cs`

- Constant `SectionName` property (e.g., `"JwtSettings"`)
- Public properties with empty string/default values
- No validation logic (validation in extension methods or validators)
- Directly bindable via `Configuration.GetSection()`

## 3. Existing Caching ✓
**Status:** No cache implementation found. No references to:
- `IMemoryCache`, `IDistributedCache`
- `Microsoft.Extensions.Caching.*` packages
- Redis/StackExchange.Redis packages

## 4. Domain Interfaces Directory ✓
**Path:** `src/CleanArchitecture.Domain/Interfaces/`

```
├── IRepository.cs (generic)
├── IUserRepository.cs
├── IPermissionRepository.cs
├── IUnitOfWork.cs
└── Messaging/
    ├── IKafkaPublisher.cs
    ├── IMessageHandler.cs
    ├── IRequestReplyClient.cs
    └── KafkaMessage.cs
```
**Pattern:** Domain-level contracts only; no I/O infrastructure.

## 5. appsettings.json Sections ✓
**File:** `src/CleanArchitecture.Api/appsettings.json`

Existing sections: `ConnectionStrings`, `JwtSettings`, `Serilog`, `KafkaSettings`

→ **Recommendation:** Add `CacheSettings` section here (follows pattern)

## 6. Infrastructure NuGet Packages ✓
**File:** `CleanArchitecture.Infrastructure.csproj`

Current: BCrypt, Confluent.Kafka, JWT, EF Core 8.0, Npgsql

→ **Missing:** `Microsoft.Extensions.Caching.StackExchangeRedis` (for distributed)

## 7. Cacheable Services ✓
**Candidates identified:**

| Service | Method | Benefit |
|---------|--------|---------|
| `PermissionService` | `HasPermissionAsync()`, `GetAllEffectiveAsync()` | Lookup-heavy, stable data |
| `UserService` | `GetByIdAsync()`, `GetAllAsync()` | Frequently queried reads |

**Note:** No `IAuthService` implementation found; check `AuthService.cs` for token validation caching.

---

## Unresolved Qs
- Does `AuthService` cache token validation results?
- Cache invalidation strategy for user/permission changes?
- Memory vs. Redis trade-off for your workload?
