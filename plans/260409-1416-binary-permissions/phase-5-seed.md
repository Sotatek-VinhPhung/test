# Phase 5: Seed Data and Integration

**Depends on**: Phase 4 | **Blocks**: Nothing

## Goal

Provide default role permission seed data so the system works out of the box. Admin gets full permissions on all modules; User role gets read-only on most modules.

---

## Files to Create

### 1. src/CleanArchitecture.Infrastructure/Persistence/Seed/PermissionSeeder.cs

Static class with a SeedAsync method that:

1. Checks if RolePermissions table has any data (skip if already seeded)
2. Creates default RolePermission rows:

   Role.Admin:
   - Users module: (long)UserPermissions.All = 31
   - Orders module: (long)OrderPermissions.All = 63

   Role.User:
   - Users module: (long)UserPermissions.Read = 2
   - Orders module: (long)(OrderPermissions.Create | OrderPermissions.Read) = 3

3. Saves via context.SaveChangesAsync

Key implementation:

    public static class PermissionSeeder
    {
        public static async Task SeedAsync(AppDbContext context)
        {
            if (await context.RolePermissions.AnyAsync())
                return;

            var defaults = new List<RolePermission>
            {
                // Admin: full access
                new() { Role = Role.Admin, Module = nameof(PermissionModule.Users),
                         Flags = (long)UserPermissions.All },
                new() { Role = Role.Admin, Module = nameof(PermissionModule.Orders),
                         Flags = (long)OrderPermissions.All },
                // User: limited
                new() { Role = Role.User, Module = nameof(PermissionModule.Users),
                         Flags = (long)UserPermissions.Read },
                new() { Role = Role.User, Module = nameof(PermissionModule.Orders),
                         Flags = (long)(OrderPermissions.Create | OrderPermissions.Read) },
            };

            await context.RolePermissions.AddRangeAsync(defaults);
            await context.SaveChangesAsync();
        }
    }

### 2. src/CleanArchitecture.Infrastructure/Persistence/Seed/SeedExtensions.cs

Extension method on IApplicationBuilder or WebApplication:

    public static class SeedExtensions
    {
        public static async Task SeedPermissionsAsync(this WebApplication app)
        {
            using var scope = app.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await PermissionSeeder.SeedAsync(context);
        }
    }

---

## Files to Modify

### 3. src/CleanArchitecture.Api/Program.cs

Add before app.Run():

    await app.SeedPermissionsAsync();

This runs on every startup but is idempotent (skips if data exists).

---

## Default Permission Matrix

| Role  | Users Module          | Orders Module                 |
|-------|-----------------------|-------------------------------|
| Admin | Create,Read,Update,Delete,Export (31) | Create,Read,Update,Delete,Approve,Cancel (63) |
| User  | Read (2)              | Create,Read (3)               |

## Adding a New Module — Checklist

When adding a new module (e.g., Reports):

1. Add ReportPermissions [Flags] enum in Domain/Enums/
2. Add Reports value to PermissionModule enum
3. Add seed rows in PermissionSeeder for each Role
4. Use [RequirePermission(PermissionModule.Reports, ...)] on new controllers
5. No migration needed — Module is stored as string

---

## Verification Checklist

- [ ] First run seeds default permissions
- [ ] Second run skips (idempotent)
- [ ] Admin user gets full flags in JWT after login
- [ ] Regular user gets read-only flags in JWT
- [ ] New user registration assigns Role.User -> gets User-level permissions
