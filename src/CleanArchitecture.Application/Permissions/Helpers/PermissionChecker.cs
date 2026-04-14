using CleanArchitecture.Domain.Enums;
using CleanArchitecture.Domain.ValueObjects;
using CleanArchitecture.Application.Permissions.Interfaces;

namespace CleanArchitecture.Application.Permissions.Helpers;

/// <summary>
/// Helper class for permission checking operations.
/// Provides utility methods for permission validation and filtering.
/// </summary>
public class PermissionChecker
{
    private readonly IUserContextService _userContextService;
    
    public PermissionChecker(IUserContextService userContextService)
    {
        _userContextService = userContextService;
    }
    
    /// <summary>
    /// Check if a user has a specific permission in a subsystem.
    /// This is the primary method recommended for permission checks.
    /// </summary>
    public async Task<bool> HasPermissionAsync(
        Guid userId,
        string subsystemCode,
        Permission requiredPermission,
        CancellationToken cancellationToken = default)
    {
        return await _userContextService.HasPermissionAsync(
            userId,
            subsystemCode,
            requiredPermission,
            cancellationToken);
    }
    
    /// <summary>
    /// Check if a user has all specified permissions (requires ALL flags).
    /// </summary>
    public async Task<bool> HasAllPermissionsAsync(
        Guid userId,
        string subsystemCode,
        params Permission[] requiredPermissions)
    {
        if (!requiredPermissions.Any())
            return true;
        
        return await _userContextService.HasAllPermissionsAsync(
            userId,
            subsystemCode,
            requiredPermissions);
    }
    
    /// <summary>
    /// Check if a user has any of the specified permissions (requires ONE flag).
    /// </summary>
    public async Task<bool> HasAnyPermissionAsync(
        Guid userId,
        string subsystemCode,
        params Permission[] requiredPermissions)
    {
        if (!requiredPermissions.Any())
            return false;
        
        var userContext = await _userContextService.GetUserContextAsync(userId);
        if (userContext == null)
            return false;
        
        foreach (var permission in requiredPermissions)
        {
            if (userContext.HasPermission(subsystemCode, permission))
                return true;
        }
        
        return false;
    }
    
    /// <summary>
    /// Filter a collection of resources based on user permissions.
    /// </summary>
    public async Task<IEnumerable<T>> FilterByPermissionAsync<T>(
        Guid userId,
        string subsystemCode,
        Permission requiredPermission,
        IEnumerable<T> resources,
        CancellationToken cancellationToken = default)
        where T : class
    {
        var hasPermission = await HasPermissionAsync(userId, subsystemCode, requiredPermission, cancellationToken);
        
        if (hasPermission)
            return resources;
        
        return Enumerable.Empty<T>();
    }
    
    /// <summary>
    /// Get user context for direct permission checks.
    /// </summary>
    public async Task<UserContext?> GetUserContextAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _userContextService.GetUserContextAsync(userId, cancellationToken);
    }
    
    /// <summary>
    /// Create a permission context for use in static methods or without DI.
    /// </summary>
    public static class Static
    {
        /// <summary>
        /// Check if user context has a specific permission.
        /// </summary>
        public static bool HasPermission(UserContext userContext, string subsystemCode, Permission permission)
        {
            if (userContext == null)
                throw new ArgumentNullException(nameof(userContext));
            
            return userContext.HasPermission(subsystemCode, permission);
        }
        
        /// <summary>
        /// Check if user context has all specified permissions.
        /// </summary>
        public static bool HasAllPermissions(UserContext userContext, string subsystemCode, params Permission[] permissions)
        {
            if (userContext == null)
                throw new ArgumentNullException(nameof(userContext));
            
            return userContext.HasAllPermissions(subsystemCode, permissions);
        }
    }
}
