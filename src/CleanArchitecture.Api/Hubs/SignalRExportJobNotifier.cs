using CleanArchitecture.Application.Export.DTOs;
using CleanArchitecture.Application.Export.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace CleanArchitecture.Api.Hubs;

public class SignalRExportJobNotifier : IExportJobNotifier
{
    private readonly IHubContext<ExportJobHub> _hub;

    public SignalRExportJobNotifier(IHubContext<ExportJobHub> hub) => _hub = hub;

    public Task NotifyJobCompletedAsync(
        Guid userId, ExportJobStatusResponse status, CancellationToken ct = default)
        => _hub.Clients.Group($"user-{userId}")
               .SendAsync("exportJobCompleted", status, ct);

    public Task NotifyJobFailedAsync(
        Guid userId, ExportJobStatusResponse status, CancellationToken ct = default)
        => _hub.Clients.Group($"user-{userId}")
               .SendAsync("exportJobFailed", status, ct);
}