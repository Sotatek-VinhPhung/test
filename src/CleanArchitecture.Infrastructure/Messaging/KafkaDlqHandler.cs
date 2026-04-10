using System.Text;
using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CleanArchitecture.Infrastructure.Messaging;

/// <summary>
/// Routes failed messages to a dead letter queue topic with error metadata headers.
/// </summary>
public class KafkaDlqHandler
{
    private readonly KafkaClient _client;
    private readonly KafkaSettings _settings;
    private readonly ILogger<KafkaDlqHandler> _logger;

    public KafkaDlqHandler(
        KafkaClient client,
        IOptions<KafkaSettings> settings,
        ILogger<KafkaDlqHandler> logger)
    {
        _client = client;
        _settings = settings.Value;
        _logger = logger;
    }

    /// <summary>
    /// Publishes a failed message to the DLQ topic with error metadata headers.
    /// </summary>
    public async Task SendToDlqAsync(
        string originalTopic,
        ConsumeResult<string, string> consumeResult,
        Exception exception,
        CancellationToken cancellationToken = default)
    {
        var dlqTopic = $"{originalTopic}{_settings.DlqTopicSuffix}";
        var producer = _client.GetProducer();

        var headers = CloneHeaders(consumeResult.Message.Headers);
        headers.Add("dlq-error", Encoding.UTF8.GetBytes(exception.Message));
        headers.Add("dlq-original-topic", Encoding.UTF8.GetBytes(originalTopic));
        headers.Add("dlq-timestamp", Encoding.UTF8.GetBytes(DateTime.UtcNow.ToString("O")));

        var dlqMessage = new Message<string, string>
        {
            Key = consumeResult.Message.Key,
            Value = consumeResult.Message.Value,
            Headers = headers
        };

        try
        {
            var result = await producer.ProduceAsync(dlqTopic, dlqMessage, cancellationToken);
            _logger.LogWarning(
                "Message sent to DLQ {DlqTopic} [partition:{Partition}, offset:{Offset}]. Error: {Error}",
                dlqTopic, result.Partition.Value, result.Offset.Value, exception.Message);
        }
        catch (ProduceException<string, string> ex)
        {
            _logger.LogError(ex, "Failed to send message to DLQ {DlqTopic}", dlqTopic);
            throw;
        }
    }

    private static Headers CloneHeaders(Headers? source)
    {
        var headers = new Headers();
        if (source is null) return headers;

        foreach (var header in source)
        {
            headers.Add(header.Key, header.GetValueBytes());
        }

        return headers;
    }
}
