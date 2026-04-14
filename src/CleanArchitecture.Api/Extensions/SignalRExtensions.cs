using CleanArchitecture.Api.Hubs;
using Microsoft.Extensions.DependencyInjection;

namespace CleanArchitecture.Api.Extensions;

/// <summary>
/// Extension methods for SignalR setup
/// </summary>
public static class SignalRExtensions
{
    public static IServiceCollection AddPermissionSignalR(this IServiceCollection services)
    {
        services.AddSignalR();
        return services;
    }
}
