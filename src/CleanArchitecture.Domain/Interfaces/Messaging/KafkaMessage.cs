namespace CleanArchitecture.Domain.Interfaces.Messaging;

/// <summary>
/// Envelope for Kafka messages. Carries key, value, topic, and optional headers.
/// </summary>
public record KafkaMessage<T>
{
    /// <summary>Message key for partitioning. Null uses round-robin.</summary>
    public string? Key { get; init; }

    /// <summary>The strongly-typed message payload.</summary>
    public required T Value { get; init; }

    /// <summary>Target topic name.</summary>
    public required string Topic { get; init; }

    /// <summary>Optional headers (correlation IDs, metadata).</summary>
    public IDictionary<string, string>? Headers { get; init; }

    /// <summary>Message timestamp. Defaults to UtcNow if not set.</summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}
