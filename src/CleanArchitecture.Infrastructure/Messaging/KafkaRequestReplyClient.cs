using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using CleanArchitecture.Domain.Interfaces.Messaging;
using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CleanArchitecture.Infrastructure.Messaging;

/// <summary>
/// Request/reply over Kafka using correlation IDs. Publishes a request to a topic
/// and awaits a correlated reply on a dedicated reply topic.
/// </summary>
public class KafkaRequestReplyClient : IRequestReplyClient, IDisposable
{
    private readonly KafkaClient _client;
    private readonly KafkaSettings _settings;
    private readonly ILogger<KafkaRequestReplyClient> _logger;
    private readonly ConcurrentDictionary<string, TaskCompletionSource<string>> _pending = new();

    private IConsumer<string, string>? _replyConsumer;
    private Task? _replyListenerTask;
    private CancellationTokenSource? _cts;
    private readonly object _initLock = new();

    public KafkaRequestReplyClient(
        KafkaClient client,
        IOptions<KafkaSettings> settings,
        ILogger<KafkaRequestReplyClient> logger)
    {
        _client = client;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<TReply> SendAsync<TRequest, TReply>(
        string topic,
        TRequest request,
        CancellationToken cancellationToken = default)
        where TRequest : class
        where TReply : class
    {
        EnsureReplyListenerStarted();

        var correlationId = Guid.NewGuid().ToString();
        var tcs = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        _pending[correlationId] = tcs;

        try
        {
            // Publish request with correlation headers
            var producer = _client.GetProducer();
            var headers = new Headers
            {
                { "correlation-id", Encoding.UTF8.GetBytes(correlationId) },
                { "reply-topic", Encoding.UTF8.GetBytes(_settings.ReplyTopic) }
            };

            var message = new Message<string, string>
            {
                Key = correlationId,
                Value = JsonSerializer.Serialize(request),
                Headers = headers
            };

            await producer.ProduceAsync(topic, message, cancellationToken);
            _logger.LogDebug("Request sent to {Topic} with correlation {CorrelationId}",
                topic, correlationId);

            // Await reply with timeout
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(_settings.RequestReplyTimeoutMs);

            await using var registration = timeoutCts.Token.Register(() =>
                tcs.TrySetCanceled(timeoutCts.Token));

            var replyJson = await tcs.Task;

            return JsonSerializer.Deserialize<TReply>(replyJson)
                ?? throw new JsonException($"Reply deserialization returned null for {typeof(TReply).Name}");
        }
        finally
        {
            _pending.TryRemove(correlationId, out _);
        }
    }

    private void EnsureReplyListenerStarted()
    {
        if (_replyListenerTask is not null) return;

        lock (_initLock)
        {
            if (_replyListenerTask is not null) return;

            _cts = new CancellationTokenSource();
            _replyConsumer = _client.BuildConsumer(groupIdOverride: $"{_settings.GroupId}-reply");
            _replyConsumer.Subscribe(_settings.ReplyTopic);
            _replyListenerTask = Task.Run(() => ReplyListenerLoop(_cts.Token));

            _logger.LogInformation("Reply listener started on topic {ReplyTopic}", _settings.ReplyTopic);
        }
    }

    private void ReplyListenerLoop(CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var result = _replyConsumer!.Consume(cancellationToken);
                    if (result?.Message is null) continue;

                    var correlationId = ExtractHeader(result.Message.Headers, "correlation-id");
                    if (correlationId is null)
                    {
                        _logger.LogWarning("Reply message missing correlation-id header, skipping");
                        continue;
                    }

                    if (_pending.TryGetValue(correlationId, out var tcs))
                    {
                        tcs.TrySetResult(result.Message.Value);
                        _logger.LogDebug("Reply matched for correlation {CorrelationId}", correlationId);
                    }
                    else
                    {
                        _logger.LogDebug("No pending request for correlation {CorrelationId}", correlationId);
                    }
                }
                catch (ConsumeException ex)
                {
                    _logger.LogError(ex, "Reply listener consume error, retrying...");
                }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Graceful shutdown
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Reply listener fatal error, will restart on next SendAsync call");

            // Reset so EnsureReplyListenerStarted() can restart on next call
            lock (_initLock)
            {
                _replyListenerTask = null;
            }
        }
        finally
        {
            _replyConsumer?.Close();
        }
    }

    private static string? ExtractHeader(Headers? headers, string key)
    {
        if (headers is null) return null;

        var header = headers.FirstOrDefault(h => h.Key == key);
        return header is null ? null : Encoding.UTF8.GetString(header.GetValueBytes());
    }

    public void Dispose()
    {
        _cts?.Cancel();

        // Complete all pending requests with cancellation
        foreach (var (_, tcs) in _pending)
        {
            tcs.TrySetCanceled();
        }

        _pending.Clear();

        try
        {
            _replyListenerTask?.Wait(TimeSpan.FromSeconds(5));
        }
        catch (AggregateException)
        {
            // Expected on cancellation
        }

        _replyConsumer?.Dispose();
        _cts?.Dispose();
        _logger.LogInformation("Request/reply client disposed");
    }
}
