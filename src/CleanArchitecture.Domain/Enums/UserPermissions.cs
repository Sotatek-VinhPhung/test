namespace CleanArchitecture.Domain.Enums;

/// <summary>
/// Bitwise permission flags for the Users module.
/// Each value must be a power of 2.
/// </summary>
[Flags]
public enum UserPermissions : long
{
    None   = 0,
    Create = 1 << 0,  // 1
    Read   = 1 << 1,  // 2
    Update = 1 << 2,  // 4
    Delete = 1 << 3,  // 8
    Export = 1 << 4,  // 16
    All    = Create | Read | Update | Delete | Export
}
