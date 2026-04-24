namespace CleanArchitecture.Application.Export.DTOs;

public class UploadTemplateRequest
{
    public string Name { get; set; } = null!;
    public string Type { get; set; } = null!;
    public string? Description { get; set; }
    public string? Category { get; set; }
    public Stream FileStream { get; set; } = null!;
    public string FileName { get; set; } = null!;
}

public class TemplateDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public string Type { get; set; } = null!;
    public string? Description { get; set; }
    public string? Category { get; set; }
    public string FileName { get; set; } = null!;
    public long Size { get; set; }
    public int Version { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}