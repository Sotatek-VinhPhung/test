using CleanArchitecture.Application.Common.Interfaces;
using CleanArchitecture.Application.Export.Interfaces;
using CleanArchitecture.Domain.Interfaces.Export;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace CleanArchitecture.Application.Export.Services;

public class TemplateService : ITemplateService
{
    private readonly ITemplateRepository _repo;
    private readonly IFileStorageService _storage;
    private readonly IMemoryCache _cache;
    private readonly ILogger<TemplateService> _logger;
    private readonly string _localFallbackPath;

    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

    public TemplateService(
        ITemplateRepository repo,
        IFileStorageService storage,
        IMemoryCache cache,
        ILogger<TemplateService> logger,
        string localFallbackPath = "Templates")
    {
        _repo = repo;
        _storage = storage;
        _cache = cache;
        _logger = logger;
        _localFallbackPath = localFallbackPath;
    }

    public async Task<Stream> GetTemplateStreamAsync(string type, string name, CancellationToken ct = default)
    {
        var cacheKey = $"template:{type.ToLowerInvariant()}:{name.ToLowerInvariant()}";

        if (_cache.TryGetValue<byte[]>(cacheKey, out var cached) && cached != null)
            return new MemoryStream(cached, writable: false);

        var template = await _repo.GetActiveByTypeAndNameAsync(type, name, ct);
        byte[] bytes;

        if (template != null)
        {
            _logger.LogInformation("Loading template from MinIO: {Type}/{Name} v{Version}",
                type, name, template.Version);

            await using var minioStream = await _storage.DownloadFileAsync(
                template.Bucket, template.ObjectName, ct);
            using var ms = new MemoryStream();
            await minioStream.CopyToAsync(ms, ct);
            bytes = ms.ToArray();
        }
        else
        {
            var subFolder = type.Equals("Word", StringComparison.OrdinalIgnoreCase) ? "Word" : "Excel";
            var ext = type.Equals("Word", StringComparison.OrdinalIgnoreCase) ? ".docx" : ".xlsx";
            var localPath = Path.Combine(_localFallbackPath, subFolder, $"{name}{ext}");

            if (!File.Exists(localPath))
                throw new FileNotFoundException($"Template not found: {type}/{name}");

            _logger.LogWarning("Template {Type}/{Name} using local file fallback", type, name);
            bytes = await File.ReadAllBytesAsync(localPath, ct);
        }

        _cache.Set(cacheKey, bytes, CacheDuration);
        return new MemoryStream(bytes, writable: false);
    }

    public void InvalidateCache(string type, string name)
    {
        _cache.Remove($"template:{type.ToLowerInvariant()}:{name.ToLowerInvariant()}");
    }
}