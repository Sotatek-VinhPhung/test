using CleanArchitecture.Domain.Common;

namespace CleanArchitecture.Domain.Entities;

/// <summary>
/// Represents an exported file with metadata stored in database.
/// </summary>
public class ExportedFile : BaseEntity
{
    /// <summary>
    /// Name of the exported file.
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// URL to download the file.
    /// </summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// MinIO bucket name where file is stored.
    /// </summary>
    public string Bucket { get; set; } = string.Empty;

    /// <summary>
    /// File size in bytes.
    /// </summary>
    public long Size { get; set; }

    /// <summary>
    /// File type/format (Excel, Word, PDF).
    /// </summary>
    public string FileType { get; set; } = string.Empty;

    /// <summary>
    /// Original object name in MinIO.
    /// </summary>
    public string ObjectName { get; set; } = string.Empty;

    /// <summary>
    /// User who requested the export.
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Export reason or note.
    /// </summary>
    public string? Note { get; set; }

    /// <summary>
    /// Export expiration date (optional).
    /// </summary>
    public DateTime? ExpiresAt { get; set; }
    public string? CacheKey { get; set; }
    public int? TemplateVersion { get; set; }
}
