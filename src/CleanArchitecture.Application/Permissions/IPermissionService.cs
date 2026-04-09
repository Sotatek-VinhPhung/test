using CleanArchitecture.Domain.Enums;

namespace CleanArchitecture.Application.Permissions;

/// <summary>
/// Service for checking and resolving permissions.
/// Use this for service-level permission checks (hits DB for real-time data).
/// </summary>
public interface IPermissionService
{
    /// <summary>
    /// Check if a user has ALL the required flags on a module.
    /// </summary>
    Task<bool> HasPermissionAsync(
        Guid userId, Role role, string module, long requiredFlags,
        CancellationToken ct = default);

    /// <summary>
    /// Get the effective (Role | Override) flags for all modules for a user.
    /// Returns a dictionary: module name -> effective flags.
    /// Used when building JWT claims.
    /// </summary>
    Task<Dictionary<string, long>> GetAllEffectiveAsync(
        Guid userId, Role role, CancellationToken ct = default);

    /// <summary>
    /// Throws ForbiddenException if the user lacks required flags.
    /// </summary>
    Task RequirePermissionAsync(
        Guid userId, Role role, string module, long requiredFlags,
        CancellationToken ct = default);
}
