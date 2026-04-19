namespace CleanArchitecture.Domain.Interfaces.Export;

/// <summary>
/// Interface for generating Excel files.
/// </summary>
public interface IExcelFileGenerator
{
    /// <summary>
    /// Generate Excel file from data.
    /// </summary>
    /// <param name="fileName">Output file name</param>
    /// <param name="data">Generic data to be exported</param>
    /// <param name="sheetName">Excel sheet name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Stream with Excel file content</returns>
    Task<Stream> GenerateAsync<T>(
        string fileName,
        IEnumerable<T> data,
        string sheetName = "Data",
        CancellationToken cancellationToken = default) where T : class;
}

/// <summary>
/// Interface for generating Word documents.
/// </summary>
public interface IWordFileGenerator
{
    /// <summary>
    /// Generate Word document from data.
    /// </summary>
    /// <param name="fileName">Output file name</param>
    /// <param name="title">Document title</param>
    /// <param name="content">Document content</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Stream with Word document content</returns>
    Task<Stream> GenerateAsync(
        string fileName,
        string title,
        string content,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Interface for generating PDF files.
/// </summary>
public interface IPdfFileGenerator
{
    /// <summary>
    /// Generate PDF from HTML content.
    /// </summary>
    /// <param name="fileName">Output file name</param>
    /// <param name="htmlContent">HTML content to convert</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Stream with PDF file content</returns>
    Task<Stream> GenerateFromHtmlAsync(
        string fileName,
        string htmlContent,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generate PDF from Word document stream.
    /// </summary>
    /// <param name="fileName">Output file name</param>
    /// <param name="wordStream">Word document stream</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Stream with PDF file content</returns>
    Task<Stream> GenerateFromWordAsync(
        string fileName,
        Stream wordStream,
        CancellationToken cancellationToken = default);
}
