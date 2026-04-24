// CleanArchitecture.Application/Export/Interfaces/ITemplateManagementService.cs
using CleanArchitecture.Application.Export.DTOs;

namespace CleanArchitecture.Application.Export.Interfaces;

public interface ITemplateManagementService
{
    Task<TemplateDto> UploadAsync(UploadTemplateRequest request, Guid userId, CancellationToken ct = default);
    Task<IEnumerable<TemplateDto>> ListAsync(string? type = null, string? category = null, CancellationToken ct = default);
    Task<IEnumerable<TemplateDto>> GetVersionsAsync(string type, string name, CancellationToken ct = default);
    Task DeactivateAsync(Guid id, CancellationToken ct = default);
    Task ActivateAsync(Guid id, CancellationToken ct = default);
}