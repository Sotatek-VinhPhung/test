using CleanArchitecture.Domain.Enums;

namespace CleanArchitecture.Domain.Entities;

/// <summary>
/// Default permission flags for a Role on a specific module.
/// Composite key: (Role, Module).
/// </summary>
public class RolePermission
{
    public Role Role { get; set; }
    public string Module { get; set; } = string.Empty;
    public long Flags { get; set; }
}
