namespace CleanArchitecture.Application.Export.Messaging;

/// <summary>
/// Kafka message payload - chỉ chứa JobId, rawData đã lưu DB.
/// </summary>
public class ExportJobMessage
{
    public Guid JobId { get; set; }
    public string JobType { get; set; } = null!;  // "Export" | "Preview"
}