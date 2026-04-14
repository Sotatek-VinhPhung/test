using CleanArchitecture.Application.Notifications.DTOs;

namespace CleanArchitecture.Application.Notifications.Interfaces;

/// <summary>
/// Service for broadcasting permission change notifications to connected clients via SignalR
/// </summary>
public interface IPermissionNotificationService
{
    /// <summary>
    /// Notify all clients that a role has been assigned to a user
    /// </summary>
    Task NotifyRoleAssignedAsync(Guid userId, Guid roleId, string roleName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Notify all clients that a role has been revoked from a user
    /// </summary>
    Task NotifyRoleRevokedAsync(Guid userId, Guid roleId, string roleName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Notify all clients that role permissions have been updated
    /// </summary>
    Task NotifyPermissionsUpdatedAsync(Guid roleId, string roleName, string subsystemCode, long permissions, List<string>? permissionNames = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Notify a specific user that their permissions have been overridden
    /// </summary>
    Task NotifyUserPermissionOverrideAsync(Guid userId, string subsystemCode, long permissions, bool isRemoved, List<string>? permissionNames = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Notify a specific user that their permission override has been removed
    /// </summary>
    Task NotifyUserPermissionOverrideRemovedAsync(Guid userId, string subsystemCode, CancellationToken cancellationToken = default);
}
