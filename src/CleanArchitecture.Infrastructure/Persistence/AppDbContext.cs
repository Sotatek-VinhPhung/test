using CleanArchitecture.Domain.Common;
using CleanArchitecture.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CleanArchitecture.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    // User table (required)
    public DbSet<User> Users => Set<User>();

    // New RBAC tables (replacing legacy module-based approach)
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<Subsystem> Subsystems => Set<Subsystem>();
    public DbSet<RoleSubsystemPermission> RoleSubsystemPermissions => Set<RoleSubsystemPermission>();

    // Export feature
    public DbSet<ExportedFile> ExportedFiles => Set<ExportedFile>();

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        try
        {
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Configuration error: {ex.InnerException?.Message}");
            throw;
        }
    }

    /// <summary>
    /// Auto-set CreatedAt/UpdatedAt timestamps on save.
    /// </summary>
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = DateTime.UtcNow;
                    break;
                case EntityState.Modified:
                    entry.Entity.UpdatedAt = DateTime.UtcNow;
                    break;
            }
        }

        return await base.SaveChangesAsync(cancellationToken);
    }
}
