namespace CleanArchitecture.Domain.Enums;

/// <summary>
/// Identifies a permission module. Each module has its own [Flags] enum.
/// Stored as string in DB — adding new values requires no migration.
/// </summary>
public enum PermissionModule
{
    Users,
    Orders,
    Settings  // For admin/configuration operations
}
