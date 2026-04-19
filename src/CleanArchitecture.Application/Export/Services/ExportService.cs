using CleanArchitecture.Application.Common.Interfaces;
using CleanArchitecture.Application.Export.DTOs;
using CleanArchitecture.Application.Export.Interfaces;
using CleanArchitecture.Domain.Entities;
using CleanArchitecture.Domain.Interfaces.Export;

namespace CleanArchitecture.Application.Export.Services;

/// <summary>
/// Service for orchestrating data export to files and storage.
/// Implements Clean Architecture principles with dependency injection.
/// </summary>
public class ExportService : IExportService
{
    private readonly IExportedFileRepository _exportedFileRepository;
    private readonly IFileStorageService _fileStorageService;
    private readonly IExcelFileGenerator _excelGenerator;
    private readonly IWordFileGenerator _wordGenerator;
    private readonly IPdfFileGenerator _pdfGenerator;
    private readonly ICurrentUserService _currentUserService;
    private readonly string _minIOBucket;

    public ExportService(
        IExportedFileRepository exportedFileRepository,
        IFileStorageService fileStorageService,
        IExcelFileGenerator excelGenerator,
        IWordFileGenerator wordGenerator,
        IPdfFileGenerator pdfGenerator,
        ICurrentUserService currentUserService,
        string minIOBucket = "exports")
    {
        _exportedFileRepository = exportedFileRepository;
        _fileStorageService = fileStorageService;
        _excelGenerator = excelGenerator;
        _wordGenerator = wordGenerator;
        _pdfGenerator = pdfGenerator;
        _currentUserService = currentUserService;
        _minIOBucket = minIOBucket;
    }

    public async Task<ExportFileResponse> ExportDataAsync(
        ExportDataRequest request,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        // Ensure bucket exists
        await _fileStorageService.EnsureBucketExistsAsync(_minIOBucket, cancellationToken);

        // Generate file based on format
        var (fileStream, contentType, fileExtension) = await GenerateFileAsync(request, cancellationToken);

        try
        {
            // Generate unique object name
            var objectName = GenerateObjectName(request.FileName, fileExtension);

            // Upload to MinIO
            var fileUrl = await _fileStorageService.UploadFileAsync(
                _minIOBucket,
                objectName,
                fileStream,
                contentType,
                cancellationToken);

            // Get file size
            var fileSize = await _fileStorageService.GetFileSizeAsync(
                _minIOBucket,
                objectName,
                cancellationToken);

            // Create database record
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

            // Save to database
            await _exportedFileRepository.AddAsync(exportedFile, cancellationToken);
            await _exportedFileRepository.SaveChangesAsync(cancellationToken);

            return MapToResponse(exportedFile);
        }
        finally
        {
            fileStream?.Dispose();
        }
    }

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

        // Delete from MinIO
        await _fileStorageService.DeleteFileAsync(
            exportedFile.Bucket,
            exportedFile.ObjectName,
            cancellationToken);

        // Delete from database
        await _exportedFileRepository.DeleteAsync(exportId, cancellationToken);
        await _exportedFileRepository.SaveChangesAsync(cancellationToken);
    }

    private async Task<(Stream, string, string)> GenerateFileAsync(
        ExportDataRequest request,
        CancellationToken cancellationToken)
    {
        return request.Format switch
        {
            ExportFormat.Excel => 
                (await _excelGenerator.GenerateAsync(request.FileName, ConvertToObjects(request.Data), cancellationToken: cancellationToken), 
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", 
                ".xlsx"),

            ExportFormat.Word => 
                (await _wordGenerator.GenerateAsync(request.FileName, request.FileName, FormatDataAsHtml(request.Data), cancellationToken), 
                "application/vnd.openxmlformats-officedocument.wordprocessingml.document", 
                ".docx"),

            ExportFormat.PDF => 
                (await _pdfGenerator.GenerateFromHtmlAsync(request.FileName, FormatDataAsHtml(request.Data), cancellationToken), 
                "application/pdf", 
                ".pdf"),

            _ => throw new InvalidOperationException($"Unsupported export format: {request.Format}")
        };
    }

    private IEnumerable<dynamic> ConvertToObjects(IEnumerable<Dictionary<string, object?>>? data)
    {
        if (data == null)
            return Enumerable.Empty<dynamic>();

        return data.Select(d => (dynamic)d).ToList();
    }

    private string FormatDataAsHtml(IEnumerable<Dictionary<string, object?>>? data)
    {
        if (data == null || !data.Any())
            return "<p>No data to display</p>";

        var html = new System.Text.StringBuilder();
        html.AppendLine("<table border='1' cellpadding='5' cellspacing='0' style='border-collapse: collapse;'>");

        // Header row
        var firstRow = data.First();
        html.AppendLine("<thead><tr>");
        foreach (var key in firstRow.Keys)
        {
            html.AppendLine($"<th>{System.Net.WebUtility.HtmlEncode(key.ToString())}</th>");
        }
        html.AppendLine("</tr></thead>");

        // Data rows
        html.AppendLine("<tbody>");
        foreach (var row in data)
        {
            html.AppendLine("<tr>");
            foreach (var value in row.Values)
            {
                html.AppendLine($"<td>{System.Net.WebUtility.HtmlEncode(value?.ToString() ?? "")}</td>");
            }
            html.AppendLine("</tr>");
        }
        html.AppendLine("</tbody>");
        html.AppendLine("</table>");

        return html.ToString();
    }

    private string GenerateObjectName(string fileName, string extension)
    {
        var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
        var sanitizedName = System.Text.RegularExpressions.Regex.Replace(fileName, @"[^a-zA-Z0-9._-]", "_");
        return $"{sanitizedName}_{timestamp}{extension}";
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
}
