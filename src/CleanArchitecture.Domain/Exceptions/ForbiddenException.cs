namespace CleanArchitecture.Domain.Exceptions;

/// <summary>
/// Thrown when a user lacks required permissions. Maps to HTTP 403.
/// </summary>
public class ForbiddenException : Exception
{
    public ForbiddenException(string message) : base(message) { }

    public ForbiddenException(string module, long requiredFlags)
        : base($"Insufficient permissions on '{module}'. Required flags: {requiredFlags}.") { }
}
