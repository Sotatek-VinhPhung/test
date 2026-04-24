using CleanArchitecture.Application.Common.Interfaces;
using CleanArchitecture.Application.Export.DTOs;
using CleanArchitecture.Application.Export.Helpers;
using CleanArchitecture.Application.Export.Interfaces;
using CleanArchitecture.Domain.Entities;
using CleanArchitecture.Domain.Interfaces.Export;
using Microsoft.Extensions.Logging;

namespace CleanArchitecture.Application.Export.Services;

public class ExportService : IExportService
{
    private readonly IExportedFileRepository _exportedFileRepository;
    private readonly IFileStorageService _fileStorageService;
    private readonly IExcelFileGenerator _excelGenerator;
    private readonly IWordFileGenerator _wordGenerator;
    private readonly IPdfFileGenerator _pdfGenerator;
    private readonly ICurrentUserService _currentUserService;
    private readonly IExcelTemplateEngine _excelTemplateEngine;
    private readonly IWordTemplateEngine _wordTemplateEngine;
    private readonly IGotenbergService _gotenbergService;
    private readonly ITemplateRepository _templateRepository;
    private readonly ILogger<ExportService> _logger;

    private readonly string _templateBasePath;
    private readonly string _minIOBucket;

    // Các field không tham gia vào cache key (ví dụ timestamp biến thiên)
    private static readonly string[] IgnoredCacheFields = { "ExportedAt", "RequestId", "GeneratedAt" };

    public ExportService(
        IExportedFileRepository exportedFileRepository,
        IFileStorageService fileStorageService,
        IExcelFileGenerator excelGenerator,
        IWordFileGenerator wordGenerator,
        IPdfFileGenerator pdfGenerator,
        ICurrentUserService currentUserService,
        IExcelTemplateEngine excelTemplateEngine,
        IWordTemplateEngine wordTemplateEngine,
        IGotenbergService gotenbergService,
        ITemplateRepository templateRepository,
        ILogger<ExportService> logger,
        string templateBasePath = "Templates",
        string minIOBucket = "exports")
    {
        _exportedFileRepository = exportedFileRepository;
        _fileStorageService = fileStorageService;
        _excelGenerator = excelGenerator;
        _wordGenerator = wordGenerator;
        _pdfGenerator = pdfGenerator;
        _currentUserService = currentUserService;
        _excelTemplateEngine = excelTemplateEngine;
        _wordTemplateEngine = wordTemplateEngine;
        _gotenbergService = gotenbergService;
        _templateRepository = templateRepository;
        _logger = logger;
        _templateBasePath = templateBasePath;
        _minIOBucket = minIOBucket;
    }

    // ============================================================
    // EXPORT DATA (Excel/Word/PDF)
    // ============================================================
    public async Task<ExportFileResponse> ExportDataAsync(
        ExportDataRequest request,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        await _fileStorageService.EnsureBucketExistsAsync(_minIOBucket, cancellationToken);

        // BƯỚC 1: Tính cache key (nếu có template)
        var (cacheKey, templateVersion) = await BuildCacheInfoAsync(
            request.Format,
            request.Format == ExportFormat.Word ? request.WordTemplateName : request.TemplateName,
            request.RawData,
            cancellationToken);

        var fileExtension = GetExtension(request.Format);

        // BƯỚC 2: Nếu có cacheKey → check MinIO + DB
        if (!string.IsNullOrEmpty(cacheKey))
        {
            var cacheObjectName = $"cache/{cacheKey}{fileExtension}";

            // Check MinIO (source of truth)
            var existsOnMinIO = await _fileStorageService.ObjectExistsAsync(
                _minIOBucket, cacheObjectName, cancellationToken);

            if (existsOnMinIO)
            {
                _logger.LogInformation("Cache HIT on MinIO: {CacheKey}", cacheKey);

                var freshUrl = await _fileStorageService.GetPresignedUrlAsync(
                    _minIOBucket, cacheObjectName, 3600, cancellationToken);

                // Thử lấy metadata từ DB
                var dbRecord = await _exportedFileRepository.FindByCacheKeyAsync(cacheKey, cancellationToken);

                if (dbRecord != null)
                {
                    var resp = MapToResponse(dbRecord);
                    resp.Url = freshUrl;
                    return resp;
                }

                // DB không có record (DB mất hoặc chưa sync) → tự sync lại
                return await SyncMinIOToDbAsync(
                    cacheKey, cacheObjectName, fileExtension,
                    request, userId, templateVersion, cancellationToken);
            }

            _logger.LogInformation("Cache MISS: {CacheKey}, generating new", cacheKey);
        }

        // BƯỚC 3: Gen file mới
        return await GenerateAndSaveAsync(
            request, userId, cacheKey, templateVersion, fileExtension, cancellationToken);
    }

    private async Task<ExportFileResponse> GenerateAndSaveAsync(
        ExportDataRequest request,
        Guid userId,
        string? cacheKey,
        int? templateVersion,
        string fileExtension,
        CancellationToken ct)
    {
        var (fileStream, contentType, _) = await GenerateFileAsync(request, ct);

        try
        {
            // Nếu có cacheKey → dùng làm object name (deterministic)
            // Nếu không → dùng timestamp như cũ
            var objectName = !string.IsNullOrEmpty(cacheKey)
                ? $"cache/{cacheKey}{fileExtension}"
                : GenerateObjectName(request.FileName, fileExtension);

            var fileUrl = await _fileStorageService.UploadFileAsync(
                _minIOBucket, objectName, fileStream, contentType, ct);

            var fileSize = await _fileStorageService.GetFileSizeAsync(
                _minIOBucket, objectName, ct);

            var exportedFile = new ExportedFile
            {
                Id = Guid.NewGuid(),
                FileName = $"{request.FileName}{fileExtension}",
                Url = fileUrl,
                Bucket = _minIOBucket,
                Size = fileSize,
                FileType = request.Format.ToString(),
                ObjectName = objectName,
                UserId = userId,
                Note = request.Note,
                ExpiresAt = request.ExpiresAt,
                CreatedAt = DateTime.UtcNow,
                CacheKey = cacheKey,
                TemplateVersion = templateVersion
            };

            // Lưu DB — nếu fail không rollback file MinIO
            try
            {
                await _exportedFileRepository.AddAsync(exportedFile, ct);
                await _exportedFileRepository.SaveChangesAsync(ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "DB save failed but file uploaded to MinIO: {ObjectName}", objectName);
            }

            return MapToResponse(exportedFile);
        }
        finally
        {
            fileStream?.Dispose();
        }
    }

    private async Task<ExportFileResponse> SyncMinIOToDbAsync(
        string cacheKey,
        string objectName,
        string fileExtension,
        ExportDataRequest request,
        Guid userId,
        int? templateVersion,
        CancellationToken ct)
    {
        _logger.LogInformation(
            "Syncing MinIO file to DB (DB missing record): {ObjectName}", objectName);

        var size = await _fileStorageService.GetFileSizeAsync(_minIOBucket, objectName, ct);
        var url = await _fileStorageService.GetPresignedUrlAsync(
            _minIOBucket, objectName, 3600, ct);

        var exportedFile = new ExportedFile
        {
            Id = Guid.NewGuid(),
            FileName = $"{request.FileName}{fileExtension}",
            Url = url,
            Bucket = _minIOBucket,
            Size = size,
            FileType = request.Format.ToString(),
            ObjectName = objectName,
            UserId = userId,
            CreatedAt = DateTime.UtcNow,
            CacheKey = cacheKey,
            TemplateVersion = templateVersion,
            Note = "Restored from MinIO cache"
        };

        try
        {
            await _exportedFileRepository.AddAsync(exportedFile, ct);
            await _exportedFileRepository.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "DB sync failed, serving from MinIO only");
        }

        return MapToResponse(exportedFile);
    }

    // ============================================================
    // PREVIEW PDF (Excel/Word → PDF)
    // ============================================================
    public async Task<ExportFileResponse> GeneratePreviewPdfAsync(
        PreviewPdfRequest request,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        if (request.SourceFormat == PreviewSourceFormat.Word
            && string.IsNullOrWhiteSpace(request.WordTemplateName))
            throw new ArgumentException("WordTemplateName is required");

        if (request.SourceFormat == PreviewSourceFormat.Excel
            && string.IsNullOrWhiteSpace(request.ExcelTemplateName))
            throw new ArgumentException("ExcelTemplateName is required");

        await _fileStorageService.EnsureBucketExistsAsync(_minIOBucket, cancellationToken);

        var templateName = request.SourceFormat == PreviewSourceFormat.Word
            ? request.WordTemplateName
            : request.ExcelTemplateName;
        var templateType = request.SourceFormat == PreviewSourceFormat.Word ? "Word" : "Excel";

        // 🔥 Cache key cho preview (format = "PDF-Preview" để phân biệt với export Word/Excel)
        var (cacheKey, templateVersion) = await BuildCacheInfoForPreviewAsync(
            templateType, templateName, request.RawData, request.SheetName, cancellationToken);

        // Check cache trên MinIO
        if (!string.IsNullOrEmpty(cacheKey))
        {
            var cacheObjectName = $"cache/{cacheKey}.pdf";

            var existsOnMinIO = await _fileStorageService.ObjectExistsAsync(
                _minIOBucket, cacheObjectName, cancellationToken);

            if (existsOnMinIO)
            {
                _logger.LogInformation("Preview cache HIT on MinIO: {CacheKey}", cacheKey);

                var freshUrl = await _fileStorageService.GetPresignedUrlAsync(
                    _minIOBucket, cacheObjectName, 3600, cancellationToken);

                var dbRecord = await _exportedFileRepository.FindByCacheKeyAsync(
                    cacheKey, cancellationToken);

                if (dbRecord != null)
                {
                    var resp = MapToResponse(dbRecord);
                    resp.Url = freshUrl;
                    return resp;
                }

                // Sync DB
                return await SyncPreviewMinIOToDbAsync(
                    cacheKey, cacheObjectName, request, userId, templateVersion, cancellationToken);
            }

            _logger.LogInformation("Preview cache MISS: {CacheKey}, generating new", cacheKey);
        }

        // Gen mới
        return await GeneratePreviewAndSaveAsync(
            request, userId, cacheKey, templateVersion, cancellationToken);
    }

    private async Task<ExportFileResponse> GeneratePreviewAndSaveAsync(
        PreviewPdfRequest request,
        Guid userId,
        string? cacheKey,
        int? templateVersion,
        CancellationToken ct)
    {
        // Fill template → Word/Excel stream
        Stream sourceStream;
        string sourceFileName;

        if (request.SourceFormat == PreviewSourceFormat.Word)
        {
            var templatePath = Path.Combine(
                _templateBasePath, "Word", $"{request.WordTemplateName}.docx");
            if (!File.Exists(templatePath))
                throw new FileNotFoundException($"Word template not found: {templatePath}");

            sourceStream = _wordTemplateEngine.FillTemplate(templatePath, request.RawData);
            sourceFileName = $"{request.FileName}.docx";
        }
        else
        {
            var templatePath = Path.Combine(
                _templateBasePath, "Excel", $"{request.ExcelTemplateName}.xlsx");
            if (!File.Exists(templatePath))
                throw new FileNotFoundException($"Excel template not found: {templatePath}");

            sourceStream = _excelTemplateEngine.FillTemplate(
                templatePath, request.RawData, request.SheetName);
            sourceFileName = $"{request.FileName}.xlsx";
        }

        // Convert sang PDF
        Stream pdfStream;
        try
        {
            if (sourceStream.CanSeek) sourceStream.Position = 0;
            pdfStream = await _gotenbergService.ConvertOfficeToPdfAsync(
                sourceStream, sourceFileName, ct);
        }
        finally
        {
            sourceStream.Dispose();
        }

        try
        {
            // Object name: dùng cacheKey nếu có, nếu không thì timestamp
            var objectName = !string.IsNullOrEmpty(cacheKey)
                ? $"cache/{cacheKey}.pdf"
                : GenerateObjectName(request.FileName, ".pdf");

            var fileUrl = await _fileStorageService.UploadFileAsync(
                _minIOBucket, objectName, pdfStream, "application/pdf", ct);

            var fileSize = await _fileStorageService.GetFileSizeAsync(
                _minIOBucket, objectName, ct);

            var previewUrl = await _fileStorageService.GetPresignedUrlAsync(
                _minIOBucket, objectName, 3600, ct);

            var exportedFile = new ExportedFile
            {
                Id = Guid.NewGuid(),
                FileName = $"{request.FileName}.pdf",
                Url = fileUrl,
                Bucket = _minIOBucket,
                Size = fileSize,
                FileType = "PDF",
                ObjectName = objectName,
                UserId = userId,
                Note = request.Note ?? "Preview PDF",
                ExpiresAt = request.ExpiresAt,
                CreatedAt = DateTime.UtcNow,
                CacheKey = cacheKey,
                TemplateVersion = templateVersion
            };

            if (request.SaveToHistory)
            {
                try
                {
                    await _exportedFileRepository.AddAsync(exportedFile, ct);
                    await _exportedFileRepository.SaveChangesAsync(ct);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "DB save failed for preview, file in MinIO");
                }
            }
            else if (!string.IsNullOrEmpty(cacheKey))
            {
                // Dù SaveToHistory=false, vẫn lưu DB nếu có cacheKey để support cache
                try
                {
                    await _exportedFileRepository.AddAsync(exportedFile, ct);
                    await _exportedFileRepository.SaveChangesAsync(ct);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "DB save failed for preview cache");
                }
            }

            var mapped = MapToResponse(exportedFile);
            mapped.Url = previewUrl;
            return mapped;
        }
        finally
        {
            pdfStream?.Dispose();
        }
    }

    private async Task<ExportFileResponse> SyncPreviewMinIOToDbAsync(
        string cacheKey,
        string objectName,
        PreviewPdfRequest request,
        Guid userId,
        int? templateVersion,
        CancellationToken ct)
    {
        var size = await _fileStorageService.GetFileSizeAsync(_minIOBucket, objectName, ct);
        var url = await _fileStorageService.GetPresignedUrlAsync(
            _minIOBucket, objectName, 3600, ct);

        var exportedFile = new ExportedFile
        {
            Id = Guid.NewGuid(),
            FileName = $"{request.FileName}.pdf",
            Url = url,
            Bucket = _minIOBucket,
            Size = size,
            FileType = "PDF",
            ObjectName = objectName,
            UserId = userId,
            CreatedAt = DateTime.UtcNow,
            CacheKey = cacheKey,
            TemplateVersion = templateVersion,
            Note = "Restored from MinIO cache"
        };

        try
        {
            await _exportedFileRepository.AddAsync(exportedFile, ct);
            await _exportedFileRepository.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "DB sync failed for preview");
        }

        return MapToResponse(exportedFile);
    }

    // ============================================================
    // CACHE KEY BUILDERS
    // ============================================================
    private async Task<(string? cacheKey, int? version)> BuildCacheInfoAsync(
        ExportFormat format,
        string? templateName,
        Dictionary<string, object?>? rawData,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(templateName)) return (null, null);

        var templateType = format == ExportFormat.Word ? "Word"
                        : format == ExportFormat.Excel ? "Excel"
                        : null;

        if (templateType == null) return (null, null);

        var template = await _templateRepository.GetActiveByTypeAndNameAsync(
            templateType, templateName, ct);

        var version = template?.Version ?? 1;

        var cacheKey = CacheKeyBuilder.Build(
            templateType,
            templateName,
            version,
            rawData,
            format.ToString(),
            IgnoredCacheFields);

        return (cacheKey, version);
    }

    private async Task<(string? cacheKey, int? version)> BuildCacheInfoForPreviewAsync(
        string templateType,
        string? templateName,
        Dictionary<string, object?>? rawData,
        string? sheetName,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(templateName)) return (null, null);

        var template = await _templateRepository.GetActiveByTypeAndNameAsync(
            templateType, templateName, ct);

        var version = template?.Version ?? 1;

        // Gộp sheetName vào cache key (khác sheet → khác cache)
        var format = $"PDF-Preview|Sheet={sheetName ?? ""}";

        var cacheKey = CacheKeyBuilder.Build(
            templateType,
            templateName,
            version,
            rawData,
            format,
            IgnoredCacheFields);

        return (cacheKey, version);
    }

    // ============================================================
    // FILE GENERATION (giữ nguyên)
    // ============================================================
    private async Task<(Stream, string, string)> GenerateFileAsync(
        ExportDataRequest request,
        CancellationToken cancellationToken)
    {
        return request.Format switch
        {
            ExportFormat.Excel =>
            (
                GenerateExcelFromTemplate(request),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                ".xlsx"
            ),

            ExportFormat.Word =>
            (
                GenerateWordFromTemplate(request),
                "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                ".docx"
            ),

            ExportFormat.PDF =>
                (await _pdfGenerator.GenerateFromHtmlAsync(
                    request.FileName,
                    FormatDataAsHtml(request.Data),
                    cancellationToken),
                "application/pdf",
                ".pdf"),

            _ => throw new InvalidOperationException($"Unsupported export format: {request.Format}")
        };
    }

    private Stream GenerateExcelFromTemplate(ExportDataRequest request)
    {
        var templatePath = Path.Combine(
            _templateBasePath, "Excel", $"{request.TemplateName}.xlsx");

        if (!File.Exists(templatePath))
            throw new FileNotFoundException($"Excel template not found: {templatePath}");

        return _excelTemplateEngine.FillTemplate(
            templatePath, request.RawData, request.SheetName);
    }

    private Stream GenerateWordFromTemplate(ExportDataRequest request)
    {
        if (!string.IsNullOrWhiteSpace(request.WordTemplateName))
        {
            var templatePath = Path.Combine(
                _templateBasePath, "Word", $"{request.WordTemplateName}.docx");

            return _wordTemplateEngine.FillTemplate(templatePath, request.RawData);
        }

        throw new ArgumentException(
            "WordTemplateName is required for Word export.");
    }

    // ============================================================
    // Các method còn lại giữ nguyên
    // ============================================================
    public async Task<ExportFileResponse?> GetExportFileAsync(
        Guid exportId, CancellationToken cancellationToken = default)
    {
        var exportedFile = await _exportedFileRepository.GetByIdAsync(exportId, cancellationToken);
        return exportedFile != null ? MapToResponse(exportedFile) : null;
    }

    public async Task<IEnumerable<ExportFileResponse>> GetUserExportFilesAsync(
        Guid userId, CancellationToken cancellationToken = default)
    {
        var exportedFiles = await _exportedFileRepository.GetByUserIdAsync(userId, cancellationToken);
        return exportedFiles.Select(MapToResponse);
    }

    public async Task DeleteExportFileAsync(
        Guid exportId, CancellationToken cancellationToken = default)
    {
        var exportedFile = await _exportedFileRepository.GetByIdAsync(exportId, cancellationToken);
        if (exportedFile == null)
            throw new KeyNotFoundException($"Export file with id {exportId} not found.");

        await _fileStorageService.DeleteFileAsync(
            exportedFile.Bucket, exportedFile.ObjectName, cancellationToken);

        await _exportedFileRepository.DeleteAsync(exportId, cancellationToken);
        await _exportedFileRepository.SaveChangesAsync(cancellationToken);
    }

    private string FormatDataAsHtml(IEnumerable<Dictionary<string, object?>>? data)
    {
        if (data == null || !data.Any()) return "<p>No data</p>";

        var html = new System.Text.StringBuilder();
        html.AppendLine("<table border='1'>");
        var first = data.First();
        html.AppendLine("<tr>");
        foreach (var key in first.Keys)
            html.AppendLine($"<th>{key}</th>");
        html.AppendLine("</tr>");
        foreach (var row in data)
        {
            html.AppendLine("<tr>");
            foreach (var val in row.Values)
                html.AppendLine($"<td>{val}</td>");
            html.AppendLine("</tr>");
        }
        html.AppendLine("</table>");
        return html.ToString();
    }

    private string GenerateObjectName(string fileName, string extension)
    {
        var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
        var sanitized = System.Text.RegularExpressions.Regex
            .Replace(fileName, @"[^a-zA-Z0-9._-]", "_");
        return $"{sanitized}_{timestamp}{extension}";
    }

    private static string GetExtension(ExportFormat format) => format switch
    {
        ExportFormat.Word => ".docx",
        ExportFormat.Excel => ".xlsx",
        ExportFormat.PDF => ".pdf",
        _ => ".bin"
    };

    private ExportFileResponse MapToResponse(ExportedFile exportedFile)
    {
        return new ExportFileResponse
        {
            Id = exportedFile.Id,
            FileName = exportedFile.FileName,
            Url = exportedFile.Url,
            Size = exportedFile.Size,
            FileType = exportedFile.FileType,
            CreatedAt = exportedFile.CreatedAt,
            ExpiresAt = exportedFile.ExpiresAt,
            Bucket = exportedFile.Bucket
        };
    }
}