using System.Reflection;
using CleanArchitecture.Domain.Interfaces.Messaging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CleanArchitecture.Infrastructure.Messaging;

/// <summary>
/// Registers all Kafka services: client, publisher, consumer, DLQ, request/reply.
/// Auto-discovers IMessageHandler&lt;T&gt; implementations and registers a
/// KafkaConsumerService&lt;T&gt; hosted service per handler.
/// </summary>
public static class KafkaServiceRegistration
{
    /// <summary>
    /// Adds Kafka services to the DI container.
    /// Scans the provided assemblies (or calling assembly) for IMessageHandler&lt;T&gt; implementations.
    /// </summary>
    public static IServiceCollection AddKafkaServices(
        this IServiceCollection services,
        IConfiguration configuration,
        params Assembly[] handlerAssemblies)
    {
        // Bind KafkaSettings from appsettings.json
        services.Configure<KafkaSettings>(
            configuration.GetSection(KafkaSettings.SectionName));

        // Core services — singleton because producer is thread-safe and shared
        services.AddSingleton<KafkaClient>();
        services.AddSingleton<IKafkaPublisher, KafkaPublisher>();
        services.AddSingleton<KafkaDlqHandler>();
        services.AddSingleton<IRequestReplyClient, KafkaRequestReplyClient>();
        // 🔥 THÊM: Topic initializer chạy trước consumer
        services.AddHostedService<KafkaTopicInitializer>();

        // Auto-discover and register handlers + consumer hosted services
        var assemblies = handlerAssemblies.Length > 0
            ? handlerAssemblies
            : [Assembly.GetCallingAssembly()];

        foreach (var assembly in assemblies)
        {
            RegisterHandlers(services, assembly);
        }

        return services;
    }

    private static void RegisterHandlers(IServiceCollection services, Assembly assembly)
    {
        var handlerTypes = assembly.GetTypes()
            .Where(t => t is { IsAbstract: false, IsInterface: false })
            .Where(t => t.GetInterfaces().Any(i =>
                i.IsGenericType &&
                i.GetGenericTypeDefinition() == typeof(IMessageHandler<>)))
            .ToList();

        foreach (var handlerType in handlerTypes)
        {
            var handlerInterface = handlerType.GetInterfaces()
                .First(i => i.IsGenericType &&
                            i.GetGenericTypeDefinition() == typeof(IMessageHandler<>));

            var messageType = handlerInterface.GetGenericArguments()[0];

            // Register handler as scoped to support scoped dependencies (DbContext, UoW)
            services.AddScoped(handlerInterface, handlerType);

            // Register hosted consumer service: KafkaConsumerService<T>
            var consumerServiceType = typeof(KafkaConsumerService<>).MakeGenericType(messageType);
            services.AddSingleton(typeof(IHostedService), sp =>
                ActivatorUtilities.CreateInstance(sp, consumerServiceType));
        }

        // Deferred logging — runs after host is built when IHostedService starts
        if (handlerTypes.Count > 0)
        {
            var handlerNames = string.Join(", ", handlerTypes.Select(t => t.Name));
            var handlerCount = handlerTypes.Count;

            services.AddSingleton<IHostedService>(sp =>
            {
                var logger = sp.GetRequiredService<ILogger<KafkaClient>>();
                logger.LogInformation("[Kafka] Discovered {Count} message handler(s): {Handlers}",
                    handlerCount, handlerNames);
                return new NoOpHostedService();
            });
        }
    }

    /// <summary>No-op hosted service used solely to trigger deferred logging at startup.</summary>
    private sealed class NoOpHostedService : IHostedService
    {
        public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;
        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
