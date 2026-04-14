using CleanArchitecture.Application.Permissions.DTOs;
using CleanArchitecture.Application.Permissions.Interfaces;
using CleanArchitecture.Application.Notifications.Interfaces;
using CleanArchitecture.Domain.Entities;
using CleanArchitecture.Domain.Enums;
using CleanArchitecture.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CleanArchitecture.Infrastructure.Permissions;

/// <summary>
/// Implementation for managing user roles and permissions.
/// </summary>
public class RoleManagementService : IRoleManagementService
{
    private readonly AppDbContext _context;
    private readonly IUserContextService _userContextService;
    private readonly IPermissionNotificationService _notificationService;

    public RoleManagementService(
        AppDbContext context,
        IUserContextService userContextService,
        IPermissionNotificationService notificationService)
    {
        _context = context;
        _userContextService = userContextService;
        _notificationService = notificationService;
    }
    
    /// <summary>
    /// Assign a role to a user.
    /// </summary>
    public async Task<RoleAssignmentResponse> AssignRoleToUserAsync(
        Guid userId, 
        Guid roleId,
        CancellationToken cancellationToken = default)
    {
        // Verify user exists
        var user = await _context.Users.FindAsync(new object[] { userId }, cancellationToken);
        if (user == null)
            throw new InvalidOperationException($"User {userId} not found");

        // Verify role exists
        var role = await _context.Roles.FindAsync(new object[] { roleId }, cancellationToken);
        if (role == null)
            throw new InvalidOperationException($"Role {roleId} not found");

        // Check if user already has this role
        var existingUserRole = await _context.UserRoles
            .FirstOrDefaultAsync(ur => ur.UserId == userId && ur.RoleId == roleId, cancellationToken);

        if (existingUserRole != null)
            throw new InvalidOperationException($"User already has role {role.Code}");

        // Create user role mapping
        var userRole = new UserRole
        {
            UserId = userId,
            RoleId = roleId
        };

        _context.UserRoles.Add(userRole);
        await _context.SaveChangesAsync(cancellationToken);

        // Invalidate user context cache
        await _userContextService.InvalidateUserContextAsync(userId, cancellationToken);

        // Send SignalR notification
        await _notificationService.NotifyRoleAssignedAsync(userId, roleId, role.Code, cancellationToken);

        return new RoleAssignmentResponse
        {
            UserId = userId,
            RoleId = roleId,
            RoleCode = role.Code,
            Operation = "Assigned",
            CreatedAt = DateTime.UtcNow
        };
    }
    
    /// <summary>
    /// Revoke a role from a user.
    /// </summary>
    public async Task<RoleAssignmentResponse> RevokeRoleFromUserAsync(
        Guid userId, 
        Guid roleId,
        CancellationToken cancellationToken = default)
    {
        // Find user role mapping
        var userRole = await _context.UserRoles
            .FirstOrDefaultAsync(ur => ur.UserId == userId && ur.RoleId == roleId, cancellationToken);

        if (userRole == null)
            throw new InvalidOperationException($"User does not have this role");

        // Get role info before deleting
        var role = await _context.Roles.FindAsync(new object[] { roleId }, cancellationToken);

        // Delete the mapping
        _context.UserRoles.Remove(userRole);
        await _context.SaveChangesAsync(cancellationToken);

        // Invalidate user context cache
        await _userContextService.InvalidateUserContextAsync(userId, cancellationToken);

        // Send SignalR notification
        await _notificationService.NotifyRoleRevokedAsync(userId, roleId, role?.Code ?? "Unknown", cancellationToken);

        return new RoleAssignmentResponse
        {
            UserId = userId,
            RoleId = roleId,
            RoleCode = role?.Code ?? "Unknown",
            Operation = "Revoked",
            CreatedAt = DateTime.UtcNow
        };
    }
    
    /// <summary>
    /// Update permissions for a role in a specific subsystem.
    /// </summary>
    public async Task<PermissionUpdateResponse> UpdateRolePermissionsAsync(
        Guid roleId, 
        Guid subsystemId, 
        long flags,
        CancellationToken cancellationToken = default)
    {
        // Verify role exists
        var role = await _context.Roles.FindAsync(new object[] { roleId }, cancellationToken);
        if (role == null)
            throw new InvalidOperationException($"Role {roleId} not found");

        // Verify subsystem exists
        var subsystem = await _context.Subsystems.FindAsync(new object[] { subsystemId }, cancellationToken);
        if (subsystem == null)
            throw new InvalidOperationException($"Subsystem {subsystemId} not found");

        // Find or create role subsystem permission mapping
        var rolePermission = await _context.RoleSubsystemPermissions
            .FirstOrDefaultAsync(
                rsp => rsp.RoleId == roleId && rsp.SubsystemId == subsystemId,
                cancellationToken);

        if (rolePermission == null)
        {
            // Create new
            rolePermission = new RoleSubsystemPermission
            {
                RoleId = roleId,
                SubsystemId = subsystemId,
                Flags = flags,
                UpdatedAt = DateTime.UtcNow
            };
            _context.RoleSubsystemPermissions.Add(rolePermission);
        }
        else
        {
            // Update existing
            rolePermission.Flags = flags;
            rolePermission.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync(cancellationToken);

        // Invalidate cache for all users with this role
        await InvalidateRoleUsersContextAsync(roleId, cancellationToken);

        // Get permission names
        var permissionNames = GetPermissionNames(flags);

        // Send SignalR notification to all clients
        await _notificationService.NotifyPermissionsUpdatedAsync(
            roleId, role.Code, subsystem.Code, flags, permissionNames, cancellationToken);

        return new PermissionUpdateResponse
        {
            EntityId = roleId,
            SubsystemId = subsystemId,
            SubsystemCode = subsystem.Code,
            Flags = flags,
            PermissionNames = permissionNames,
            UpdatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Override user permissions in a specific subsystem.
    /// NOTE: User overrides currently not implemented in new RBAC.
    /// Assign multiple roles to users if needed for different permission levels.
    /// </summary>
    [Obsolete("User permission overrides not implemented in new RBAC. Use role assignment instead.")]
    public async Task<PermissionUpdateResponse> OverrideUserPermissionsAsync(
        Guid userId, 
        Guid subsystemId, 
        long flags,
        string? reason = null,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("User permission overrides not supported in new RBAC system. Assign multiple roles instead.");
    }

    /// <summary>
    /// Remove a user permission override.
    /// NOTE: Not applicable for new RBAC.
    /// </summary>
    [Obsolete("User permission overrides not implemented in new RBAC.")]
    public async Task RemoveUserPermissionOverrideAsync(
        Guid userId, 
        Guid subsystemId,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("User permission overrides not supported in new RBAC system.");
    }
    
    /// <summary>
    /// Get all users with a specific role.
    /// </summary>
    public async Task<List<Guid>> GetUsersWithRoleAsync(
        Guid roleId,
        CancellationToken cancellationToken = default)
    {
        return await _context.UserRoles
            .Where(ur => ur.RoleId == roleId)
            .Select(ur => ur.UserId)
            .ToListAsync(cancellationToken);
    }
    
    /// <summary>
    /// Helper: Convert flags to permission names.
    /// </summary>
    private static List<string> GetPermissionNames(long flags)
    {
        var names = new List<string>();
        var permission = (Permission)flags;
        
        foreach (Permission p in Enum.GetValues(typeof(Permission)))
        {
            if (p != Permission.None && permission.HasPermission(p))
            {
                names.Add(p.ToString());
            }
        }
        
        return names;
    }
    
    /// <summary>
    /// Helper: Invalidate context for all users with a specific role.
    /// </summary>
    private async Task InvalidateRoleUsersContextAsync(Guid roleId, CancellationToken cancellationToken)
    {
        var userIds = await GetUsersWithRoleAsync(roleId, cancellationToken);
        
        foreach (var userId in userIds)
        {
            await _userContextService.InvalidateUserContextAsync(userId, cancellationToken);
        }
    }
}
