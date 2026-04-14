namespace CleanArchitecture.Application.Permissions.DTOs;

/// <summary>
/// Request to update permissions for a role in a specific subsystem.
/// </summary>
public class UpdateRolePermissionsRequest
{
    /// <summary>
    /// Role ID to update permissions for.
    /// </summary>
    public Guid RoleId { get; set; }
    
    /// <summary>
    /// Subsystem ID to update permissions for.
    /// </summary>
    public Guid SubsystemId { get; set; }
    
    /// <summary>
    /// New permission flags (bitwise combined).
    /// </summary>
    public long Flags { get; set; }
    
    /// <summary>
    /// Alternative: list of permission names to set (View, Create, Edit, Delete, etc.)
    /// If provided, will be converted to flags via bitwise OR.
    /// </summary>
    public List<string>? PermissionNames { get; set; }
}
