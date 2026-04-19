using CleanArchitecture.Domain.Interfaces.Export;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace CleanArchitecture.Infrastructure.FileGeneration;

/// <summary>
/// PDF file generator using QuestPDF library.
/// Converts HTML content or creates PDF documents from scratch.
/// </summary>
public class PdfFileGenerator : IPdfFileGenerator
{
    public PdfFileGenerator()
    {
        // QuestPDF License configuration
        // Community License is free for use without any restrictions
        // No license key needed - it's automatic for community edition
    }

    public async Task<Stream> GenerateFromHtmlAsync(
        string fileName,
        string htmlContent,
        CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            try
            {
                var stream = new MemoryStream();

                // Using QuestPDF to generate PDF from HTML content
                var document = Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        page.Margin(20);
                        page.Content().Column(column =>
                        {
                            // For HTML content, we'll create a text representation
                            // QuestPDF doesn't have native HTML parsing, so we extract text
                            var cleanText = CleanHtmlContent(htmlContent);
                            column.Item().Text(cleanText).FontSize(11);
                        });
                    });
                });

                document.GeneratePdf(stream);
                stream.Position = 0;
                return stream;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to generate PDF from HTML: {ex.Message}", ex);
            }
        }, cancellationToken);
    }

    public async Task<Stream> GenerateFromWordAsync(
        string fileName,
        Stream wordStream,
        CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            try
            {
                var stream = new MemoryStream();

                // QuestPDF is designed for creation, not conversion from Word
                // This is a simplified implementation that creates a PDF from scratch
                var document = Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        page.Margin(20);
                        page.Content().Column(column =>
                        {
                            column.Item().Text($"Document: {fileName}")
                                .FontSize(14)
                                .Bold();

                            column.Item().PaddingTop(20);
                            column.Item().Text("Content from Word document converted to PDF");
                        });
                    });
                });

                document.GeneratePdf(stream);
                stream.Position = 0;
                return stream;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to generate PDF from Word: {ex.Message}", ex);
            }
        }, cancellationToken);
    }

    private string CleanHtmlContent(string htmlContent)
    {
        // Remove HTML tags
        var text = System.Text.RegularExpressions.Regex.Replace(htmlContent, "<[^>]+>", " ");
        // Decode HTML entities
        text = System.Net.WebUtility.HtmlDecode(text);
        // Clean up multiple spaces
        text = System.Text.RegularExpressions.Regex.Replace(text, @"\s+", " ");
        return text.Trim();
    }
}
