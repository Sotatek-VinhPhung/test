namespace CleanArchitecture.Domain.Interfaces.Messaging;

/// <summary>
/// Publishes strongly-typed messages to Kafka topics.
/// </summary>
public interface IKafkaPublisher
{
    /// <summary>Publish a single message to the specified topic.</summary>
    Task PublishAsync<T>(KafkaMessage<T> message, CancellationToken cancellationToken = default)
        where T : class;

    /// <summary>Publish a batch of messages to the specified topic.</summary>
    Task PublishBatchAsync<T>(IEnumerable<KafkaMessage<T>> messages, CancellationToken cancellationToken = default)
        where T : class;
}
