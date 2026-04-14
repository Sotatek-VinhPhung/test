using CleanArchitecture.Domain.Enums;

namespace CleanArchitecture.Domain.Entities;

/// <summary>
/// Maps Role to Subsystem with specific permission flags.
/// Enables fine-grained, role-based access control per subsystem.
/// Composite key: (RoleId, SubsystemId).
/// </summary>
public class RoleSubsystemPermission
{
    /// <summary>
    /// The role that has these permissions.
    /// </summary>
    public Guid RoleId { get; set; }
    
    /// <summary>
    /// The subsystem these permissions apply to.
    /// </summary>
    public Guid SubsystemId { get; set; }
    
    /// <summary>
    /// Bitwise flags representing the permitted operations.
    /// Stored as long (64-bit) to support up to 64 permission flags.
    /// Cast between Permission enum and long as needed.
    /// </summary>
    public long Flags { get; set; }
    
    /// <summary>
    /// Timestamp when these permissions were last updated.
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public Domain.Entities.Role Role { get; set; } = null!;
    public Subsystem Subsystem { get; set; } = null!;
    
    // Convenience methods
    
    /// <summary>
    /// Check if a specific permission is granted.
    /// </summary>
    public bool HasPermission(Permission permission)
    {
        return ((Permission)Flags).HasPermission(permission);
    }
    
    /// <summary>
    /// Check if all specified permissions are granted.
    /// </summary>
    public bool HasAllPermissions(params Permission[] permissions)
    {
        var combined = PermissionExtensions.Merge(permissions);
        return ((Permission)Flags).HasPermission(combined);
    }
    
    /// <summary>
    /// Grant a permission flag.
    /// </summary>
    public void GrantPermission(Permission permission)
    {
        Flags |= (long)permission;
        UpdatedAt = DateTime.UtcNow;
    }
    
    /// <summary>
    /// Revoke a permission flag.
    /// </summary>
    public void RevokePermission(Permission permission)
    {
        Flags &= ~(long)permission;
        UpdatedAt = DateTime.UtcNow;
    }
    
    /// <summary>
    /// Set permissions to a specific value, replacing all existing flags.
    /// </summary>
    public void SetPermissions(Permission permissions)
    {
        Flags = (long)permissions;
        UpdatedAt = DateTime.UtcNow;
    }
}
