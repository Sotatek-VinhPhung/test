# Binary/Bitwise Permission System — Implementation Plan

## Summary
Add module-based binary permission system with role defaults + user overrides. Effective permission = `RolePermission[module] | UserOverride[module]`. Enforced via JWT-based `[RequirePermission]` attribute AND `IPermissionService` for service-level checks.

---

## Phase 1: Domain Layer — Permission Primitives
**7 new files, 0 modifications**

Create:
1. `src/CleanArchitecture.Domain/Enums/PermissionModule.cs` — Module registry enum (Users, Orders)
2. `src/CleanArchitecture.Domain/Enums/UserPermissions.cs` — `[Flags] : long` (None=0, Create=1, Read=2, Update=4, Delete=8, Export=16, All=31)
3. `src/CleanArchitecture.Domain/Enums/OrderPermissions.cs` — `[Flags] : long` (None=0, Create=1, Read=2, Update=4, Delete=8, Approve=16, Cancel=32, All=63)
4. `src/CleanArchitecture.Domain/Entities/RolePermission.cs` — Composite PK (Role, Module), `long Flags`
5. `src/CleanArchitecture.Domain/Entities/UserPermissionOverride.cs` — Composite PK (UserId, Module), `long Flags`, FK to User
6. `src/CleanArchitecture.Domain/Interfaces/IPermissionRepository.cs` — GetByRole, GetByUserId, GetEffectiveFlags, Upsert methods
7. `src/CleanArchitecture.Domain/Exceptions/ForbiddenException.cs` — Maps to HTTP 403

---

## Phase 2: Infrastructure — Persistence & JWT Claims
**3 new files, 3 modifications**

Create:
1. `src/CleanArchitecture.Infrastructure/Persistence/Configurations/RolePermissionConfiguration.cs` — EF composite key, Role as string
2. `src/CleanArchitecture.Infrastructure/Persistence/Configurations/UserPermissionOverrideConfiguration.cs` — EF composite key, FK cascade
3. `src/CleanArchitecture.Infrastructure/Persistence/Repositories/PermissionRepository.cs` — Implements IPermissionRepository with bitwise OR logic
4. `src/CleanArchitecture.Infrastructure/Auth/PermissionClaimNames.cs` — Claim name constants (`perm:Module`)

Modify:
1. `src/CleanArchitecture.Infrastructure/Persistence/AppDbContext.cs` — Add RolePermissions + UserPermissionOverrides DbSets
2. `src/CleanArchitecture.Domain/Interfaces/IUnitOfWork.cs` — Add `IPermissionRepository Permissions`
3. `src/CleanArchitecture.Infrastructure/Persistence/UnitOfWork.cs` — Implement Permissions property
4. `src/CleanArchitecture.Infrastructure/DependencyInjection.cs` — Register PermissionRepository

---

## Phase 3: Application — Permission Service
**3 new files, 3 modifications**

Create:
1. `src/CleanArchitecture.Application/Permissions/IPermissionService.cs` — HasPermission, GetAllEffective, RequirePermission
2. `src/CleanArchitecture.Application/Permissions/PermissionService.cs` — Bitwise OR/AND logic via IUnitOfWork
3. `src/CleanArchitecture.Application/Permissions/DTOs/PermissionDto.cs` — Record(Module, Flags)

Modify:
1. `src/CleanArchitecture.Application/Common/Interfaces/IJwtTokenGenerator.cs` — Add `Dictionary<string, long>? permissions` param
2. `src/CleanArchitecture.Infrastructure/Auth/JwtTokenGenerator.cs` — Add `perm:Module=flags` claims
3. `src/CleanArchitecture.Application/Auth/Services/AuthService.cs` — Inject IPermissionService, load perms at login/register/refresh
4. `src/CleanArchitecture.Application/DependencyInjection.cs` — Register PermissionService

---

## Phase 4: API — Authorization Pipeline
**4 new files, 3 modifications**

Create:
1. `src/CleanArchitecture.Api/Authorization/RequirePermissionAttribute.cs` — Policy = "Permission:{module}:{flags}"
2. `src/CleanArchitecture.Api/Authorization/PermissionRequirement.cs` — IAuthorizationRequirement
3. `src/CleanArchitecture.Api/Authorization/PermissionPolicyProvider.cs` — Dynamic policy creation
4. `src/CleanArchitecture.Api/Authorization/PermissionAuthorizationHandler.cs` — JWT claim bitwise check

Modify:
1. `src/CleanArchitecture.Api/Middleware/ExceptionHandlingMiddleware.cs` — Add ForbiddenException → 403
2. `src/CleanArchitecture.Api/Program.cs` — Register policy provider + handler
3. `src/CleanArchitecture.Api/Controllers/UsersController.cs` — Add [RequirePermission] attributes

---

## Phase 5: Seed Data
**2 new files, 1 modification**

Create:
1. `src/CleanArchitecture.Infrastructure/Persistence/Seed/PermissionSeeder.cs` — Default role→module→flags
2. `src/CleanArchitecture.Infrastructure/Persistence/Seed/SeedExtensions.cs` — WebApplication extension

Modify:
1. `src/CleanArchitecture.Api/Program.cs` — Call `await app.SeedPermissionsAsync()`

---

## Default Permission Matrix

| Role  | Users Module | Orders Module |
|-------|-------------|---------------|
| Admin | All (31) | All (63) |
| User  | Read (2) | Create+Read (3) |

## Build & Test Verification
- `dotnet build` after each phase
- `dotnet test` after Phase 5 (update existing tests for new AuthService constructor)
