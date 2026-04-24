using CleanArchitecture.Application.Export.DTOs;

namespace CleanArchitecture.Application.Export.Interfaces;

public interface IExportJobService
{
    Task<Guid> EnqueueExportAsync(
        ExportDataRequest request, Guid userId, CancellationToken ct = default);

    Task<Guid> EnqueuePreviewAsync(
        PreviewPdfRequest request, Guid userId, CancellationToken ct = default);

    Task<ExportJobStatusResponse?> GetStatusAsync(
        Guid jobId, CancellationToken ct = default);
}