using CleanArchitecture.Domain.Enums;

namespace CleanArchitecture.Domain.ValueObjects;

/// <summary>
/// Immutable aggregate of user permission information.
/// Represents the complete permission context for a user across all subsystems.
/// Merges permissions from all user roles using bitwise OR operations.
/// This is designed to be cached and invalidated when user roles/permissions change.
/// </summary>
public class UserContext
{
    /// <summary>
    /// The user's unique identifier.
    /// </summary>
    public Guid UserId { get; private set; }
    
    /// <summary>
    /// User's email address.
    /// </summary>
    public string Email { get; private set; }
    
    /// <summary>
    /// List of role IDs this user has been assigned.
    /// </summary>
    public IReadOnlyList<Guid> RoleIds { get; private set; }
    
    /// <summary>
    /// Merged permissions per subsystem.
    /// Key: Subsystem code (e.g., "Reports", "Users")
    /// Value: Combined permission flags from all user roles on that subsystem
    /// </summary>
    public IReadOnlyDictionary<string, long> SubsystemPermissions { get; private set; }
    
    /// <summary>
    /// Optional list of region IDs for multi-region access control.
    /// </summary>
    public IReadOnlyList<Guid>? RegionIds { get; private set; }
    
    /// <summary>
    /// Optional list of department IDs for departmental access control.
    /// </summary>
    public IReadOnlyList<Guid>? DepartmentIds { get; private set; }
    
    /// <summary>
    /// Timestamp when this context was created/loaded.
    /// Used for cache expiration and staleness detection.
    /// </summary>
    public DateTime CreatedAt { get; private set; }
    
    /// <summary>
    /// Private constructor for immutability.
    /// </summary>
    private UserContext(
        Guid userId,
        string email,
        IReadOnlyList<Guid> roleIds,
        IReadOnlyDictionary<string, long> subsystemPermissions,
        IReadOnlyList<Guid>? regionIds = null,
        IReadOnlyList<Guid>? departmentIds = null,
        DateTime? createdAt = null)
    {
        UserId = userId;
        Email = email;
        RoleIds = roleIds;
        SubsystemPermissions = subsystemPermissions;
        RegionIds = regionIds;
        DepartmentIds = departmentIds;
        CreatedAt = createdAt ?? DateTime.UtcNow;
    }
    
    /// <summary>
    /// Factory method to create a new UserContext.
    /// </summary>
    public static UserContext Create(
        Guid userId,
        string email,
        IEnumerable<Guid> roleIds,
        Dictionary<string, long> subsystemPermissions,
        IEnumerable<Guid>? regionIds = null,
        IEnumerable<Guid>? departmentIds = null)
    {
        return new UserContext(
            userId,
            email,
            roleIds.ToList().AsReadOnly(),
            subsystemPermissions.AsReadOnly(),
            regionIds?.ToList().AsReadOnly(),
            departmentIds?.ToList().AsReadOnly());
    }
    
    /// <summary>
    /// Check if user has a specific permission in a subsystem.
    /// </summary>
    /// <param name="subsystemCode">Subsystem code (e.g., "Reports")</param>
    /// <param name="permission">Permission flag to check</param>
    /// <returns>True if user has the permission, false otherwise</returns>
    public bool HasPermission(string subsystemCode, Permission permission)
    {
        if (!SubsystemPermissions.TryGetValue(subsystemCode, out var flags))
            return false;
        
        return ((Permission)flags).HasPermission(permission);
    }

    /// <summary>
    /// Check if user has all specified permissions in a subsystem.
    /// </summary>
    public bool HasAllPermissions(string subsystemCode, params Permission[] permissions)
    {
        if (!SubsystemPermissions.TryGetValue(subsystemCode, out var flags))
            return false;

        var combined = PermissionExtensions.Merge(permissions);
        return ((Permission)flags).HasPermission(combined);
    }
    
    /// <summary>
    /// Get permission flags for a specific subsystem.
    /// Returns 0 if user has no permissions on that subsystem.
    /// </summary>
    public long GetSubsystemFlags(string subsystemCode)
    {
        return SubsystemPermissions.TryGetValue(subsystemCode, out var flags) ? flags : 0;
    }
    
    /// <summary>
    /// Get all subsystem codes the user has access to.
    /// </summary>
    public IEnumerable<string> GetAccessibleSubsystems()
    {
        return SubsystemPermissions.Keys;
    }
    
    /// <summary>
    /// Check if this context is still valid based on maximum age.
    /// </summary>
    public bool IsStale(TimeSpan maxAge)
    {
        return DateTime.UtcNow - CreatedAt > maxAge;
    }
    
    /// <summary>
    /// Check if user is in a specific region (if regions are supported).
    /// </summary>
    public bool IsInRegion(Guid regionId)
    {
        return RegionIds?.Contains(regionId) ?? false;
    }
    
    /// <summary>
    /// Check if user is in a specific department (if departments are supported).
    /// </summary>
    public bool IsInDepartment(Guid departmentId)
    {
        return DepartmentIds?.Contains(departmentId) ?? false;
    }
}
