// CleanArchitecture.Application/Export/Services/ExportService.cs

using CleanArchitecture.Application.Common.Interfaces;
using CleanArchitecture.Application.Export.DTOs;
using CleanArchitecture.Application.Export.Interfaces;
using CleanArchitecture.Domain.Entities;
using CleanArchitecture.Domain.Interfaces.Export;

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
    private readonly IWordTemplateEngine _wordTemplateEngine; // 🔥 THÊM
    private readonly IGotenbergService _gotenbergService;

    private readonly string _templateBasePath;
    private readonly string _minIOBucket;

    public ExportService(
        IExportedFileRepository exportedFileRepository,
        IFileStorageService fileStorageService,
        IExcelFileGenerator excelGenerator,
        IWordFileGenerator wordGenerator,
        IPdfFileGenerator pdfGenerator,
        ICurrentUserService currentUserService,
        IExcelTemplateEngine excelTemplateEngine,
        IWordTemplateEngine wordTemplateEngine, // 🔥 THÊM
        IGotenbergService gotenbergService,
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
        _wordTemplateEngine = wordTemplateEngine; // 🔥 THÊM
        _templateBasePath = templateBasePath;
        _minIOBucket = minIOBucket;
        _gotenbergService = gotenbergService;
    }

    public async Task<ExportFileResponse> ExportDataAsync(
        ExportDataRequest request,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        await _fileStorageService.EnsureBucketExistsAsync(_minIOBucket, cancellationToken);

        var (fileStream, contentType, fileExtension) = await GenerateFileAsync(request, cancellationToken);

        try
        {
            var objectName = GenerateObjectName(request.FileName, fileExtension);

            var fileUrl = await _fileStorageService.UploadFileAsync(
                _minIOBucket,
                objectName,
                fileStream,
                contentType,
                cancellationToken);

            var fileSize = await _fileStorageService.GetFileSizeAsync(
                _minIOBucket,
                objectName,
                cancellationToken);

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
                CreatedAt = DateTime.UtcNow
            };

            await _exportedFileRepository.AddAsync(exportedFile, cancellationToken);
            await _exportedFileRepository.SaveChangesAsync(cancellationToken);

            return MapToResponse(exportedFile);
        }
        finally
        {
            fileStream?.Dispose();
        }
    }

    private async Task<(Stream, string, string)> GenerateFileAsync(
        ExportDataRequest request,
        CancellationToken cancellationToken)
    {
        return request.Format switch
        {
            // 🔥 Excel từ template
            ExportFormat.Excel =>
            (
                GenerateExcelFromTemplate(request),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                ".xlsx"
            ),

            // 🔥 Word từ template (MiniWord) hoặc fallback generator cũ
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

    // =============================
    // 🔥 EXCEL từ template
    private Stream GenerateExcelFromTemplate(ExportDataRequest request)
    {
        var templatePath = Path.Combine(
            _templateBasePath, "Excel", $"{request.TemplateName}.xlsx");

        if (!File.Exists(templatePath))
            throw new FileNotFoundException($"Excel template not found: {templatePath}");

        return _excelTemplateEngine.FillTemplate(
            templatePath,
            request.RawData,
            request.SheetName);
    }

    // =============================
    // 🔥 WORD từ template (MiniWord)
    private Stream GenerateWordFromTemplate(ExportDataRequest request)
    {
        // Nếu có WordTemplateName → dùng MiniWord template engine
        if (!string.IsNullOrWhiteSpace(request.WordTemplateName))
        {
            var templatePath = Path.Combine(
                _templateBasePath, "Word", $"{request.WordTemplateName}.docx");

            return _wordTemplateEngine.FillTemplate(templatePath, request.RawData);
        }

        // Fallback: dùng generator cũ nếu không có template
        throw new ArgumentException(
            "WordTemplateName is required for Word export. " +
            "Please provide a template name.");
    }

    // =============================
    // Các method còn lại giữ nguyên
    public async Task<ExportFileResponse?> GetExportFileAsync(
        Guid exportId,
        CancellationToken cancellationToken = default)
    {
        var exportedFile = await _exportedFileRepository.GetByIdAsync(exportId, cancellationToken);
        return exportedFile != null ? MapToResponse(exportedFile) : null;
    }

    public async Task<IEnumerable<ExportFileResponse>> GetUserExportFilesAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var exportedFiles = await _exportedFileRepository.GetByUserIdAsync(userId, cancellationToken);
        return exportedFiles.Select(MapToResponse);
    }

    public async Task DeleteExportFileAsync(
        Guid exportId,
        CancellationToken cancellationToken = default)
    {
        var exportedFile = await _exportedFileRepository.GetByIdAsync(exportId, cancellationToken);
        if (exportedFile == null)
            throw new KeyNotFoundException($"Export file with id {exportId} not found.");

        await _fileStorageService.DeleteFileAsync(
            exportedFile.Bucket,
            exportedFile.ObjectName,
            cancellationToken);

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
                sourceStream, sourceFileName, cancellationToken);
        }
        finally
        {
            sourceStream.Dispose();
        }

        // Upload MinIO
        try
        {
            var objectName = GenerateObjectName(request.FileName, ".pdf");

            var fileUrl = await _fileStorageService.UploadFileAsync(
                _minIOBucket, objectName, pdfStream,
                "application/pdf", cancellationToken);

            var fileSize = await _fileStorageService.GetFileSizeAsync(
                _minIOBucket, objectName, cancellationToken);

            // Presigned URL 1 giờ để UI truy cập được
            var previewUrl = await _fileStorageService.GetPresignedUrlAsync(
                _minIOBucket, objectName, 3600, cancellationToken);

            if (request.SaveToHistory)
            {
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
                    CreatedAt = DateTime.UtcNow
                };

                await _exportedFileRepository.AddAsync(exportedFile, cancellationToken);
                await _exportedFileRepository.SaveChangesAsync(cancellationToken);

                var mapped = MapToResponse(exportedFile);
                mapped.Url = previewUrl; // ưu tiên presigned URL cho UI
                return mapped;
            }

            return new ExportFileResponse
            {
                Id = Guid.Empty,
                FileName = $"{request.FileName}.pdf",
                Url = previewUrl,
                Size = fileSize,
                FileType = "PDF",
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = request.ExpiresAt,
                Bucket = _minIOBucket
            };
        }
        finally
        {
            pdfStream?.Dispose();
        }
    }

}