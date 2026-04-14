namespace CleanArchitecture.Application.Auth.Events;

/// <summary>
/// Event published to Kafka when a user successfully logs in.
/// </summary>
public record UserLoginEvent
{
    /// <summary>Unique identifier of the user who logged in.</summary>
    public required Guid UserId { get; init; }

    /// <summary>Email address of the user.</summary>
    public required string Email { get; init; }

    /// <summary>User's role.</summary>
    public required string Role { get; init; }

    /// <summary>Timestamp of the login event.</summary>
    public DateTime LoginAt { get; init; } = DateTime.UtcNow;

    /// <summary>IP address or hostname from which the user logged in (optional).</summary>
    public string? SourceIp { get; init; }
}
