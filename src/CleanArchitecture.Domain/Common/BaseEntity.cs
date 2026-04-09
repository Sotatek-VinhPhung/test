namespace CleanArchitecture.Domain.Common;

/// <summary>
/// Base entity with common audit properties.
/// All domain entities should inherit from this class.
/// </summary>
public abstract class BaseEntity
{
    public Guid Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
