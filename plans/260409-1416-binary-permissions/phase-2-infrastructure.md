# Phase 2: Infrastructure — Persistence & JWT

> **Depends on**: Phase 1
> **Blocks**: Phase 3

## Goal
Wire up EF Core mappings, implement the permission repository, update JWT to carry permission claims.

---

## Files to Create

### 1. `src/CleanArchitecture.Infrastructure/Persistence/Configurations/RolePermissionConfiguration.cs`

```csharp
using CleanArchitecture.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CleanArchitecture.Infrastructure.Persistence.Configurations;

public class RolePermissionConfiguration : IEntityTypeConfiguration<RolePermission>
{
    public void Configure(EntityTypeBuilder<RolePermission> builder)
    {
        builder.ToTable("RolePermissions");

        // Composite PK: Role + Module
        builder.HasKey(rp => new { rp.Role, rp.Module });

        builder.Property(rp => rp.Role)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(rp => rp.Module)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(rp => rp.Flags)
            .IsRequired();
    }
}
```

**Notes**:
- `Role` stored as string (matches existing `UserConfiguration` pattern)
- `Module` stored as string — no migration when adding new modules
- `Flags` maps to `bigint` in PostgreSQL (default for `long`)

---

### 2. `src/CleanArchitecture.Infrastructure/Persistence/Configurations/UserPermissionOverrideConfiguration.cs`

```csharp
using CleanArchitecture.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CleanArchitecture.Infrastructure.Persistence.Configurations;

public class UserPermissionOverrideConfiguration : IEntityTypeConfiguration<UserPermissionOverride>
{
    public void Configure(EntityTypeBuilder<UserPermissionOverride> builder)
    {
        builder.ToTable("UserPermissionOverrides");

        // Composite PK: UserId + Module
        builder.HasKey(up => new { up.UserId, up.Module });

        builder.Property(up => up.Module)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(up => up.Flags)
            .IsRequired();

        builder.HasOne(up => up.User)
            .WithMany()
            .HasForeignKey(up => up.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
```

---

### 3. `src/CleanArchitecture.Infrastructure/Persistence/Repositories/PermissionRepository.cs`

```csharp
using CleanArchitecture.Domain.Entities;
using CleanArchitecture.Domain.Enums;
using CleanArchitecture.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CleanArchitecture.Infrastructure.Persistence.Repositories;

public class PermissionRepository : IPermissionRepository
{
    private readonly AppDbContext _context;

    public PermissionRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<RolePermission>> GetByRoleAsync(
        Role role, CancellationToken ct = default)
    {
        return await _context.RolePermissions
            .Where(rp => rp.Role == role)
            .AsNoTracking()
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<UserPermissionOverride>> GetByUserIdAsync(
        Guid userId, CancellationToken ct = default)
    {
        return await _context.UserPermissionOverrides
            .Where(up => up.UserId == userId)
            .AsNoTracking()
            .ToListAsync(ct);
    }

    public async Task<long> GetEffectiveFlagsAsync(
        Guid userId, Role role, string module, CancellationToken ct = default)
    {
        var roleFlags = await _context.RolePermissions
            .Where(rp => rp.Role == role && rp.Module == module)
            .Select(rp => rp.Flags)
            .FirstOrDefaultAsync(ct);

        var userFlags = await _context.UserPermissionOverrides
            .Where(up => up.UserId == userId && up.Module == module)
            .Select(up => up.Flags)
            .FirstOrDefaultAsync(ct);

        return roleFlags | userFlags; // Bitwise OR
    }

    public async Task UpsertRolePermissionAsync(
        RolePermission permission, CancellationToken ct = default)
    {
        var existing = await _context.RolePermissions
            .FirstOrDefaultAsync(rp => rp.Role == permission.Role
                && rp.Module == permission.Module, ct);

        if (existing is null)
            await _context.RolePermissions.AddAsync(permission, ct);
        else
            existing.Flags = permission.Flags;
    }

    public async Task UpsertUserOverrideAsync(
        UserPermissionOverride overrideEntity, CancellationToken ct = default)
    {
        var existing = await _context.UserPermissionOverrides
            .FirstOrDefaultAsync(up => up.UserId == overrideEntity.UserId
                && up.Module == overrideEntity.Module, ct);

        if (existing is null)
            await _context.UserPermissionOverrides.AddAsync(overrideEntity, ct);
        else
            existing.Flags = overrideEntity.Flags;
    }
}
```

---

### 4. `src/CleanArchitecture.Infrastructure/Auth/PermissionClaimNames.cs`

```csharp
namespace CleanArchitecture.Infrastructure.Auth;

/// <summary>
/// Constants for permission-related JWT claim names.
/// Format: "perm:{Module}" with value = flags as string.
/// </summary>
public static class PermissionClaimNames
{
    public const string Prefix = "perm:";

    public static string ForModule(string module) => $"{Prefix}{module}";
}
```

---

## Files to Modify

### 5. `src/CleanArchitecture.Infrastructure/Persistence/AppDbContext.cs`

**Add** two new DbSets:

```csharp
// ADD these two lines after the existing Users DbSet:
public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
public DbSet<UserPermissionOverride> UserPermissionOverrides => Set<UserPermissionOverride>();
```

**Add** using directives if needed. No other changes — configurations are auto-discovered via `ApplyConfigurationsFromAssembly`.

---

### 6. `src/CleanArchitecture.Infrastructure/Persistence/UnitOfWork.cs`

**Add** `IPermissionRepository` to `IUnitOfWork` and `UnitOfWork`:

First, update **`IUnitOfWork`** in Domain:
```csharp
// In src/CleanArchitecture.Domain/Interfaces/IUnitOfWork.cs — ADD:
IPermissionRepository Permissions { get; }
```

Then update the implementation:
```csharp
// In UnitOfWork.cs — ADD field and property:
private IPermissionRepository? _permissions;
public IPermissionRepository Permissions => _permissions ??= new PermissionRepository(_context);
```

---

### 7. `src/CleanArchitecture.Infrastructure/DependencyInjection.cs`

**Add** after the `IUserRepository` registration:
```csharp
services.AddScoped<IPermissionRepository, PermissionRepository>();
```

---

## Migration

After all files are in place:
```bash
dotnet ef migrations add AddPermissionTables \
  --project src/CleanArchitecture.Infrastructure \
  --startup-project src/CleanArchitecture.Api
```

Expected tables:
- `RolePermissions` (Role varchar(20), Module varchar(50), Flags bigint) — PK: (Role, Module)
- `UserPermissionOverrides` (UserId uuid, Module varchar(50), Flags bigint) — PK: (UserId, Module), FK: UserId → Users.Id

---

## Verification Checklist
- [ ] `dotnet build` succeeds with no errors
- [ ] Migration generates correct composite PKs
- [ ] `RolePermission.Role` stored as string (not int)
- [ ] FK from `UserPermissionOverrides.UserId` → `Users.Id` with CASCADE delete
- [ ] No changes to existing `Users` table schema
