using System.Text.Json;
using CleanArchitecture.Domain.Interfaces.Messaging;
using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CleanArchitecture.Infrastructure.Messaging;

/// <summary>
/// Background consumer worker — one instance per registered IMessageHandler&lt;T&gt;.
/// Subscribes to the handler's topic, deserializes JSON, dispatches to handler,
/// commits offsets manually, and routes failures to DLQ after max retries.
/// Creates a DI scope per message to support scoped dependencies (e.g. DbContext).
/// </summary>
public class KafkaConsumerService<T> : BackgroundService where T : class
{
    private readonly KafkaClient _client;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly KafkaDlqHandler _dlqHandler;
    private readonly KafkaSettings _settings;
    private readonly ILogger<KafkaConsumerService<T>> _logger;
    private string? _topic;

    public KafkaConsumerService(
        KafkaClient client,
        IServiceScopeFactory scopeFactory,
        KafkaDlqHandler dlqHandler,
        IOptions<KafkaSettings> settings,
        ILogger<KafkaConsumerService<T>> logger)
    {
        _client = client;
        _scopeFactory = scopeFactory;
        _dlqHandler = dlqHandler;
        _settings = settings.Value;
        _logger = logger;
    }

    /// <summary>
    /// Lazily resolves the handler topic within a DI scope.
    /// Called once at consumer startup, then cached in _topic.
    /// </summary>
    private string GetTopic()
    {
        if (_topic is not null)
            return _topic;

        using var scope = _scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<IMessageHandler<T>>();
        return _topic = handler.Topic;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Run on a dedicated thread to avoid blocking the host
        return Task.Run(() => ConsumeLoop(stoppingToken), stoppingToken);
    }

    private async Task ConsumeLoop(CancellationToken stoppingToken)
    {
        var topic = GetTopic();
        _logger.LogInformation("Starting consumer for topic {Topic} (handler: {Handler})",
            topic, typeof(T).Name);

        using var consumer = _client.BuildConsumer();
        consumer.Subscribe(topic);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                ConsumeResult<string, string>? result = null;
                try
                {
                    result = consumer.Consume(stoppingToken);
                    if (result?.Message is null) continue;

                    await HandleWithRetryAsync(result, consumer, stoppingToken);
                }
                catch (ConsumeException ex)
                {
                    _logger.LogError(ex, "Consumer error on topic {Topic}: {Error}",
                        topic, ex.Error.Reason);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
            }
        }
        finally
        {
            consumer.Close();
            _logger.LogInformation("Consumer stopped for topic {Topic}", GetTopic());
        }
    }

    private async Task HandleWithRetryAsync(
        ConsumeResult<string, string> result,
        IConsumer<string, string> consumer,
        CancellationToken stoppingToken)
    {
        var topic = GetTopic();

        // Deserialize once before retries — malformed JSON is non-transient, send to DLQ immediately
        T message;
        try
        {
            message = JsonSerializer.Deserialize<T>(result.Message.Value)
                ?? throw new JsonException($"Deserialization returned null for {typeof(T).Name}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Non-transient deserialization error on {Topic}, sending to DLQ", topic);
            await _dlqHandler.SendToDlqAsync(topic, result, ex, stoppingToken);
            consumer.Commit(result);
            return;
        }

        Exception? lastException = null;

        for (var attempt = 1; attempt <= _settings.MaxRetryAttempts; attempt++)
        {
            try
            {
                // Create a scope per message to support scoped dependencies (DbContext, UoW)
                using var scope = _scopeFactory.CreateScope();
                var handler = scope.ServiceProvider.GetRequiredService<IMessageHandler<T>>();

                await handler.HandleAsync(message, stoppingToken);
                consumer.Commit(result);

                _logger.LogDebug("Handled message from {Topic} [offset:{Offset}]",
                    topic, result.Offset.Value);
                return;
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                lastException = ex;
                _logger.LogWarning(
                    "Handler failed for {Topic} (attempt {Attempt}/{Max}): {Error}",
                    topic, attempt, _settings.MaxRetryAttempts, ex.Message);

                if (attempt < _settings.MaxRetryAttempts)
                    await Task.Delay(_settings.RetryDelayMs, stoppingToken);
            }
        }

        // All retries exhausted — send to DLQ
        if (lastException is not null)
        {
            await _dlqHandler.SendToDlqAsync(topic, result, lastException, stoppingToken);
        }

        // Commit to move past the failed message
        consumer.Commit(result);
    }
}
