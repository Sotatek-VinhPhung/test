using CleanArchitecture.Domain.Common;
using CleanArchitecture.Domain.Enums;

namespace CleanArchitecture.Domain.Entities;

/// <summary>
/// User domain entity with authentication properties.
/// Supports many-to-many relationship with roles for RBAC.
/// </summary>
public class User : BaseEntity
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;

    /// <summary>
    /// Legacy single-role support. Consider using UserRoles for RBAC.
    /// </summary>
    public Enums.Role Role { get; set; } = Enums.Role.User;

    public string? RefreshToken { get; set; }
    public DateTime? RefreshTokenExpiry { get; set; }

    // Navigation properties

    /// <summary>
    /// Roles assigned to this user via the UserRole junction table.
    /// </summary>
    public ICollection<UserRole> UserRoles { get; set; } = [];
}
