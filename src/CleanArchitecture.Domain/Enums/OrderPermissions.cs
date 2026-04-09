namespace CleanArchitecture.Domain.Enums;

[Flags]
public enum OrderPermissions : long
{
    None    = 0,
    Create  = 1 << 0,  // 1
    Read    = 1 << 1,  // 2
    Update  = 1 << 2,  // 4
    Delete  = 1 << 3,  // 8
    Approve = 1 << 4,  // 16
    Cancel  = 1 << 5,  // 32
    All     = Create | Read | Update | Delete | Approve | Cancel
}
