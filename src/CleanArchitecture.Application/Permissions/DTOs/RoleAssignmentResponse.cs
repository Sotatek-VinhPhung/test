namespace CleanArchitecture.Application.Permissions.DTOs;

/// <summary>
/// Response after assigning or revoking a role.
/// </summary>
public class RoleAssignmentResponse
{
    /// <summary>
    /// User ID.
    /// </summary>
    public Guid UserId { get; set; }
    
    /// <summary>
    /// Role ID.
    /// </summary>
    public Guid RoleId { get; set; }
    
    /// <summary>
    /// Role code (e.g., "Admin", "Manager", "Viewer").
    /// </summary>
    public string RoleCode { get; set; } = string.Empty;
    
    /// <summary>
    /// Operation performed (e.g., "Assigned", "Revoked").
    /// </summary>
    public string Operation { get; set; } = string.Empty;
    
    /// <summary>
    /// Timestamp of the operation.
    /// </summary>
    public DateTime CreatedAt { get; set; }
}
