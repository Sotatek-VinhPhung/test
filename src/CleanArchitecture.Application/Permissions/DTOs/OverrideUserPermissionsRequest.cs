namespace CleanArchitecture.Application.Permissions.DTOs;

/// <summary>
/// Request to override permissions for a specific user in a subsystem.
/// Overrides take precedence over role-based permissions.
/// </summary>
public class OverrideUserPermissionsRequest
{
    /// <summary>
    /// User ID to override permissions for.
    /// </summary>
    public Guid UserId { get; set; }
    
    /// <summary>
    /// Subsystem ID to override permissions for.
    /// </summary>
    public Guid SubsystemId { get; set; }
    
    /// <summary>
    /// Permission flags to grant/override.
    /// </summary>
    public long Flags { get; set; }
    
    /// <summary>
    /// Alternative: list of permission names (View, Create, Edit, Delete, etc.)
    /// If provided, will be converted to flags via bitwise OR.
    /// </summary>
    public List<string>? PermissionNames { get; set; }
    
    /// <summary>
    /// Reason for the override (for audit purposes).
    /// </summary>
    public string? Reason { get; set; }
}
