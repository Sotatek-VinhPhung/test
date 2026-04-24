using CleanArchitecture.Application.Export.Interfaces;
using CleanArchitecture.Domain.Entities;
using CleanArchitecture.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CleanArchitecture.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository for managing ExportedFile entities.
/// Implements repository pattern for data access layer.
/// </summary>
public class ExportedFileRepository : IExportedFileRepository
{
    private readonly AppDbContext _context;

    public ExportedFileRepository(AppDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<ExportedFile> AddAsync(
        ExportedFile exportedFile,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(exportedFile);
        var entry = await _context.ExportedFiles.AddAsync(exportedFile, cancellationToken);
        return entry.Entity;
    }

    public async Task<ExportedFile?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await _context.ExportedFiles
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<ExportedFile>> GetByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return await _context.ExportedFiles
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task UpdateAsync(
        ExportedFile exportedFile,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(exportedFile);
        _context.ExportedFiles.Update(exportedFile);
        await Task.CompletedTask;
    }

    public async Task DeleteAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var exportedFile = await GetByIdAsync(id, cancellationToken);
        if (exportedFile != null)
        {
            _context.ExportedFiles.Remove(exportedFile);
        }
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<ExportedFile?> FindByCacheKeyAsync(
    string cacheKey, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        return await _context.ExportedFiles
            .Where(x => x.CacheKey == cacheKey
                     && (x.ExpiresAt == null || x.ExpiresAt > now))
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync(ct);
    }
}
