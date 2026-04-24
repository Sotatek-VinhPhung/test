namespace CleanArchitecture.Domain.Enums;

public static class ExportJobStatus
{
    public const string Pending = "Pending";
    public const string Processing = "Processing";
    public const string Completed = "Completed";
    public const string Failed = "Failed";
}

public static class ExportJobType
{
    public const string Export = "Export";
    public const string Preview = "Preview";
}