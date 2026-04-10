namespace CleanArchitecture.Domain.Interfaces.Messaging;

/// <summary>
/// Handles messages of type <typeparamref name="T"/> consumed from a Kafka topic.
/// Implement one handler per topic/message type and register via DI.
/// </summary>
public interface IMessageHandler<in T> where T : class
{
    /// <summary>The topic this handler subscribes to.</summary>
    string Topic { get; }

    /// <summary>Process a single message.</summary>
    Task HandleAsync(T message, CancellationToken cancellationToken = default);
}
