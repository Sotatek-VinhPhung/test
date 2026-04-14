using CleanArchitecture.Domain.Entities;

namespace CleanArchitecture.Application.Permissions.Interfaces;

/// <summary>
/// Repository interface for new RBAC (Subsystem-based).
/// Provides access to role permissions data without tying Application layer to Infrastructure.
/// </summary>
public interface IRolePermissionRepository
{
    /// <summary>
    /// Get subsystem by code.
    /// </summary>
    Task<Subsystem?> GetSubsystemByCodeAsync(string code, CancellationToken ct = default);

    /// <summary>
    /// Get user roles (role IDs assigned to user).
    /// </summary>
    Task<List<Guid>> GetUserRoleIdsAsync(Guid userId, CancellationToken ct = default);

    /// <summary>
    /// Check if user has required permission flags in subsystem.
    /// </summary>
    Task<bool> HasPermissionAsync(Guid userId, Guid subsystemId, long requiredFlags, CancellationToken ct = default);

    /// <summary>
    /// Get all effective permissions for user across all subsystems.
    /// Returns dictionary: subsystem code -> combined flags from all user roles.
    /// </summary>
    Task<Dictionary<string, long>> GetAllEffectivePermissionsAsync(Guid userId, CancellationToken ct = default);
}
