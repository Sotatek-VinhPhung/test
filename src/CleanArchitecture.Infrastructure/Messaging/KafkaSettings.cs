namespace CleanArchitecture.Infrastructure.Messaging;

/// <summary>
/// Kafka configuration POCO — bound from appsettings.json "KafkaSettings" section.
/// </summary>
public class KafkaSettings
{
    public const string SectionName = "KafkaSettings";

    /// <summary>Comma-separated broker addresses.</summary>
    public string BootstrapServers { get; set; } = "localhost:9092";

    /// <summary>Consumer group ID for this application.</summary>
    public string GroupId { get; set; } = "clean-architecture-group";

    /// <summary>Enable auto-commit for consumer offsets.</summary>
    public bool EnableAutoCommit { get; set; } = false;

    /// <summary>Auto offset reset policy: earliest | latest.</summary>
    public string AutoOffsetReset { get; set; } = "earliest";

    /// <summary>Request timeout in milliseconds.</summary>
    public int RequestTimeoutMs { get; set; } = 30000;

    /// <summary>Max retry attempts before sending to DLQ.</summary>
    public int MaxRetryAttempts { get; set; } = 3;

    /// <summary>Delay between retries in milliseconds.</summary>
    public int RetryDelayMs { get; set; } = 1000;

    /// <summary>Suffix appended to topic name for DLQ.</summary>
    public string DlqTopicSuffix { get; set; } = ".dlq";

    /// <summary>Reply topic for request/reply pattern.</summary>
    public string ReplyTopic { get; set; } = "reply-topic";

    /// <summary>Timeout for request/reply in milliseconds.</summary>
    public int RequestReplyTimeoutMs { get; set; } = 10000;
}
