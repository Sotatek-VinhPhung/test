using CleanArchitecture.Domain.Common;

namespace CleanArchitecture.Domain.Entities;

public class ExportJob : BaseEntity
{
    public Guid UserId { get; set; }
    public string JobType { get; set; } = null!;          // "Export" | "Preview"
    public string Status { get; set; } = "Pending";       // Pending | Processing | Completed | Failed
    public string RequestJson { get; set; } = null!;
    public Guid? ResultFileId { get; set; }
    public string? ResultUrl { get; set; }
    public string? ResultFileName { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}