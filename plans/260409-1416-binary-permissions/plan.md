# Binary/Bitwise Permission System — Implementation Plan

> **Project**: CleanArchitecture (.NET 8)
> **Created**: 2026-04-09
> **Status**: Draft
> **Phases**: 5

---

## Architecture Overview

```
Effective Permission = RolePermission[module] | UserPermissionOverride[module]
```

**Storage model**: Two tables (`RolePermission`, `UserPermissionOverride`) each store a `string` module name + `long` flags value. No schema changes needed when adding new modules — just define a new `[Flags]` enum in Domain.

**Enforcement**: Dual-layer — `[RequirePermission]` attribute on API endpoints + `IPermissionService` for service-level checks. Both resolve effective permissions via the same logic.

**JWT strategy**: Embed permission claims as `perm:{Module}={flags}` in the access token. This avoids a DB call on every request for attribute-based checks. Service-level checks can optionally re-query DB for real-time accuracy.

---

## Current State Summary

| Layer          | Key Files                              | Relevant State                          |
|----------------|----------------------------------------|-----------------------------------------|
| Domain         | `User.cs`, `Role.cs`                   | User has `Role` (User=0, Admin=1)       |
| Application    | `AuthService.cs`, `IJwtTokenGenerator` | JWT: NameIdentifier, Email, Role claims |
| Infrastructure | `JwtTokenGenerator.cs`, `AppDbContext` | HMAC-SHA256 JWT, EF Core + PostgreSQL   |
| API            | `UsersController.cs`, `Program.cs`     | `[Authorize]` on UsersController        |

---

## Phase Overview

| Phase | Name                        | Files Created | Files Modified | Depends On |
|-------|-----------------------------|---------------|----------------|------------|
| 1     | Domain: Permission Model    | 7             | 0              | —          |
| 2     | Infrastructure: Persistence | 5             | 3              | Phase 1    |
| 3     | Application: Permission Service | 3         | 3              | Phase 2    |
| 4     | API: Authorization Pipeline | 4             | 2              | Phase 3    |
| 5     | Seed Data & Integration     | 2             | 1              | Phase 4    |

---

## Phase 1: Domain — Permission Model
**Goal**: Define the permission primitives. No dependencies on other layers.

See: [phase-1-domain.md](./phase-1-domain.md)

**Files to CREATE:**
1. `src/CleanArchitecture.Domain/Enums/PermissionModule.cs` — enum of module names
2. `src/CleanArchitecture.Domain/Enums/UserPermissions.cs` — `[Flags]` enum for Users module
3. `src/CleanArchitecture.Domain/Enums/OrderPermissions.cs` — `[Flags]` enum for Orders module
4. `src/CleanArchitecture.Domain/Entities/RolePermission.cs` — Role → module → flags
5. `src/CleanArchitecture.Domain/Entities/UserPermissionOverride.cs` — User → module → flags
6. `src/CleanArchitecture.Domain/Interfaces/IPermissionRepository.cs` — repository contract
7. `src/CleanArchitecture.Domain/Exceptions/ForbiddenException.cs` — 403 exception

**Files to MODIFY:** None

---

## Phase 2: Infrastructure — Persistence
**Goal**: EF Core mappings, repository, DB migration, JWT enhancement.

See: [phase-2-infrastructure.md](./phase-2-infrastructure.md)

**Files to CREATE:**
1. `src/CleanArchitecture.Infrastructure/Persistence/Configurations/RolePermissionConfiguration.cs`
2. `src/CleanArchitecture.Infrastructure/Persistence/Configurations/UserPermissionOverrideConfiguration.cs`
3. `src/CleanArchitecture.Infrastructure/Persistence/Repositories/PermissionRepository.cs`
4. `src/CleanArchitecture.Infrastructure/Persistence/Migrations/` — EF migration (auto-generated)
5. `src/CleanArchitecture.Infrastructure/Auth/PermissionClaimNames.cs` — claim key constants

**Files to MODIFY:**
1. `src/CleanArchitecture.Infrastructure/Persistence/AppDbContext.cs` — add DbSets
2. `src/CleanArchitecture.Infrastructure/Persistence/UnitOfWork.cs` — add IPermissionRepository
3. `src/CleanArchitecture.Infrastructure/DependencyInjection.cs` — register PermissionRepository

---

## Phase 3: Application — Permission Service
**Goal**: Business logic for permission resolution + JWT embedding.

See: [phase-3-application.md](./phase-3-application.md)

**Files to CREATE:**
1. `src/CleanArchitecture.Application/Permissions/IPermissionService.cs`
2. `src/CleanArchitecture.Application/Permissions/PermissionService.cs`
3. `src/CleanArchitecture.Application/Permissions/DTOs/PermissionDto.cs`

**Files to MODIFY:**
1. `src/CleanArchitecture.Application/Common/Interfaces/IJwtTokenGenerator.cs` — add permissions param
2. `src/CleanArchitecture.Application/Auth/Services/AuthService.cs` — load & pass permissions at login/register/refresh
3. `src/CleanArchitecture.Application/DependencyInjection.cs` — register PermissionService

---

## Phase 4: API — Authorization Pipeline
**Goal**: Attribute-based enforcement + exception handling.

See: [phase-4-api.md](./phase-4-api.md)

**Files to CREATE:**
1. `src/CleanArchitecture.Api/Authorization/RequirePermissionAttribute.cs`
2. `src/CleanArchitecture.Api/Authorization/PermissionAuthorizationHandler.cs`
3. `src/CleanArchitecture.Api/Authorization/PermissionRequirement.cs`
4. `src/CleanArchitecture.Api/Authorization/PermissionPolicyProvider.cs`

**Files to MODIFY:**
1. `src/CleanArchitecture.Api/Middleware/ExceptionHandlingMiddleware.cs` — handle ForbiddenException
2. `src/CleanArchitecture.Api/Controllers/UsersController.cs` — add `[RequirePermission]` examples

---

## Phase 5: Seed Data & Integration
**Goal**: Default role permissions + admin override example.

See: [phase-5-seed.md](./phase-5-seed.md)

**Files to CREATE:**
1. `src/CleanArchitecture.Infrastructure/Persistence/Seed/PermissionSeeder.cs`
2. `src/CleanArchitecture.Infrastructure/Persistence/Seed/SeedExtensions.cs`

**Files to MODIFY:**
1. `src/CleanArchitecture.Api/Program.cs` — call seeder on startup

---

## Key Design Decisions

### 1. String-based module names (not int enum in DB)
The `Module` column stores the enum `.ToString()` value (e.g., `"Users"`, `"Orders"`). Adding a new module = add a new `[Flags]` enum + a `PermissionModule` enum value. No migration needed.

### 2. `long` for flag storage (not `int`)
64 bits = 64 distinct permissions per module. Plenty of headroom. EF maps `long` → `bigint` in PostgreSQL.

### 3. Permission claims in JWT
Format: `perm:Users=15`, `perm:Orders=3`. Parsed by the `PermissionAuthorizationHandler` at the API layer — zero DB calls for attribute-based checks.

Trade-off: Permissions are stale until token refresh. Acceptable for most apps. For real-time checks, inject `IPermissionService` directly (hits DB).

### 4. Role enum kept as-is
`Role.User` and `Role.Admin` remain. `RolePermission` table maps these to their default flag sets. The `Role` claim still exists in JWT for backward compatibility.

### 5. Composite key on permission tables
- `RolePermission`: PK = `(Role, Module)` — one row per role per module
- `UserPermissionOverride`: PK = `(UserId, Module)` — one row per user per module

No surrogate `Id` needed. These are lookup tables, not domain entities with lifecycle.

---

## File Count Summary

| Action   | Count |
|----------|-------|
| Create   | 21    |
| Modify   | 9     |
| **Total**| **30**|

---

## Unresolved Questions

1. **Cache invalidation**: When admin changes a user's permissions, their JWT is stale. Options: (a) short token TTL (5-15 min), (b) permission version stamp checked on sensitive endpoints, (c) accept staleness. Recommend (a) for v1.
2. **Deny permissions**: Current design is additive only (Role OR UserOverride). Should we support explicit deny flags (a separate `DeniedFlags` column)? Recommend: not for v1 — YAGNI.
3. **Permission management API**: Do we need CRUD endpoints for assigning permissions to roles/users? Not in this plan scope — can be a follow-up.
