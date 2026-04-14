using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using CleanArchitecture.Application.Permissions.Interfaces;
using CleanArchitecture.Application.Permissions.DTOs;
using CleanArchitecture.Domain.Enums;
using CleanArchitecture.Infrastructure.Persistence.Repositories;
using CleanArchitecture.Api.Authorization;

namespace CleanArchitecture.Api.Controllers;

/// <summary>
/// API endpoints for user permission management and querying.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PermissionsController : ControllerBase
{
    private readonly IUserContextService _userContextService;
    private readonly IRoleManagementService _roleManagementService;
    private readonly RoleRepository _roleRepository;
    private readonly SubsystemRepository _subsystemRepository;

    public PermissionsController(
        IUserContextService userContextService,
        IRoleManagementService roleManagementService,
        RoleRepository roleRepository,
        SubsystemRepository subsystemRepository)
    {
        _userContextService = userContextService;
        _roleManagementService = roleManagementService;
        _roleRepository = roleRepository;
        _subsystemRepository = subsystemRepository;
    }
    
    /// <summary>
    /// Get all permissions for the current user.
    /// Returns a dictionary mapping subsystem codes to permission flags.
    /// </summary>
    /// <returns>User's complete permission context</returns>
    [HttpGet("me")]
    public async Task<IActionResult> GetMyPermissions()
    {
        // Get current user ID from claims
        var userIdClaim = User.FindFirst("sub") ?? User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier");
        if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
            return Unauthorized();
        
        var context = await _userContextService.GetUserContextAsync(userId);
        if (context == null)
            return NotFound("User context not found");
        
        return Ok(new
        {
            userId = context.UserId,
            email = context.Email,
            roleIds = context.RoleIds,
            subsystemPermissions = context.SubsystemPermissions
                .ToDictionary(
                    kvp => kvp.Key,
                    kvp => GetPermissionObject((Permission)kvp.Value))
        });
    }
    
    /// <summary>
    /// Get permissions for a specific subsystem for the current user.
    /// Returns permission flags as boolean flags for UI consumption.
    /// Example: { "view": true, "create": false, "edit": true, "delete": false, "export": true }
    /// </summary>
    /// <param name="subsystemCode">The subsystem code (e.g., "Reports", "Users")</param>
    /// <returns>Permission flags for the subsystem</returns>
    [HttpGet("subsystems/{subsystemCode}")]
    public async Task<IActionResult> GetSubsystemPermissions(string subsystemCode)
    {
        // Get current user ID
        var userIdClaim = User.FindFirst("sub") ?? User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier");
        if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
            return Unauthorized();
        
        var permissions = await _userContextService.GetSubsystemPermissionsAsync(userId, subsystemCode);
        
        return Ok(new
        {
            subsystem = subsystemCode,
            permissions = GetPermissionObject(permissions),
            rawFlags = (long)permissions
        });
    }
    
    /// <summary>
    /// Check if current user has a specific permission in a subsystem.
    /// </summary>
    /// <param name="subsystemCode">The subsystem code</param>
    /// <param name="permissionCode">The permission name (e.g., "View", "Create", "Edit")</param>
    /// <returns>Boolean indicating if user has the permission</returns>
    [HttpGet("subsystems/{subsystemCode}/check/{permissionCode}")]
    public async Task<IActionResult> CheckPermission(string subsystemCode, string permissionCode)
    {
        // Get current user ID
        var userIdClaim = User.FindFirst("sub") ?? User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier");
        if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
            return Unauthorized();
        
        // Parse permission from code
        if (!Enum.TryParse<Permission>(permissionCode, ignoreCase: true, out var permission))
            return BadRequest($"Invalid permission code: {permissionCode}");
        
        var hasPermission = await _userContextService.HasPermissionAsync(userId, subsystemCode, permission);
        
        return Ok(new
        {
            subsystem = subsystemCode,
            permission = permissionCode,
            granted = hasPermission
        });
    }
    
    /// <summary>
    /// Get all available subsystems and their metadata.
    /// </summary>
    [HttpGet("subsystems")]
    public async Task<IActionResult> GetAvailableSubsystems()
    {
        var subsystems = await _subsystemRepository.GetActiveSubsystemsAsync();
        
        return Ok(subsystems.Select(s => new
        {
            id = s.Id,
            code = s.Code,
            name = s.Name,
            description = s.Description
        }));
    }
    
    /// <summary>
    /// Get all available permissions and their values.
    /// This helps UI build dynamic permission checkboxes.
    /// </summary>
    [HttpGet("available")]
    public IActionResult GetAvailablePermissions()
    {
        var permissions = Enum.GetValues(typeof(Permission))
            .Cast<Permission>()
            .Where(p => p != Permission.None)
            .Select(p => new
            {
                name = p.ToString(),
                value = (long)p,
                description = GetPermissionDescription(p)
            })
            .ToList();
        
        return Ok(permissions);
    }
    
    /// <summary>
    /// Get all roles available in the system.
    /// </summary>
    [HttpGet("roles")]
    [Authorize(Policy = "AdminOnly")] // Only admins can view all roles
    public async Task<IActionResult> GetRoles()
    {
        var roles = await _roleRepository.GetActiveRolesAsync();
        
        return Ok(roles.Select(r => new
        {
            id = r.Id,
            code = r.Code,
            name = r.Name,
            description = r.Description
        }));
    }
    
    /// <summary>
    /// Helper method to convert Permission enum to a dictionary of booleans for UI.
    /// </summary>
    private static Dictionary<string, bool> GetPermissionObject(Permission permissions)
    {
        return new Dictionary<string, bool>
        {
            { "view", permissions.HasPermission(Permission.View) },
            { "create", permissions.HasPermission(Permission.Create) },
            { "edit", permissions.HasPermission(Permission.Edit) },
            { "delete", permissions.HasPermission(Permission.Delete) },
            { "export", permissions.HasPermission(Permission.Export) },
            { "approve", permissions.HasPermission(Permission.Approve) },
            { "execute", permissions.HasPermission(Permission.Execute) },
            { "audit", permissions.HasPermission(Permission.Audit) },
            { "manageUsers", permissions.HasPermission(Permission.ManageUsers) },
            { "viewReports", permissions.HasPermission(Permission.ViewReports) },
            { "editReports", permissions.HasPermission(Permission.EditReports) },
            { "scheduleReports", permissions.HasPermission(Permission.ScheduleReports) },
            { "manageRoles", permissions.HasPermission(Permission.ManageRoles) },
            { "managePermissions", permissions.HasPermission(Permission.ManagePermissions) }
        };
    }
    
    /// <summary>
    /// Helper method to get human-readable descriptions for permissions.
    /// </summary>
    private static string GetPermissionDescription(Permission permission)
    {
        return permission switch
        {
            Permission.View => "View/Read access",
            Permission.Create => "Create new items",
            Permission.Edit => "Modify existing items",
            Permission.Delete => "Delete items",
            Permission.Export => "Export data",
            Permission.Approve => "Approve items for publishing",
            Permission.Execute => "Execute reports/queries",
            Permission.Audit => "Access audit logs",
            Permission.ManageUsers => "Create and manage users",
            Permission.ViewReports => "View reports module",
            Permission.EditReports => "Create/modify reports",
            Permission.ScheduleReports => "Schedule report execution",
            Permission.ManageRoles => "Create/manage roles",
            Permission.ManagePermissions => "Assign/modify permissions",
            _ => "Unknown permission"
        };
    }

    #region Role Management Endpoints

    /// <summary>
    /// Assign a role to a user.
    /// Requires ManageRoles permission.
    /// </summary>
    [HttpPost("users/{userId}/roles")]
    [RequirePermission(PermissionModule.Settings, (long)Permission.ManageRoles)]
    [ProducesResponseType(typeof(RoleAssignmentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AssignRole(
        Guid userId,
        [FromBody] AssignRoleRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _roleManagementService.AssignRoleToUserAsync(
                userId,
                request.RoleId,
                cancellationToken);

            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Revoke a role from a user.
    /// Requires ManageRoles permission.
    /// </summary>
    [HttpDelete("users/{userId}/roles/{roleId}")]
    [RequirePermission(PermissionModule.Settings, (long)Permission.ManageRoles)]
    [ProducesResponseType(typeof(RoleAssignmentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RevokeRole(
        Guid userId,
        Guid roleId,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _roleManagementService.RevokeRoleFromUserAsync(
                userId,
                roleId,
                cancellationToken);

            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Update permissions for a role in a specific subsystem.
    /// Requires ManagePermissions permission.
    /// </summary>
    [HttpPut("roles/{roleId}/subsystems/{subsystemId}/permissions")]
    [RequirePermission(PermissionModule.Settings, (long)Permission.ManagePermissions)]
    [ProducesResponseType(typeof(PermissionUpdateResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateRolePermissions(
        Guid roleId,
        Guid subsystemId,
        [FromBody] UpdateRolePermissionsRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            // If permission names provided, convert to flags
            var flags = request.Flags;
            if (request.PermissionNames != null && request.PermissionNames.Any())
            {
                flags = ConvertPermissionNamesToFlags(request.PermissionNames);
            }

            var result = await _roleManagementService.UpdateRolePermissionsAsync(
                roleId,
                subsystemId,
                flags,
                cancellationToken);

            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Override permissions for a specific user in a subsystem.
    /// Overrides take precedence over role-based permissions.
    /// Requires ManagePermissions permission.
    /// </summary>
    [HttpPost("users/{userId}/subsystems/{subsystemId}/permissions/override")]
    [RequirePermission(PermissionModule.Settings, (long)Permission.ManagePermissions)]
    [ProducesResponseType(typeof(PermissionUpdateResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> OverrideUserPermissions(
        Guid userId,
        Guid subsystemId,
        [FromBody] OverrideUserPermissionsRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            // If permission names provided, convert to flags
            var flags = request.Flags;
            if (request.PermissionNames != null && request.PermissionNames.Any())
            {
                flags = ConvertPermissionNamesToFlags(request.PermissionNames);
            }

            var result = await _roleManagementService.OverrideUserPermissionsAsync(
                userId,
                subsystemId,
                flags,
                request.Reason,
                cancellationToken);

            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Remove a user permission override.
    /// Requires ManagePermissions permission.
    /// </summary>
    [HttpDelete("users/{userId}/subsystems/{subsystemId}/permissions/override")]
    [RequirePermission(PermissionModule.Settings, (long)Permission.ManagePermissions)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveUserPermissionOverride(
        Guid userId,
        Guid subsystemId,
        CancellationToken cancellationToken)
    {
        try
        {
            await _roleManagementService.RemoveUserPermissionOverrideAsync(
                userId,
                subsystemId,
                cancellationToken);

            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Get all users assigned to a specific role.
    /// Requires ManageRoles permission.
    /// </summary>
    [HttpGet("roles/{roleId}/users")]
    [RequirePermission(PermissionModule.Settings, (long)Permission.ManageRoles)]
    [ProducesResponseType(typeof(List<Guid>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetUsersWithRole(
        Guid roleId,
        CancellationToken cancellationToken)
    {
        try
        {
            var userIds = await _roleManagementService.GetUsersWithRoleAsync(roleId, cancellationToken);
            return Ok(userIds);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Convert permission names to bitwise flags.
    /// </summary>
    private static long ConvertPermissionNamesToFlags(List<string> permissionNames)
    {
        long flags = 0;

        foreach (var name in permissionNames)
        {
            if (Enum.TryParse<Permission>(name, ignoreCase: true, out var permission))
            {
                flags |= (long)permission;
            }
        }

        return flags;
    }

    #endregion
}
