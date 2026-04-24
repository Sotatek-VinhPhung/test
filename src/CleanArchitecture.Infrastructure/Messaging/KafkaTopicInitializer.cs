using Confluent.Kafka;
using Confluent.Kafka.Admin;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CleanArchitecture.Infrastructure.Messaging;

/// <summary>
/// Khởi tạo các topic Kafka cần thiết khi app startup.
/// Tránh lỗi "Unknown topic or partition" lần chạy đầu tiên.
/// </summary>
public class KafkaTopicInitializer : IHostedService
{
    private readonly KafkaSettings _settings;
    private readonly ILogger<KafkaTopicInitializer> _logger;

    // Danh sách topic cần tạo sẵn
    private static readonly string[] RequiredTopics =
    {
        "export-requested",
        "preview-requested",
        "export-requested.dlq",
        "preview-requested.dlq"
    };

    public KafkaTopicInitializer(
        IOptions<KafkaSettings> settings,
        ILogger<KafkaTopicInitializer> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            var config = new AdminClientConfig
            {
                BootstrapServers = _settings.BootstrapServers,
                SocketTimeoutMs = 10000
            };

            using var admin = new AdminClientBuilder(config).Build();

            // Lấy metadata hiện tại để check topic nào đã tồn tại
            var metadata = admin.GetMetadata(TimeSpan.FromSeconds(10));
            var existingTopics = metadata.Topics.Select(t => t.Topic).ToHashSet();

            var topicsToCreate = RequiredTopics
                .Where(t => !existingTopics.Contains(t))
                .Select(t => new TopicSpecification
                {
                    Name = t,
                    NumPartitions = 1,
                    ReplicationFactor = 1
                })
                .ToList();

            if (topicsToCreate.Count == 0)
            {
                _logger.LogInformation("All Kafka topics already exist");
                return;
            }

            await admin.CreateTopicsAsync(topicsToCreate);

            _logger.LogInformation("Kafka topics created: {Topics}",
                string.Join(", ", topicsToCreate.Select(t => t.Name)));
        }
        catch (CreateTopicsException ex)
        {
            // Topic đã tồn tại khi race condition → bỏ qua
            foreach (var result in ex.Results)
            {
                if (result.Error.Code == ErrorCode.TopicAlreadyExists)
                {
                    _logger.LogDebug("Topic {Topic} already exists", result.Topic);
                }
                else
                {
                    _logger.LogWarning("Failed to create topic {Topic}: {Error}",
                        result.Topic, result.Error.Reason);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to initialize Kafka topics. App will still start but " +
                "topics will be auto-created on first message");
            // Không throw — cho app tiếp tục chạy, Kafka sẽ tự tạo topic sau
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}