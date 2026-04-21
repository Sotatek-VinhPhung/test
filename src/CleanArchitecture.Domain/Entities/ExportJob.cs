using CleanArchitecture.Domain.Common;

namespace CleanArchitecture.Domain.Entities;

/// <summary>
/// Đại diện cho một job export file (Excel, Word, PDF).
/// Job được đưa vào queue và xử lý bởi background worker.
/// </summary>
public class ExportJob : BaseEntity
{
    /// <summary>
    /// Tên file xuất ra.
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// Định dạng export: Excel, Word, PDF.
    /// </summary>
    public string Format { get; set; } = string.Empty;

    /// <summary>
    /// Trạng thái job: Pending, Processing, Completed, Failed.
    /// </summary>
    public string Status { get; set; } = "Pending";

    /// <summary>
    /// Tiến độ xử lý (0-100%).
    /// </summary>
    public int Progress { get; set; }

    /// <summary>
    /// URL download file sau khi xử lý xong.
    /// </summary>
    public string? FileUrl { get; set; }

    /// <summary>
    /// Bucket MinIO nơi lưu file.
    /// </summary>
    public string? Bucket { get; set; }

    /// <summary>
    /// Tên object trong MinIO.
    /// </summary>
    public string? ObjectName { get; set; }

    /// <summary>
    /// Dung lượng file (bytes).
    /// </summary>
    public long FileSize { get; set; }

    /// <summary>
    /// Thông báo lỗi nếu xử lý thất bại.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Dữ liệu cần export (JSON format).
    /// </summary>
    public string? Payload { get; set; }

    /// <summary>
    /// User yêu cầu export.
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Ghi chú/lý do export.
    /// </summary>
    public string? Note { get; set; }

    /// <summary>
    /// Thời gian bắt đầu xử lý.
    /// </summary>
    public DateTime? StartedAt { get; set; }

    /// <summary>
    /// Thời gian hoàn thành.
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// Thời gian hết hạn file.
    /// </summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>
    /// Số lần retry khi job fail.
    /// </summary>
    public int RetryCount { get; set; }

    /// <summary>
    /// Max retry attempts.
    /// </summary>
    public int MaxRetries { get; set; } = 3;
}
