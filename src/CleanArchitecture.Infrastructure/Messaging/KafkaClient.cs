using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CleanArchitecture.Infrastructure.Messaging;

/// <summary>
/// Singleton Kafka connection manager. Builds and caches producer/consumer instances.
/// </summary>
public sealed class KafkaClient : IDisposable
{
    private readonly KafkaSettings _settings;
    private readonly ILogger<KafkaClient> _logger;
    private IProducer<string, string>? _producer;
    private readonly object _lock = new();

    public KafkaClient(IOptions<KafkaSettings> settings, ILogger<KafkaClient> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    /// <summary>
    /// Returns the shared singleton producer. Lazy-initialized, thread-safe.
    /// </summary>
    public IProducer<string, string> GetProducer()
    {
        if (_producer is not null) return _producer;

        lock (_lock)
        {
            if (_producer is not null) return _producer;

            var config = new ProducerConfig
            {
                BootstrapServers = _settings.BootstrapServers,
                Acks = Acks.All,
                MessageTimeoutMs = _settings.RequestTimeoutMs
            };

            _producer = new ProducerBuilder<string, string>(config)
                .SetErrorHandler((_, error) =>
                    _logger.LogError("Kafka producer error: {Error}", error.Reason))
                .Build();

            _logger.LogInformation("Kafka producer created for {Servers}", _settings.BootstrapServers);
        }

        return _producer;
    }

    /// <summary>
    /// Builds a new consumer instance. Each consumer service gets its own.
    /// </summary>
    public IConsumer<string, string> BuildConsumer(string? groupIdOverride = null)
    {
        var autoOffsetReset = Enum.TryParse<AutoOffsetReset>(_settings.AutoOffsetReset, true, out var parsed)
            ? parsed
            : AutoOffsetReset.Earliest;

        var config = new ConsumerConfig
        {
            BootstrapServers = _settings.BootstrapServers,
            GroupId = groupIdOverride ?? _settings.GroupId,
            AutoOffsetReset = autoOffsetReset,
            EnableAutoCommit = _settings.EnableAutoCommit,
            AllowAutoCreateTopics = true   //THÊM DÒNG NÀY
        };

        var consumer = new ConsumerBuilder<string, string>(config)
            .SetErrorHandler((_, error) =>
                _logger.LogError("Kafka consumer error: {Error}", error.Reason))
            .SetPartitionsAssignedHandler((_, partitions) =>
                _logger.LogInformation("Consumer assigned partitions: {Partitions}",
                    string.Join(", ", partitions)))
            .Build();

        _logger.LogInformation("Kafka consumer created for group {GroupId}", config.GroupId);
        return consumer;
    }

    public void Dispose()
    {
        IProducer<string, string>? producer;

        lock (_lock)
        {
            producer = _producer;
            _producer = null;
        }

        if (producer is null) return;

        try
        {
            producer.Flush(TimeSpan.FromSeconds(5));
            producer.Dispose();
            _logger.LogInformation("Kafka producer disposed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error disposing Kafka producer");
        }
    }
}
