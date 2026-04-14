namespace CleanArchitecture.Domain.Enums;

/// <summary>
/// Bitwise permission flags for subsystem operations.
/// Each permission is represented as a power of 2 for bitwise operations.
/// Maximum 64 permissions per BIGINT (though typically 6-10 are used per subsystem).
/// </summary>
[Flags]
public enum Permission : long
{
    // Generic permissions (common across subsystems)
    None = 0,
    View = 1 << 0,      // 1
    Create = 1 << 1,    // 2
    Edit = 1 << 2,      // 4
    Delete = 1 << 3,    // 8
    Export = 1 << 4,    // 16
    Approve = 1 << 5,   // 32
    
    // Specialized permissions
    Execute = 1 << 6,   // 64
    Audit = 1 << 7,     // 128
    ManageUsers = 1 << 8, // 256
    ViewReports = 1 << 9, // 512
    EditReports = 1 << 10, // 1024
    ScheduleReports = 1 << 11, // 2048
    
    // Admin permissions
    ManageRoles = 1 << 12, // 4096
    ManagePermissions = 1 << 13, // 8192
    
    // Add more as needed (up to bit 63 for long)
}

/// <summary>
/// Helper extension methods for Permission enum.
/// </summary>
public static class PermissionExtensions
{
    /// <summary>
    /// Check if a permission flag is set.
    /// </summary>
    public static bool HasPermission(this Permission permissions, Permission flag)
    {
        return (permissions & flag) == flag;
    }

    /// <summary>
    /// Add a permission flag.
    /// </summary>
    public static Permission AddPermission(this Permission permissions, Permission flag)
    {
        return permissions | flag;
    }

    /// <summary>
    /// Remove a permission flag.
    /// </summary>
    public static Permission RemovePermission(this Permission permissions, Permission flag)
    {
        return permissions & ~flag;
    }

    /// <summary>
    /// Merge multiple permission sets using bitwise OR.
    /// </summary>
    public static Permission Merge(params Permission[] permissions)
    {
        Permission result = Permission.None;
        foreach (var perm in permissions)
            result |= perm;
        return result;
    }

    /// <summary>
    /// Merge a collection of permission sets using bitwise OR.
    /// </summary>
    public static Permission MergeCollection(IEnumerable<Permission> permissions)
    {
        Permission result = Permission.None;
        foreach (var perm in permissions)
            result |= perm;
        return result;
    }
}
