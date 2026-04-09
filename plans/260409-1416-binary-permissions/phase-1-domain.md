# Phase 1: Domain — Permission Model

> **Depends on**: Nothing
> **Blocks**: Phase 2

## Goal
Define permission primitives in the Domain layer. Pure C# — no dependencies on EF, ASP.NET, or other layers.

---

## Files to Create

### 1. `src/CleanArchitecture.Domain/Enums/PermissionModule.cs`

Registry of all modules that have permissions. Used as the key in permission tables.

```csharp
namespace CleanArchitecture.Domain.Enums;

/// <summary>
/// Identifies a permission module. Each module has its own [Flags] enum.
/// Stored as string in DB — adding new values requires no migration.
/// </summary>
public enum PermissionModule
{
    Users,
    Orders
}
```

**Why enum, not just strings?** Compile-time safety when referencing modules. The DB column stores `.ToString()` so it's still migration-free when adding values.

---

### 2. `src/CleanArchitecture.Domain/Enums/UserPermissions.cs`

```csharp
namespace CleanArchitecture.Domain.Enums;

/// <summary>
/// Bitwise permission flags for the Users module.
/// Each value must be a power of 2.
/// </summary>
[Flags]
public enum UserPermissions : long
{
    None    = 0,
    Create  = 1 << 0,   // 1
    Read    = 1 << 1,   // 2
    Update  = 1 << 2,   // 4
    Delete  = 1 << 3,   // 8
    Export  = 1 << 4,   // 16
    All     = Create | Read | Update | Delete | Export
}
```

**Pattern**: Always use `1 << N` for clarity. `: long` base type matches the DB column.

---

### 3. `src/CleanArchitecture.Domain/Enums/OrderPermissions.cs`

```csharp
namespace CleanArchitecture.Domain.Enums;

[Flags]
public enum OrderPermissions : long
{
    None    = 0,
    Create  = 1 << 0,
    Read    = 1 << 1,
    Update  = 1 << 2,
    Delete  = 1 << 3,
    Approve = 1 << 4,
    Cancel  = 1 << 5,
    All     = Create | Read | Update | Delete | Approve | Cancel
}
```

---

### 4. `src/CleanArchitecture.Domain/Entities/RolePermission.cs`

Maps a `Role` → default flags for one module. Not a `BaseEntity` (no audit fields needed, no Guid PK).

```csharp
using CleanArchitecture.Domain.Enums;

namespace CleanArchitecture.Domain.Entities;

/// <summary>
/// Default permission flags for a Role on a specific module.
/// Composite key: (Role, Module).
/// </summary>
public class RolePermission
{
    public Role Role { get; set; }
    public string Module { get; set; } = string.Empty;
    public long Flags { get; set; }
}
```

**Key decisions**:
- `Module` is `string` (not `PermissionModule` enum) in the entity — EF stores it directly. Code that writes it uses `PermissionModule.Users.ToString()`.
- `Flags` is `long` — matches the `[Flags]` enum base type.
- No `Id` — composite PK on `(Role, Module)`.

---

### 5. `src/CleanArchitecture.Domain/Entities/UserPermissionOverride.cs`

Per-user overrides that are OR'd with role defaults.

```csharp
namespace CleanArchitecture.Domain.Entities;

/// <summary>
/// Per-user permission override for a specific module.
/// Effective = RolePermission.Flags | UserPermissionOverride.Flags
/// Composite key: (UserId, Module).
/// </summary>
public class UserPermissionOverride
{
    public Guid UserId { get; set; }
    public string Module { get; set; } = string.Empty;
    public long Flags { get; set; }

    // Navigation
    public User User { get; set; } = null!;
}
```

---

### 6. `src/CleanArchitecture.Domain/Interfaces/IPermissionRepository.cs`

```csharp
using CleanArchitecture.Domain.Entities;
using CleanArchitecture.Domain.Enums;

namespace CleanArchitecture.Domain.Interfaces;

/// <summary>
/// Permission data access contract.
/// </summary>
public interface IPermissionRepository
{
    /// <summary>Get all default permissions for a role.</summary>
    Task<IReadOnlyList<RolePermission>> GetByRoleAsync(
        Role role, CancellationToken ct = default);

    /// <summary>Get all user-specific overrides.</summary>
    Task<IReadOnlyList<UserPermissionOverride>> GetByUserIdAsync(
        Guid userId, CancellationToken ct = default);

    /// <summary>Get effective flags for a user on a specific module.</summary>
    Task<long> GetEffectiveFlagsAsync(
        Guid userId, Role role, string module, CancellationToken ct = default);

    // Write operations
    Task UpsertRolePermissionAsync(RolePermission permission, CancellationToken ct = default);
    Task UpsertUserOverrideAsync(UserPermissionOverride overrideEntity, CancellationToken ct = default);
}
```

---

### 7. `src/CleanArchitecture.Domain/Exceptions/ForbiddenException.cs`

```csharp
namespace CleanArchitecture.Domain.Exceptions;

/// <summary>
/// Thrown when a user lacks required permissions. Maps to HTTP 403.
/// </summary>
public class ForbiddenException : Exception
{
    public ForbiddenException(string message) : base(message) { }

    public ForbiddenException(string module, long requiredFlags)
        : base($"Insufficient permissions on '{module}'. Required flags: {requiredFlags}.") { }
}
```

---

## Verification Checklist
- [ ] All `[Flags]` enums use `: long` base type
- [ ] No references to EF Core, ASP.NET, or Infrastructure
- [ ] Existing `Role` enum unchanged
- [ ] Existing `User` entity unchanged
- [ ] `RolePermission` and `UserPermissionOverride` do NOT extend `BaseEntity`
