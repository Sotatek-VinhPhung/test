using CleanArchitecture.Domain.Common;

namespace CleanArchitecture.Domain.Entities;

/// <summary>
/// Role entity for RBAC (Role-Based Access Control).
/// Replaces the simple Role enum for more flexibility.
/// Supports many-to-many relationship with users and permissions.
/// </summary>
public class Role : BaseEntity
{
    /// <summary>
    /// Unique code for the role (e.g., "Admin", "Manager", "Viewer").
    /// Used for lookups and programmatic reference.
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Human-readable name for the role.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Optional description of the role's responsibilities.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Indicates if this role is active/available for assignment.
    /// </summary>
    public bool IsActive { get; set; } = true;
    
    // Navigation properties
    
    /// <summary>
    /// Users who have been assigned this role.
    /// </summary>
    public ICollection<UserRole> UserRoles { get; set; } = [];
    
    /// <summary>
    /// Permissions this role has on various subsystems.
    /// </summary>
    public ICollection<RoleSubsystemPermission> RoleSubsystemPermissions { get; set; } = [];
    
    /// <summary>
    /// Well-known role codes for common scenarios.
    /// </summary>
    public static class WellKnown
    {
        public const string Admin = "Admin";
        public const string Manager = "Manager";
        public const string Editor = "Editor";
        public const string Viewer = "Viewer";
        public const string Guest = "Guest";
    }
}
