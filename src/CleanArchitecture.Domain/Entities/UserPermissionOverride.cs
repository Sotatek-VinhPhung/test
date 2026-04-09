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
