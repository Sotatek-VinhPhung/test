using CleanArchitecture.Infrastructure.Persistence;
using CleanArchitecture.Infrastructure.Persistence.Seed;
using Microsoft.Extensions.DependencyInjection;

namespace CleanArchitecture.Api.Extensions;

public static class SeedExtensions
{
    /// <summary>
    /// Seed RBAC system data (Subsystems, Roles, RoleSubsystemPermissions).
    /// Uses new Subsystem-based RBAC (replacing legacy module-based approach).
    /// </summary>
    public static async Task SeedRbacAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await RbacSeeder.SeedAsync(context);
    }
}
