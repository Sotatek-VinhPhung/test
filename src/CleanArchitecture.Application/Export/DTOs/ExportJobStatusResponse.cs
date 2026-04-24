namespace CleanArchitecture.Application.Export.DTOs;

public class ExportJobStatusResponse
{
    public Guid JobId { get; set; }
    public string JobType { get; set; } = null!;
    public string Status { get; set; } = null!;
    public string? ResultUrl { get; set; }
    public string? ResultFileName { get; set; }
    public Guid? ResultFileId { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}