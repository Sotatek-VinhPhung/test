using CleanArchitecture.Domain.Entities;
using CleanArchitecture.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CleanArchitecture.Infrastructure.Persistence.Seed;

/// <summary>
/// Seeder for new RBAC system (Subsystem-based).
/// Seeds: Subsystems, Roles, UserRoles (if needed), RoleSubsystemPermissions.
/// Replaces the old module-based PermissionSeeder.
/// </summary>
public static class RbacSeeder
{
    public static async Task SeedAsync(AppDbContext context)
    {
        try
        {
            // Check if RBAC already seeded (idempotent)
            // If migration hasn't been applied yet, skip seeding
            try
            {
                if (await context.Subsystems.AnyAsync() && 
                    await context.Roles.AnyAsync() && 
                    await context.RoleSubsystemPermissions.AnyAsync())
                {
                    return;
                }
            }
            catch (Exception ex) when (ex.Message.Contains("does not exist") || ex.Message.Contains("relation"))
            {
                // Tables don't exist yet - migration hasn't been applied
                System.Diagnostics.Debug.WriteLine($"RBAC tables not found. Please run: dotnet ef database update");
                throw;
            }

            // 1. Seed Subsystems
            await SeedSubsystemsAsync(context);

            // 2. Seed Roles
            await SeedRolesAsync(context);

            // 3. Seed RoleSubsystemPermissions
            await SeedRoleSubsystemPermissionsAsync(context);

            await context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"RBAC seeding error: {ex.Message}");
            throw;
        }
    }

    private static async Task SeedSubsystemsAsync(AppDbContext context)
    {
        // Check if subsystems already exist
        if (await context.Subsystems.AnyAsync())
            return;

        var subsystems = new List<Subsystem>
        {
            new()
            {
                Id = Guid.Parse("00000000-0000-0000-0000-000000000001"),
                Code = "Reports",
                Name = "Reports Module",
                Description = "Access to reports and dashboards",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new()
            {
                Id = Guid.Parse("00000000-0000-0000-0000-000000000002"),
                Code = "Users",
                Name = "Users Management",
                Description = "User and account management",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new()
            {
                Id = Guid.Parse("00000000-0000-0000-0000-000000000003"),
                Code = "Analytics",
                Name = "Analytics Module",
                Description = "Advanced analytics and insights",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new()
            {
                Id = Guid.Parse("00000000-0000-0000-0000-000000000004"),
                Code = "Settings",
                Name = "Settings Module",
                Description = "System configuration and settings",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new()
            {
                Id = Guid.Parse("00000000-0000-0000-0000-000000000005"),
                Code = "Audit",
                Name = "Audit Logs",
                Description = "Audit trail and logging",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new()
            {
                Id = Guid.Parse("00000000-0000-0000-0000-000000000006"),
                Code = "Orders",
                Name = "Orders Management",
                Description = "Order processing and management",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            }
        };

        await context.Subsystems.AddRangeAsync(subsystems);
    }

    private static async Task SeedRolesAsync(AppDbContext context)
    {
        // Check if roles already exist
        if (await context.Roles.AnyAsync())
            return;

        var roles = new List<Domain.Entities.Role>
        {
            new()
            {
                Id = Guid.Parse("10000000-0000-0000-0000-000000000001"),
                Code = Domain.Entities.Role.WellKnown.Admin,
                Name = "Administrator",
                Description = "Full system access",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new()
            {
                Id = Guid.Parse("10000000-0000-0000-0000-000000000002"),
                Code = Domain.Entities.Role.WellKnown.Manager,
                Name = "Manager",
                Description = "Department and report management",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new()
            {
                Id = Guid.Parse("10000000-0000-0000-0000-000000000003"),
                Code = Domain.Entities.Role.WellKnown.Editor,
                Name = "Editor",
                Description = "Content creation and editing",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new()
            {
                Id = Guid.Parse("10000000-0000-0000-0000-000000000004"),
                Code = Domain.Entities.Role.WellKnown.Viewer,
                Name = "Viewer",
                Description = "Read-only access",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            }
        };

        await context.Roles.AddRangeAsync(roles);
    }

    private static async Task SeedRoleSubsystemPermissionsAsync(AppDbContext context)
    {
        // Check if permissions already exist
        if (await context.RoleSubsystemPermissions.AnyAsync())
            return;

        var adminId = Guid.Parse("10000000-0000-0000-0000-000000000001");
        var managerId = Guid.Parse("10000000-0000-0000-0000-000000000002");
        var editorId = Guid.Parse("10000000-0000-0000-0000-000000000003");
        var viewerId = Guid.Parse("10000000-0000-0000-0000-000000000004");

        var reportsId = Guid.Parse("00000000-0000-0000-0000-000000000001");
        var usersId = Guid.Parse("00000000-0000-0000-0000-000000000002");
        var analyticsId = Guid.Parse("00000000-0000-0000-0000-000000000003");
        var settingsId = Guid.Parse("00000000-0000-0000-0000-000000000004");
        var auditId = Guid.Parse("00000000-0000-0000-0000-000000000005");
        var ordersId = Guid.Parse("00000000-0000-0000-0000-000000000006");

        // Calculate combined flags
        // All permissions = View | Create | Edit | Delete | Export | Approve | Execute | Audit | ManageUsers
        var allPermissions = (long)(
            Permission.View |
            Permission.Create |
            Permission.Edit |
            Permission.Delete |
            Permission.Export |
            Permission.Approve |
            Permission.Execute |
            Permission.Audit |
            Permission.ManageUsers |
            Permission.ManageRoles |
            Permission.ManagePermissions
        );

        // Manager permissions = View | Create | Edit | Approve | ManageUsers
        var managerPermissions = (long)(
            Permission.View |
            Permission.Create |
            Permission.Edit |
            Permission.Approve |
            Permission.ManageUsers
        );

        // Editor permissions = View | Create | Edit
        var editorPermissions = (long)(
            Permission.View |
            Permission.Create |
            Permission.Edit
        );

        // Viewer permissions = View only
        var viewerPermissions = (long)Permission.View;

        var permissions = new List<RoleSubsystemPermission>
        {
            // ===== ADMIN: Full access on all subsystems =====
            new()
            {
                RoleId = adminId,
                SubsystemId = reportsId,
                Flags = allPermissions,
                UpdatedAt = DateTime.UtcNow
            },
            new()
            {
                RoleId = adminId,
                SubsystemId = usersId,
                Flags = allPermissions,
                UpdatedAt = DateTime.UtcNow
            },
            new()
            {
                RoleId = adminId,
                SubsystemId = analyticsId,
                Flags = allPermissions,
                UpdatedAt = DateTime.UtcNow
            },
            new()
            {
                RoleId = adminId,
                SubsystemId = settingsId,
                Flags = allPermissions,
                UpdatedAt = DateTime.UtcNow
            },
            new()
            {
                RoleId = adminId,
                SubsystemId = auditId,
                Flags = allPermissions,
                UpdatedAt = DateTime.UtcNow
            },
            new()
            {
                RoleId = adminId,
                SubsystemId = ordersId,
                Flags = allPermissions,
                UpdatedAt = DateTime.UtcNow
            },

            // ===== MANAGER: Restricted access =====
            new()
            {
                RoleId = managerId,
                SubsystemId = reportsId,
                Flags = managerPermissions,
                UpdatedAt = DateTime.UtcNow
            },
            new()
            {
                RoleId = managerId,
                SubsystemId = usersId,
                Flags = managerPermissions,
                UpdatedAt = DateTime.UtcNow
            },
            new()
            {
                RoleId = managerId,
                SubsystemId = ordersId,
                Flags = managerPermissions,
                UpdatedAt = DateTime.UtcNow
            },
            new()
            {
                RoleId = managerId,
                SubsystemId = analyticsId,
                Flags = viewerPermissions, // View only
                UpdatedAt = DateTime.UtcNow
            },

            // ===== EDITOR: Limited access =====
            new()
            {
                RoleId = editorId,
                SubsystemId = reportsId,
                Flags = editorPermissions,
                UpdatedAt = DateTime.UtcNow
            },
            new()
            {
                RoleId = editorId,
                SubsystemId = usersId,
                Flags = viewerPermissions, // View only
                UpdatedAt = DateTime.UtcNow
            },
            new()
            {
                RoleId = editorId,
                SubsystemId = ordersId,
                Flags = editorPermissions,
                UpdatedAt = DateTime.UtcNow
            },

            // ===== VIEWER: Read-only access =====
            new()
            {
                RoleId = viewerId,
                SubsystemId = reportsId,
                Flags = viewerPermissions,
                UpdatedAt = DateTime.UtcNow
            },
            new()
            {
                RoleId = viewerId,
                SubsystemId = analyticsId,
                Flags = viewerPermissions,
                UpdatedAt = DateTime.UtcNow
            },
            new()
            {
                RoleId = viewerId,
                SubsystemId = ordersId,
                Flags = viewerPermissions,
                UpdatedAt = DateTime.UtcNow
            }
        };

        await context.RoleSubsystemPermissions.AddRangeAsync(permissions);
    }
}
