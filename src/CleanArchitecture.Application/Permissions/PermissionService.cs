using CleanArchitecture.Domain.Enums;
using CleanArchitecture.Domain.Exceptions;
using CleanArchitecture.Application.Permissions.Interfaces;

namespace CleanArchitecture.Application.Permissions;

/// <summary>
/// Updated to use new RBAC system (Subsystem-based) instead of legacy module-based approach.
/// </summary>
public class PermissionService : IPermissionService
{
    private readonly IRolePermissionRepository _rolePermissionRepository;

    public PermissionService(IRolePermissionRepository rolePermissionRepository)
    {
        _rolePermissionRepository = rolePermissionRepository;
    }

    /// <summary>
    /// Check if user has required permission in a subsystem using new RBAC.
    /// </summary>
    public async Task<bool> HasPermissionAsync(
        Guid userId, CleanArchitecture.Domain.Enums.Role role, string subsystemCode, long requiredFlags,
        CancellationToken ct = default)
    {
        // Get subsystem by code
        var subsystem = await _rolePermissionRepository.GetSubsystemByCodeAsync(subsystemCode, ct);
        if (subsystem == null)
            return false;

        // Check if user has required permission in subsystem
        return await _rolePermissionRepository.HasPermissionAsync(userId, subsystem.Id, requiredFlags, ct);
    }

    /// <summary>
    /// Get all effective permissions for user in all subsystems.
    /// </summary>
    public async Task<Dictionary<string, long>> GetAllEffectiveAsync(
        Guid userId, CleanArchitecture.Domain.Enums.Role role, CancellationToken ct = default)
    {
        return await _rolePermissionRepository.GetAllEffectivePermissionsAsync(userId, ct);
    }

    /// <summary>
    /// Require user to have permission, throw if not.
    /// </summary>
    public async Task RequirePermissionAsync(
        Guid userId, Role role, string subsystemCode, long requiredFlags,
        CancellationToken ct = default)
    {
        if (!await HasPermissionAsync(userId, role, subsystemCode, requiredFlags, ct))
            throw new ForbiddenException(subsystemCode, requiredFlags);
    }
}
