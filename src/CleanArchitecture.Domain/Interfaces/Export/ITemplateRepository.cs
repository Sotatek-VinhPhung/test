// CleanArchitecture.Domain/Interfaces/Export/ITemplateRepository.cs
using CleanArchitecture.Domain.Entities;

namespace CleanArchitecture.Domain.Interfaces.Export;

public interface ITemplateRepository
{
    Task<Template> AddAsync(Template template, CancellationToken ct = default);
    Task<Template?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Template?> GetActiveByTypeAndNameAsync(string type, string name, CancellationToken ct = default);
    Task<IEnumerable<Template>> ListAsync(string? type = null, string? category = null, CancellationToken ct = default);
    Task<IEnumerable<Template>> GetVersionsAsync(string type, string name, CancellationToken ct = default);
    Task UpdateAsync(Template template, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}