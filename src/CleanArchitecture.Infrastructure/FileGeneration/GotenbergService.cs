using System.Net.Http.Headers;
using CleanArchitecture.Domain.Interfaces.Export;
using Microsoft.Extensions.Logging;

namespace CleanArchitecture.Infrastructure.FileGeneration;

public class GotenbergService : IGotenbergService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<GotenbergService> _logger;

    public GotenbergService(HttpClient httpClient, ILogger<GotenbergService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<Stream> ConvertOfficeToPdfAsync(
        Stream officeFileStream,
        string fileName,
        CancellationToken cancellationToken = default)
    {
        if (officeFileStream == null)
            throw new ArgumentNullException(nameof(officeFileStream));
        if (string.IsNullOrWhiteSpace(fileName))
            throw new ArgumentException("FileName is required", nameof(fileName));

        const string route = "forms/libreoffice/convert";

        using var content = new MultipartFormDataContent();

        if (officeFileStream.CanSeek)
            officeFileStream.Position = 0;

        var fileContent = new StreamContent(officeFileStream);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(GetContentType(fileName));
        content.Add(fileContent, "files", fileName);

        _logger.LogInformation("Sending file {FileName} to Gotenberg for PDF conversion", fileName);

        using var response = await _httpClient.PostAsync(route, content, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("Gotenberg conversion failed. Status: {Status}, Body: {Body}",
                response.StatusCode, error);
            throw new InvalidOperationException(
                $"Gotenberg conversion failed ({(int)response.StatusCode}): {error}");
        }

        var pdfStream = new MemoryStream();
        await response.Content.CopyToAsync(pdfStream, cancellationToken);
        pdfStream.Position = 0;

        _logger.LogInformation("Successfully converted {FileName} to PDF ({Size} bytes)",
            fileName, pdfStream.Length);

        return pdfStream;
    }

    private static string GetContentType(string fileName)
    {
        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        return ext switch
        {
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            ".doc" => "application/msword",
            ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            ".xls" => "application/vnd.ms-excel",
            ".pptx" => "application/vnd.openxmlformats-officedocument.presentationml.presentation",
            ".ppt" => "application/vnd.ms-powerpoint",
            _ => "application/octet-stream"
        };
    }
}