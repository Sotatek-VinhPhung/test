using CleanArchitecture.Domain.Entities;
using CleanArchitecture.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CleanArchitecture.Infrastructure.Persistence.Seed;

public static class PermissionSeeder
{
    public static async Task SeedAsync(AppDbContext context)
    {
        if (await context.RolePermissions.AnyAsync())
            return;

        var defaults = new List<RolePermission>
        {
            // Admin: full access on all modules
            new() { Role = Role.Admin, Module = nameof(PermissionModule.Users),
                     Flags = (long)UserPermissions.All },
            new() { Role = Role.Admin, Module = nameof(PermissionModule.Orders),
                     Flags = (long)OrderPermissions.All },

            // User: limited access
            new() { Role = Role.User, Module = nameof(PermissionModule.Users),
                     Flags = (long)UserPermissions.Read },
            new() { Role = Role.User, Module = nameof(PermissionModule.Orders),
                     Flags = (long)(OrderPermissions.Create | OrderPermissions.Read) },
        };

        await context.RolePermissions.AddRangeAsync(defaults);
        await context.SaveChangesAsync();
    }
}
