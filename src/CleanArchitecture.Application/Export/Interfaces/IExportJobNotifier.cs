using CleanArchitecture.Application.Export.DTOs;

namespace CleanArchitecture.Application.Export.Interfaces;

public interface IExportJobNotifier
{
    Task NotifyJobCompletedAsync(Guid userId, ExportJobStatusResponse status, CancellationToken ct = default);
    Task NotifyJobFailedAsync(Guid userId, ExportJobStatusResponse status, CancellationToken ct = default);
}