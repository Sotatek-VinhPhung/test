using CleanArchitecture.Domain.Entities;

namespace CleanArchitecture.Domain.Interfaces.Export;

public interface IExportJobRepository
{
    Task<ExportJob> AddAsync(ExportJob job, CancellationToken ct = default);
    Task<ExportJob?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IEnumerable<ExportJob>> GetByUserIdAsync(Guid userId, CancellationToken ct = default);
    Task UpdateAsync(ExportJob job, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}