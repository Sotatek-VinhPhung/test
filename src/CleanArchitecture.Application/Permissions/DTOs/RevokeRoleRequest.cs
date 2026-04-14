namespace CleanArchitecture.Application.Permissions.DTOs;

/// <summary>
/// Request to revoke a role from a user.
/// </summary>
public class RevokeRoleRequest
{
    /// <summary>
    /// User ID to revoke the role from.
    /// </summary>
    public Guid UserId { get; set; }
    
    /// <summary>
    /// Role ID to revoke.
    /// </summary>
    public Guid RoleId { get; set; }
}
