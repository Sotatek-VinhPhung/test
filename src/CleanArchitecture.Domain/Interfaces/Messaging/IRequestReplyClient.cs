namespace CleanArchitecture.Domain.Interfaces.Messaging;

/// <summary>
/// Synchronous request/reply over Kafka. Sends a request to a topic
/// and awaits a correlated reply on a reply topic.
/// </summary>
public interface IRequestReplyClient
{
    /// <summary>
    /// Send a request and await a typed reply.
    /// </summary>
    /// <typeparam name="TRequest">Request payload type.</typeparam>
    /// <typeparam name="TReply">Expected reply payload type.</typeparam>
    /// <param name="topic">Topic to send the request to.</param>
    /// <param name="request">The request payload.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The deserialized reply.</returns>
    Task<TReply> SendAsync<TRequest, TReply>(
        string topic,
        TRequest request,
        CancellationToken cancellationToken = default)
        where TRequest : class
        where TReply : class;
}
