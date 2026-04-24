// CleanArchitecture.Infrastructure/Persistence/Repositories/TemplateRepository.cs
using CleanArchitecture.Domain.Entities;
using CleanArchitecture.Domain.Interfaces.Export;
using Microsoft.EntityFrameworkCore;

namespace CleanArchitecture.Infrastructure.Persistence.Repositories;

public class TemplateRepository : ITemplateRepository
{
    private readonly AppDbContext _db;

    public TemplateRepository(AppDbContext db) => _db = db;

    public async Task<Template> AddAsync(Template t, CancellationToken ct = default)
    {
        await _db.Templates.AddAsync(t, ct);
        return t;
    }

    public Task<Template?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => _db.Templates.FirstOrDefaultAsync(x => x.Id == id, ct);

    public Task<Template?> GetActiveByTypeAndNameAsync(
        string type, string name, CancellationToken ct = default)
        => _db.Templates
            .Where(x => x.Type == type && x.Name == name && x.IsActive)
            .OrderByDescending(x => x.Version)
            .FirstOrDefaultAsync(ct);

    public async Task<IEnumerable<Template>> ListAsync(
        string? type = null, string? category = null, CancellationToken ct = default)
    {
        var q = _db.Templates.Where(x => x.IsActive);
        if (!string.IsNullOrEmpty(type)) q = q.Where(x => x.Type == type);
        if (!string.IsNullOrEmpty(category)) q = q.Where(x => x.Category == category);
        return await q.OrderBy(x => x.Name).ToListAsync(ct);
    }

    public async Task<IEnumerable<Template>> GetVersionsAsync(
        string type, string name, CancellationToken ct = default)
        => await _db.Templates
            .Where(x => x.Type == type && x.Name == name)
            .OrderByDescending(x => x.Version)
            .ToListAsync(ct);

    public Task UpdateAsync(Template t, CancellationToken ct = default)
    {
        _db.Templates.Update(t);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken ct = default)
        => _db.SaveChangesAsync(ct);
}