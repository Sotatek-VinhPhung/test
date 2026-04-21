namespace CleanArchitecture.Application.Export.DTOs;

public class PreviewPdfRequest
{
    public string FileName { get; set; } = "preview";
    public PreviewSourceFormat SourceFormat { get; set; }
    public string? WordTemplateName { get; set; }
    public string? ExcelTemplateName { get; set; }
    public string? SheetName { get; set; }
    public Dictionary<string, object?>? RawData { get; set; }
    public bool SaveToHistory { get; set; } = false;
    public DateTime? ExpiresAt { get; set; }
    public string? Note { get; set; }
}

public enum PreviewSourceFormat
{
    Word = 1,
    Excel = 2
}