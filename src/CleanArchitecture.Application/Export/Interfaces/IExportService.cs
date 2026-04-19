using CleanArchitecture.Application.Export.DTOs;

namespace CleanArchitecture.Application.Export.Interfaces;

/// <summary>
/// Service for orchestrating data export to files and storage.
/// </summary>
public interface IExportService
{
    /// <summary>
    /// Export data to file and upload to MinIO.
    /// </summary>
    /// <param name="request">Export request with data and format</param>
    /// <param name="userId">User requesting the export</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Export file response with URL</returns>
    Task<ExportFileResponse> ExportDataAsync(
        ExportDataRequest request,
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get export file details by id.
    /// </summary>
    Task<ExportFileResponse?> GetExportFileAsync(Guid exportId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all export files for current user.
    /// </summary>
    Task<IEnumerable<ExportFileResponse>> GetUserExportFilesAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete export file.
    /// </summary>
    Task DeleteExportFileAsync(Guid exportId, CancellationToken cancellationToken = default);
}
