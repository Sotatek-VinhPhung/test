using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using CleanArchitecture.Domain.Interfaces.Messaging;
using Confluent.Kafka;
using Microsoft.Extensions.Logging;

namespace CleanArchitecture.Infrastructure.Messaging;

/// <summary>
/// Publishes strongly-typed messages to Kafka topics using System.Text.Json serialization.
/// </summary>
public class KafkaPublisher : IKafkaPublisher
{
    private readonly KafkaClient _client;
    private readonly ILogger<KafkaPublisher> _logger;

    public KafkaPublisher(KafkaClient client, ILogger<KafkaPublisher> logger)
    {
        _client = client;
        _logger = logger;
    }

    public async Task PublishAsync<T>(KafkaMessage<T> message, CancellationToken cancellationToken = default)
        where T : class
    {
        var producer = _client.GetProducer();
        var kafkaMessage = BuildKafkaMessage(message);

        try
        {
            var result = await producer.ProduceAsync(message.Topic, kafkaMessage, cancellationToken);
            _logger.LogInformation(
                "Published to {Topic} [partition:{Partition}, offset:{Offset}]",
                message.Topic, result.Partition.Value, result.Offset.Value);
        }
        catch (ProduceException<string, string> ex)
        {
            _logger.LogError(ex, "Failed to publish to {Topic}: {Error}", message.Topic, ex.Error.Reason);
            throw;
        }
    }

    public async Task PublishBatchAsync<T>(
        IEnumerable<KafkaMessage<T>> messages,
        CancellationToken cancellationToken = default) where T : class
    {
        var producer = _client.GetProducer();
        var count = 0;
        var failures = new ConcurrentBag<Exception>();

        foreach (var message in messages)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var kafkaMessage = BuildKafkaMessage(message);

            producer.Produce(message.Topic, kafkaMessage, report =>
            {
                if (report.Error.IsError)
                {
                    _logger.LogError("Batch delivery failed for {Topic}: {Error}",
                        message.Topic, report.Error.Reason);
                    failures.Add(new ProduceException<string, string>(
                        report.Error, report));
                }
            });

            count++;
        }

        producer.Flush(cancellationToken);

        if (!failures.IsEmpty)
        {
            throw new AggregateException(
                $"Batch publish failed for {failures.Count}/{count} messages", failures);
        }

        _logger.LogInformation("Batch published {Count} messages", count);
    }

    private static Message<string, string> BuildKafkaMessage<T>(KafkaMessage<T> message) where T : class
    {
        var kafkaMessage = new Message<string, string>
        {
            Key = message.Key ?? string.Empty,
            Value = JsonSerializer.Serialize(message.Value),
            Timestamp = new Timestamp(message.Timestamp)
        };

        if (message.Headers is not { Count: > 0 }) return kafkaMessage;

        kafkaMessage.Headers = new Headers();
        foreach (var (key, value) in message.Headers)
        {
            kafkaMessage.Headers.Add(key, Encoding.UTF8.GetBytes(value));
        }

        return kafkaMessage;
    }
}
