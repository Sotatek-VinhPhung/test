// CleanArchitecture.Application/Export/Services/TemplateManagementService.cs
using CleanArchitecture.Application.Common.Interfaces;
using CleanArchitecture.Application.Export.DTOs;
using CleanArchitecture.Application.Export.Interfaces;
using CleanArchitecture.Domain.Entities;
using CleanArchitecture.Domain.Interfaces.Export;

namespace CleanArchitecture.Application.Export.Services;

public class TemplateManagementService : ITemplateManagementService
{
    private readonly ITemplateRepository _repo;
    private readonly IFileStorageService _storage;
    private readonly ITemplateService _templateService;
    private const string TemplatesBucket = "templates";

    public TemplateManagementService(
        ITemplateRepository repo,
        IFileStorageService storage,
        ITemplateService templateService)
    {
        _repo = repo;
        _storage = storage;
        _templateService = templateService;
    }

    public async Task<TemplateDto> UploadAsync(UploadTemplateRequest request, Guid userId, CancellationToken ct = default)
    {
        if (request.Type != "Word" && request.Type != "Excel")
            throw new ArgumentException("Type must be Word or Excel");

        await _storage.EnsureBucketExistsAsync(TemplatesBucket, ct);

        var existing = (await _repo.GetVersionsAsync(request.Type, request.Name, ct)).ToList();
        var newVersion = existing.Any() ? existing.Max(x => x.Version) + 1 : 1;

        foreach (var old in existing.Where(x => x.IsActive))
        {
            old.IsActive = false;
            old.UpdatedAt = DateTime.UtcNow;
            await _repo.UpdateAsync(old, ct);
        }

        var ext = request.Type == "Word" ? "docx" : "xlsx";
        var objectName = $"{request.Type.ToLowerInvariant()}/{request.Name}_v{newVersion}.{ext}";
        var contentType = request.Type == "Word"
            ? "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
            : "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

        await _storage.UploadFileAsync(TemplatesBucket, objectName, request.FileStream, contentType, ct);
        var size = await _storage.GetFileSizeAsync(TemplatesBucket, objectName, ct);

        var template = new Template
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Type = request.Type,
            Description = request.Description,
            Category = request.Category,
            Bucket = TemplatesBucket,
            ObjectName = objectName,
            FileName = request.FileName,
            Size = size,
            Version = newVersion,
            IsActive = true,
            UploadedBy = userId,
            CreatedAt = DateTime.UtcNow
        };

        await _repo.AddAsync(template, ct);
        await _repo.SaveChangesAsync(ct);
        _templateService.InvalidateCache(request.Type, request.Name);

        return ToDto(template);
    }

    public async Task<IEnumerable<TemplateDto>> ListAsync(string? type = null, string? category = null, CancellationToken ct = default)
        => (await _repo.ListAsync(type, category, ct)).Select(ToDto);

    public async Task<IEnumerable<TemplateDto>> GetVersionsAsync(string type, string name, CancellationToken ct = default)
        => (await _repo.GetVersionsAsync(type, name, ct)).Select(ToDto);

    public async Task DeactivateAsync(Guid id, CancellationToken ct = default)
    {
        var t = await _repo.GetByIdAsync(id, ct) ?? throw new KeyNotFoundException();
        t.IsActive = false;
        t.UpdatedAt = DateTime.UtcNow;
        await _repo.UpdateAsync(t, ct);
        await _repo.SaveChangesAsync(ct);
        _templateService.InvalidateCache(t.Type, t.Name);
    }

    public async Task ActivateAsync(Guid id, CancellationToken ct = default)
    {
        var t = await _repo.GetByIdAsync(id, ct) ?? throw new KeyNotFoundException();
        var others = await _repo.GetVersionsAsync(t.Type, t.Name, ct);
        foreach (var o in others.Where(x => x.Id != id && x.IsActive))
        {
            o.IsActive = false;
            o.UpdatedAt = DateTime.UtcNow;
            await _repo.UpdateAsync(o, ct);
        }
        t.IsActive = true;
        t.UpdatedAt = DateTime.UtcNow;
        await _repo.UpdateAsync(t, ct);
        await _repo.SaveChangesAsync(ct);
        _templateService.InvalidateCache(t.Type, t.Name);
    }

    private static TemplateDto ToDto(Template t) => new()
    {
        Id = t.Id,
        Name = t.Name,
        Type = t.Type,
        Description = t.Description,
        Category = t.Category,
        FileName = t.FileName,
        Size = t.Size,
        Version = t.Version,
        IsActive = t.IsActive,
        CreatedAt = t.CreatedAt
    };
}