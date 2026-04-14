namespace CleanArchitecture.Application.Permissions.DTOs;

/// <summary>
/// Request to assign a role to a user.
/// </summary>
public class AssignRoleRequest
{
    /// <summary>
    /// User ID to assign the role to.
    /// </summary>
    public Guid UserId { get; set; }
    
    /// <summary>
    /// Role ID to assign.
    /// </summary>
    public Guid RoleId { get; set; }
}
