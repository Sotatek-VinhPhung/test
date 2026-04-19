namespace CleanArchitecture.Application.Export.DTOs;

/// <summary>
/// Response containing exported file metadata.
/// </summary>
public class ExportFileResponse
{
    /// <summary>
    /// Unique identifier for the export record.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// File name.
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// URL to download the file.
    /// </summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// File size in bytes.
    /// </summary>
    public long Size { get; set; }

    /// <summary>
    /// File type/format.
    /// </summary>
    public string FileType { get; set; } = string.Empty;

    /// <summary>
    /// Creation timestamp.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Expiration date of the file (if applicable).
    /// </summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>
    /// MinIO bucket name.
    /// </summary>
    public string Bucket { get; set; } = string.Empty;
}
