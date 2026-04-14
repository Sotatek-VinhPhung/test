using CleanArchitecture.Domain.Enums;
using CleanArchitecture.Domain.ValueObjects;

namespace CleanArchitecture.Application.Permissions.Interfaces;

/// <summary>
/// Interface for loading and managing user context with merged permissions.
/// </summary>
public interface IUserContextService
{
    /// <summary>
    /// Load or create a UserContext for the specified user.
    /// Loads user's roles and merges permissions across all subsystems.
    /// </summary>
    /// <param name="userId">The user's ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>UserContext with merged permissions, or null if user not found</returns>
    Task<UserContext?> GetUserContextAsync(Guid userId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Reload the user context (clear cache and reload from DB).
    /// Useful after permission changes.
    /// </summary>
    Task<UserContext?> ReloadUserContextAsync(Guid userId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Check if a user has a specific permission in a subsystem.
    /// </summary>
    Task<bool> HasPermissionAsync(
        Guid userId,
        string subsystemCode,
        Permission permission,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Check if a user has all specified permissions in a subsystem.
    /// </summary>
    Task<bool> HasAllPermissionsAsync(
        Guid userId,
        string subsystemCode,
        Permission[] permissions,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get all permissions for a user in a specific subsystem.
    /// </summary>
    Task<Permission> GetSubsystemPermissionsAsync(
        Guid userId,
        string subsystemCode,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Invalidate the cached user context.
    /// Call after modifying user roles or permissions.
    /// </summary>
    Task InvalidateUserContextAsync(Guid userId, CancellationToken cancellationToken = default);
}
