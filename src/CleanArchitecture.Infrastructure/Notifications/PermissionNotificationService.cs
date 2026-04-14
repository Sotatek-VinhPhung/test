using CleanArchitecture.Application.Notifications.DTOs;
using CleanArchitecture.Application.Notifications.Interfaces;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace CleanArchitecture.Infrastructure.Notifications;

/// <summary>
/// Implementation of IPermissionNotificationService using SignalR
/// Broadcasts permission changes to connected clients in real-time
/// </summary>
public class PermissionNotificationService : IPermissionNotificationService
{
    private readonly dynamic _hubContext;
    private readonly ILogger<PermissionNotificationService> _logger;
    private const string HubMethodRoleAssigned = "RoleAssigned";
    private const string HubMethodRoleRevoked = "RoleRevoked";
    private const string HubMethodPermissionsUpdated = "PermissionsUpdated";
    private const string HubMethodUserPermissionOverride = "UserPermissionOverride";

    public PermissionNotificationService(
        dynamic hubContext,
        ILogger<PermissionNotificationService> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task NotifyRoleAssignedAsync(Guid userId, Guid roleId, string roleName, CancellationToken cancellationToken = default)
    {
        try
        {
            var notification = new RoleAssignedNotificationDto
            {
                UserId = userId,
                RoleId = roleId,
                RoleName = roleName,
                ChangeType = "RoleAssigned",
                ChangedAt = DateTime.UtcNow,
                Details = $"Role '{roleName}' has been assigned to your account"
            };

            await _hubContext.Clients
                .Group($"user-{userId}")
                .SendAsync(HubMethodRoleAssigned, notification, cancellationToken);

            _logger.LogInformation("Notified user {UserId} about role assignment: {RoleName}", userId, roleName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error notifying user {UserId} about role assignment", userId);
        }
    }

    public async Task NotifyRoleRevokedAsync(Guid userId, Guid roleId, string roleName, CancellationToken cancellationToken = default)
    {
        try
        {
            var notification = new RoleRevokedNotificationDto
            {
                UserId = userId,
                RoleId = roleId,
                RoleName = roleName,
                ChangeType = "RoleRevoked",
                ChangedAt = DateTime.UtcNow,
                Details = $"Role '{roleName}' has been revoked from your account"
            };

            await _hubContext.Clients
                .Group($"user-{userId}")
                .SendAsync(HubMethodRoleRevoked, notification, cancellationToken);

            _logger.LogInformation("Notified user {UserId} about role revocation: {RoleName}", userId, roleName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error notifying user {UserId} about role revocation", userId);
        }
    }

    public async Task NotifyPermissionsUpdatedAsync(
        Guid roleId,
        string roleName,
        string subsystemCode,
        long permissions,
        List<string>? permissionNames = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var notification = new PermissionsUpdatedNotificationDto
            {
                RoleId = roleId,
                RoleName = roleName,
                SubsystemCode = subsystemCode,
                Permissions = permissions,
                PermissionNames = permissionNames,
                ChangeType = "PermissionsUpdated",
                ChangedAt = DateTime.UtcNow,
                Details = $"Permissions for role '{roleName}' in '{subsystemCode}' have been updated"
            };

            // Broadcast to all clients (all users with this role will receive)
            await _hubContext.Clients
                .All
                .SendAsync(HubMethodPermissionsUpdated, notification, cancellationToken);

            _logger.LogInformation(
                "Broadcasted permission update for role {RoleName} in subsystem {SubsystemCode}",
                roleName,
                subsystemCode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error broadcasting permission update for role {RoleId}", roleId);
        }
    }

    public async Task NotifyUserPermissionOverrideAsync(
        Guid userId,
        string subsystemCode,
        long permissions,
        bool isRemoved,
        List<string>? permissionNames = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var notification = new UserPermissionOverrideNotificationDto
            {
                UserId = userId,
                SubsystemCode = subsystemCode,
                Permissions = permissions,
                IsRemoved = isRemoved,
                PermissionNames = permissionNames,
                ChangeType = isRemoved ? "PermissionOverrideRemoved" : "PermissionOverrideApplied",
                ChangedAt = DateTime.UtcNow,
                Details = isRemoved
                    ? $"Permission override for '{subsystemCode}' has been removed"
                    : $"Permission override for '{subsystemCode}' has been applied"
            };

            await _hubContext.Clients
                .Group($"user-{userId}")
                .SendAsync(HubMethodUserPermissionOverride, notification, cancellationToken);

            _logger.LogInformation(
                "Notified user {UserId} about permission override in subsystem {SubsystemCode}",
                userId,
                subsystemCode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error notifying user {UserId} about permission override", userId);
        }
    }

    public async Task NotifyUserPermissionOverrideRemovedAsync(
        Guid userId,
        string subsystemCode,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var notification = new UserPermissionOverrideNotificationDto
            {
                UserId = userId,
                SubsystemCode = subsystemCode,
                IsRemoved = true,
                ChangeType = "PermissionOverrideRemoved",
                ChangedAt = DateTime.UtcNow,
                Details = $"Permission override for '{subsystemCode}' has been removed"
            };

            await _hubContext.Clients
                .Group($"user-{userId}")
                .SendAsync(HubMethodUserPermissionOverride, notification, cancellationToken);

            _logger.LogInformation(
                "Notified user {UserId} about permission override removal from subsystem {SubsystemCode}",
                userId,
                subsystemCode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error notifying user {UserId} about permission override removal", userId);
        }
    }
}
