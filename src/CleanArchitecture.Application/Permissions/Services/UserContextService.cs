using CleanArchitecture.Domain.Entities;
using CleanArchitecture.Domain.Enums;
using CleanArchitecture.Domain.ValueObjects;
using CleanArchitecture.Application.Permissions.Interfaces;

namespace CleanArchitecture.Application.Permissions.Services;

/// <summary>
/// Abstract base service for loading and managing user context with merged permissions.
/// Implemented by infrastructure layer to provide database access.
/// </summary>
public abstract class UserContextServiceBase : IUserContextService
{
    /// <summary>
    /// Load UserContext for a user by loading all roles and merging their permissions.
    /// </summary>
    public abstract Task<UserContext?> GetUserContextAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reload user context (bypass any cache).
    /// </summary>
    public virtual async Task<UserContext?> ReloadUserContextAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        // In a cached implementation, clear cache here before calling GetUserContextAsync
        return await GetUserContextAsync(userId, cancellationToken);
    }

    /// <summary>
    /// Check if user has a specific permission.
    /// </summary>
    public virtual async Task<bool> HasPermissionAsync(
        Guid userId,
        string subsystemCode,
        Permission permission,
        CancellationToken cancellationToken = default)
    {
        var context = await GetUserContextAsync(userId, cancellationToken);
        if (context == null)
            return false;

        return context.HasPermission(subsystemCode, permission);
    }

    /// <summary>
    /// Check if user has all specified permissions.
    /// </summary>
    public virtual async Task<bool> HasAllPermissionsAsync(
        Guid userId,
        string subsystemCode,
        Permission[] permissions,
        CancellationToken cancellationToken = default)
    {
        var context = await GetUserContextAsync(userId, cancellationToken);
        if (context == null)
            return false;

        return context.HasAllPermissions(subsystemCode, permissions);
    }

    /// <summary>
    /// Get all subsystem permissions for a user.
    /// </summary>
    public virtual async Task<Permission> GetSubsystemPermissionsAsync(
        Guid userId,
        string subsystemCode,
        CancellationToken cancellationToken = default)
    {
        var context = await GetUserContextAsync(userId, cancellationToken);
        if (context == null)
            return Permission.None;

        var flags = context.GetSubsystemFlags(subsystemCode);
        return (Permission)flags;
    }

    /// <summary>
    /// Invalidate the cached user context.
    /// Default implementation does nothing (no caching).
    /// Override in cached implementations to clear cache.
    /// </summary>
    public virtual Task InvalidateUserContextAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        // Default implementation: no-op
        // Cached implementations should override this to clear cache
        return Task.CompletedTask;
    }
}
