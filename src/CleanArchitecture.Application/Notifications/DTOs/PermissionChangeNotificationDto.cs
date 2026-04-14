namespace CleanArchitecture.Application.Notifications.DTOs;

/// <summary>
/// Base DTO for permission change notifications sent via SignalR
/// </summary>
public class PermissionChangeNotificationDto
{
    public Guid UserId { get; set; }
    public required string ChangeType { get; set; } // "RoleAssigned", "RoleRevoked", "PermissionsUpdated"
    public DateTime ChangedAt { get; set; }
    public string? Details { get; set; }
}

/// <summary>
/// Notification for role assignment
/// </summary>
public class RoleAssignedNotificationDto : PermissionChangeNotificationDto
{
    public Guid RoleId { get; set; }
    public required string RoleName { get; set; }
}

/// <summary>
/// Notification for role revocation
/// </summary>
public class RoleRevokedNotificationDto : PermissionChangeNotificationDto
{
    public Guid RoleId { get; set; }
    public required string RoleName { get; set; }
}

/// <summary>
/// Notification for permission updates on a role
/// </summary>
public class PermissionsUpdatedNotificationDto : PermissionChangeNotificationDto
{
    public Guid RoleId { get; set; }
    public required string RoleName { get; set; }
    public required string SubsystemCode { get; set; }
    public long Permissions { get; set; }
    public List<string>? PermissionNames { get; set; }
}

/// <summary>
/// Notification for user-specific permission override
/// </summary>
public class UserPermissionOverrideNotificationDto : PermissionChangeNotificationDto
{
    public required string SubsystemCode { get; set; }
    public long Permissions { get; set; }
    public bool IsRemoved { get; set; } // true if override was removed
    public List<string>? PermissionNames { get; set; }
}
