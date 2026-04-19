using CleanArchitecture.Domain.Entities;

namespace CleanArchitecture.Application.Export.Interfaces;

/// <summary>
/// Repository for managing exported files.
/// </summary>
public interface IExportedFileRepository
{
    /// <summary>
    /// Add new exported file record.
    /// </summary>
    Task<ExportedFile> AddAsync(ExportedFile exportedFile, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get exported file by id.
    /// </summary>
    Task<ExportedFile?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all exported files for a user.
    /// </summary>
    Task<IEnumerable<ExportedFile>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Update exported file.
    /// </summary>
    Task UpdateAsync(ExportedFile exportedFile, CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete exported file record.
    /// </summary>
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Save changes to database.
    /// </summary>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
