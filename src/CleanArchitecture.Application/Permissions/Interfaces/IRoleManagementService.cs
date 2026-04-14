using CleanArchitecture.Application.Permissions.DTOs;

namespace CleanArchitecture.Application.Permissions.Interfaces;

/// <summary>
/// Interface for managing user roles and permissions.
/// Handles assignment/revocation of roles and permission updates.
/// </summary>
public interface IRoleManagementService
{
    /// <summary>
    /// Assign a role to a user.
    /// </summary>
    Task<RoleAssignmentResponse> AssignRoleToUserAsync(
        Guid userId, 
        Guid roleId,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Revoke a role from a user.
    /// </summary>
    Task<RoleAssignmentResponse> RevokeRoleFromUserAsync(
        Guid userId, 
        Guid roleId,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Update permissions for a role in a specific subsystem.
    /// </summary>
    Task<PermissionUpdateResponse> UpdateRolePermissionsAsync(
        Guid roleId, 
        Guid subsystemId, 
        long flags,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Override user permissions in a specific subsystem.
    /// Overrides take precedence over role-based permissions.
    /// </summary>
    Task<PermissionUpdateResponse> OverrideUserPermissionsAsync(
        Guid userId, 
        Guid subsystemId, 
        long flags,
        string? reason = null,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Remove a user permission override.
    /// </summary>
    Task RemoveUserPermissionOverrideAsync(
        Guid userId, 
        Guid subsystemId,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get all users with a specific role.
    /// </summary>
    Task<List<Guid>> GetUsersWithRoleAsync(
        Guid roleId,
        CancellationToken cancellationToken = default);
}
