namespace CleanArchitecture.Application.Export.DTOs;

/// <summary>
/// Request to export data in specified format.
/// </summary>
public class ExportDataRequest
{
    /// <summary>
    /// Export format: Excel, Word, or PDF.
    /// </summary>
    public ExportFormat Format { get; set; }

    /// <summary>
    /// File name for export (without extension).
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// Generic data to be exported.
    /// </summary>
    public IEnumerable<Dictionary<string, object?>>? Data { get; set; }

    /// <summary>
    /// Optional note or reason for export.
    /// </summary>
    public string? Note { get; set; }

    /// <summary>
    /// Expiration date for the exported file (optional).
    /// </summary>
    public DateTime? ExpiresAt { get; set; }
}

/// <summary>
/// Export format enumeration.
/// </summary>
public enum ExportFormat
{
    Excel = 0,
    Word = 1,
    PDF = 2
}
