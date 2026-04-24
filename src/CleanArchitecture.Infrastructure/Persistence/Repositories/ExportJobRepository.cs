using CleanArchitecture.Domain.Entities;
using CleanArchitecture.Domain.Interfaces.Export;
using Microsoft.EntityFrameworkCore;

namespace CleanArchitecture.Infrastructure.Persistence.Repositories;

public class ExportJobRepository : IExportJobRepository
{
    private readonly AppDbContext _db;

    public ExportJobRepository(AppDbContext db) => _db = db;

    public async Task<ExportJob> AddAsync(ExportJob job, CancellationToken ct = default)
    {
        await _db.ExportJobs.AddAsync(job, ct);
        return job;
    }

    public Task<ExportJob?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => _db.ExportJobs.FirstOrDefaultAsync(x => x.Id == id, ct);

    public async Task<IEnumerable<ExportJob>> GetByUserIdAsync(Guid userId, CancellationToken ct = default)
        => await _db.ExportJobs
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(ct);

    public Task UpdateAsync(ExportJob job, CancellationToken ct = default)
    {
        _db.ExportJobs.Update(job);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken ct = default)
        => _db.SaveChangesAsync(ct);
}