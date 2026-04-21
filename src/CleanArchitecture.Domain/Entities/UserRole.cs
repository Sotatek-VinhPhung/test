namespace CleanArchitecture.Domain.Entities;

/// <summary>
/// Junction table for many-to-many relationship between User and Role.
/// Enables users to have multiple roles and roles to be assigned to multiple users.
/// </summary>
public class UserRole
{
    /// <summary>
    /// The user assigned to this role.
    /// </summary>
    public Guid UserId { get; set; }
    
    /// <summary>
    /// The role assigned to the user.
    /// </summary>
    public Guid RoleId { get; set; }
    
    /// <summary>
    /// Timestamp when the role was assigned to the user.
    /// </summary>
    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Optional timestamp for when this role assignment expires.
    /// Null means the assignment doesn't expire.
    /// </summary>
    //public DateTime? ExpiresAt { get; set; }
    
    // Navigation properties
    public User User { get; set; } = null!;
    public Role Role { get; set; } = null!;

    ///// <summary>
    ///// Check if this role assignment is currently active.
    ///// </summary>
    public bool IsActive()
    {
        //if (ExpiresAt.HasValue)
        //    return DateTime.UtcNow <= ExpiresAt.Value;

        return true;
    }
}
