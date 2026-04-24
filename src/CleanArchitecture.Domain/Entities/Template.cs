// CleanArchitecture.Domain/Entities/Template.cs
using CleanArchitecture.Domain.Common;

namespace CleanArchitecture.Domain.Entities;

public class Template : BaseEntity
{
    public string Name { get; set; } = null!;
    public string Type { get; set; } = null!;          // "Word" | "Excel"
    public string? Description { get; set; }
    public string Bucket { get; set; } = null!;
    public string ObjectName { get; set; } = null!;
    public string FileName { get; set; } = null!;
    public long Size { get; set; }
    public int Version { get; set; } = 1;
    public string? Category { get; set; }
    public Guid UploadedBy { get; set; }
}