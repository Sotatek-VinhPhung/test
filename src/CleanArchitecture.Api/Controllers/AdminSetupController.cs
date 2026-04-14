using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using CleanArchitecture.Application.Permissions.Interfaces;
using CleanArchitecture.Domain.Entities;
using CleanArchitecture.Domain.Enums;
using CleanArchitecture.Infrastructure.Persistence;
using CleanArchitecture.Infrastructure.Persistence.Repositories;

namespace CleanArchitecture.Api.Controllers;

/// <summary>
/// Admin endpoint to setup roles with permissions in one go.
/// Example: Setup "Kế toán trưởng" role with full "Báo cáo kế toán" subsystem access.
/// </summary>
[ApiController]
[Route("api/admin")]
[Authorize]
public class AdminSetupController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IRoleManagementService _roleManagementService;
    private readonly RoleRepository _roleRepository;
    private readonly SubsystemRepository _subsystemRepository;

    public AdminSetupController(
        AppDbContext context,
        IRoleManagementService roleManagementService,
        RoleRepository roleRepository,
        SubsystemRepository subsystemRepository)
    {
        _context = context;
        _roleManagementService = roleManagementService;
        _roleRepository = roleRepository;
        _subsystemRepository = subsystemRepository;
    }

    /// <summary>
    /// Create a role and assign full permissions to specific subsystems.
    /// Example request:
    /// {
    ///   "roleCode": "ChiefAccountant",
    ///   "roleName": "Kế toán trưởng",
    ///   "description": "Chief Accountant with full reporting access",
    ///   "subsystemCodes": ["ReportsAccounting", "ReportsFinance"],
    ///   "permissions": ["View", "Create", "Edit", "Delete", "Export", "Approve"]
    /// }
    /// </summary>
    [HttpPost("setup-role")]
    [RequirePermission("Settings", (long)Permission.ManageRoles)]
    public async Task<IActionResult> SetupRole(
        [FromBody] SetupRoleRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            // 1. Check if role already exists
            var existingRole = await _roleRepository.GetByCodeAsync(request.RoleCode, cancellationToken);
            Guid roleId;

            if (existingRole != null)
            {
                roleId = existingRole.Id;
            }
            else
            {
                // 2. Create new role
                var newRole = new Domain.Entities.Role
                {
                    Id = Guid.NewGuid(),
                    Code = request.RoleCode,
                    Name = request.RoleName,
                    Description = request.Description,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Roles.Add(newRole);
                await _context.SaveChangesAsync(cancellationToken);
                roleId = newRole.Id;
            }

            // 3. Get subsystems and assign permissions
            var subsystemIds = await _context.Subsystems
                .Where(s => request.SubsystemCodes.Contains(s.Code))
                .Select(s => s.Id)
                .ToListAsync(cancellationToken);

            if (!subsystemIds.Any())
                return BadRequest("No subsystems found with provided codes");

            // 4. Convert permission names to flags
            var flags = ConvertPermissionsToFlags(request.Permissions);

            // 5. Assign permissions for each subsystem
            foreach (var subsystemId in subsystemIds)
            {
                await _roleManagementService.UpdateRolePermissionsAsync(
                    roleId,
                    subsystemId,
                    flags,
                    cancellationToken);
            }

            return Ok(new
            {
                message = $"Role '{request.RoleName}' setup successfully",
                roleId = roleId,
                subsystemsAssigned = request.SubsystemCodes.Count,
                permissionsAssigned = request.Permissions
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Quick assign role to user.
    /// Example: Assign "Kế toán trưởng" role to "Nguyễn Văn A"
    /// </summary>
    [HttpPost("users/{userId}/assign-role")]
    [RequirePermission("Settings", (long)Permission.ManageRoles)]
    public async Task<IActionResult> AssignRoleToUser(
        Guid userId,
        [FromBody] QuickAssignRoleRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            // Get role by code
            var role = await _roleRepository.GetByCodeAsync(request.RoleCode, cancellationToken);
            if (role == null)
                return NotFound($"Role '{request.RoleCode}' not found");

            // Assign role
            var result = await _roleManagementService.AssignRoleToUserAsync(
                userId,
                role.Id,
                cancellationToken);

            return Ok(new
            {
                message = $"Role '{request.RoleCode}' assigned to user",
                userId = userId,
                roleId = role.Id,
                roleName = role.Name
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Get all roles with their subsystem permissions.
    /// Useful for UI to show role structure.
    /// </summary>
    [HttpGet("roles/with-permissions")]
    public async Task<IActionResult> GetRolesWithPermissions(CancellationToken cancellationToken)
    {
        var roles = await _context.Roles
            .Where(r => r.IsActive)
            .Include(r => r.RoleSubsystemPermissions)
            .ThenInclude(rsp => rsp.Subsystem)
            .ToListAsync(cancellationToken);

        var result = roles.Select(r => new
        {
            id = r.Id,
            code = r.Code,
            name = r.Name,
            description = r.Description,
            subsystems = r.RoleSubsystemPermissions
                .Where(rsp => rsp.Subsystem.IsActive)
                .Select(rsp => new
                {
                    subsystemId = rsp.Subsystem.Id,
                    subsystemCode = rsp.Subsystem.Code,
                    subsystemName = rsp.Subsystem.Name,
                    flags = rsp.Flags,
                    permissions = GetPermissionNames((Permission)rsp.Flags)
                })
                .ToList()
        });

        return Ok(result);
    }

    /// <summary>
    /// Test endpoint - Check what permissions a user would have.
    /// </summary>
    [HttpGet("users/{userId}/effective-permissions")]
    public async Task<IActionResult> GetUserEffectivePermissions(
        Guid userId,
        CancellationToken cancellationToken)
    {
        var user = await _context.Users
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .ThenInclude(r => r.RoleSubsystemPermissions)
            .ThenInclude(rsp => rsp.Subsystem)
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user == null)
            return NotFound("User not found");

        var permissions = user.UserRoles
            .Where(ur => ur.IsActive())
            .SelectMany(ur => ur.Role.RoleSubsystemPermissions)
            .Where(rsp => rsp.Subsystem.IsActive)
            .GroupBy(rsp => rsp.Subsystem.Code)
            .Select(g => new
            {
                subsystem = g.Key,
                combinedFlags = g.Aggregate(0L, (acc, rsp) => acc | rsp.Flags),
                permissions = GetPermissionNames((Permission)g.Aggregate(0L, (acc, rsp) => acc | rsp.Flags)),
                roles = g.Select(rsp => new
                {
                    roleCode = rsp.Role.Code,
                    roleName = rsp.Role.Name,
                    flags = rsp.Flags
                }).DistinctBy(r => r.roleCode)
            })
            .ToList();

        return Ok(new
        {
            userId = userId,
            userName = user.Name,
            email = user.Email,
            userRoles = user.UserRoles
                .Where(ur => ur.IsActive())
                .Select(ur => new { code = ur.Role.Code, name = ur.Role.Name })
                .ToList(),
            permissions = permissions
        });
    }

    // Helper methods

    private static long ConvertPermissionsToFlags(List<string> permissionNames)
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

    private static List<string> GetPermissionNames(Permission permissions)
    {
        return Enum.GetValues(typeof(Permission))
            .Cast<Permission>()
            .Where(p => p != Permission.None && permissions.HasPermission(p))
            .Select(p => p.ToString())
            .ToList();
    }
}

/// <summary>
/// Request to setup a role with permissions.
/// </summary>
public class SetupRoleRequest
{
    public string RoleCode { get; set; } = "";
    public string RoleName { get; set; } = "";
    public string Description { get; set; } = "";
    public List<string> SubsystemCodes { get; set; } = new();
    public List<string> Permissions { get; set; } = new();
}

/// <summary>
/// Quick request to assign existing role to user by role code.
/// </summary>
public class QuickAssignRoleRequest
{
    public string RoleCode { get; set; } = "";
}
