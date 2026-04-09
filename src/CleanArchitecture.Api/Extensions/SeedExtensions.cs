using CleanArchitecture.Infrastructure.Persistence;
using CleanArchitecture.Infrastructure.Persistence.Seed;
using Microsoft.Extensions.DependencyInjection;

namespace CleanArchitecture.Api.Extensions;

public static class SeedExtensions
{
    public static async Task SeedPermissionsAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await PermissionSeeder.SeedAsync(context);
    }
}
