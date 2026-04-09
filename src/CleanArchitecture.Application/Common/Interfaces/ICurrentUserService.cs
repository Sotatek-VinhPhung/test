namespace CleanArchitecture.Application.Common.Interfaces;

/// <summary>
/// Provides the current authenticated user's identity.
/// </summary>
public interface ICurrentUserService
{
    Guid? UserId { get; }
}
