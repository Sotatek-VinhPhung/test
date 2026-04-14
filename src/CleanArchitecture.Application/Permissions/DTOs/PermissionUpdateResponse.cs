namespace CleanArchitecture.Application.Permissions.DTOs;

/// <summary>
/// Response after updating permissions.
/// </summary>
public class PermissionUpdateResponse
{
    /// <summary>
    /// The entity ID (RoleId or UserId) that was updated.
    /// </summary>
    public Guid EntityId { get; set; }
    
    /// <summary>
    /// Subsystem ID that was updated.
    /// </summary>
    public Guid SubsystemId { get; set; }
    
    /// <summary>
    /// Subsystem code.
    /// </summary>
    public string SubsystemCode { get; set; } = string.Empty;
    
    /// <summary>
    /// Updated permission flags.
    /// </summary>
    public long Flags { get; set; }
    
    /// <summary>
    /// Permission names granted.
    /// </summary>
    public List<string> PermissionNames { get; set; } = new();
    
    /// <summary>
    /// Timestamp of update.
    /// </summary>
    public DateTime UpdatedAt { get; set; }
}
