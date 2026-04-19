using CleanArchitecture.Domain.Interfaces.Export;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace CleanArchitecture.Infrastructure.FileGeneration;

/// <summary>
/// Word file generator using OpenXML SDK.
/// Generates .docx files from HTML or plain text content.
/// </summary>
public class WordFileGenerator : IWordFileGenerator
{
    public async Task<Stream> GenerateAsync(
        string fileName,
        string title,
        string content,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(fileName);
        ArgumentException.ThrowIfNullOrEmpty(title);

        return await Task.Run(() =>
        {
            var stream = new MemoryStream();

            using (var wordDoc = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document, true))
            {
                // Create main document part
                var mainPart = wordDoc.AddMainDocumentPart();
                mainPart.Document = new Document(new Body());

                var body = mainPart.Document.Body ?? new Body();

                // Add title
                var titleParagraph = new Paragraph(
                    new ParagraphProperties(
                        new ParagraphStyleId { Val = "Heading1" }),
                    new Run(
                        new RunProperties(
                            new Bold(),
                            new FontSize { Val = "28" }),
                        new Text(title)));
                body.AppendChild(titleParagraph);

                // Add content (treat as HTML and parse)
                AddContentToBody(body, content);

                mainPart.Document.Body = body;
            }

            stream.Position = 0;
            return stream;
        }, cancellationToken);
    }

    private void AddContentToBody(Body body, string content)
    {
        if (content.Contains("<table", StringComparison.OrdinalIgnoreCase))
        {
            // Parse HTML table
            var table = ParseHtmlTable(content);
            if (table != null)
                body.AppendChild(table);
        }
        else
        {
            // Add as text paragraphs
            var lines = content.Split(new[] { "<br>", "<br/>", "<br />" }, StringSplitOptions.None);
            foreach (var line in lines)
            {
                var cleanText = System.Net.WebUtility.HtmlDecode(System.Text.RegularExpressions.Regex.Replace(line, "<[^>]+>", ""));
                if (!string.IsNullOrWhiteSpace(cleanText))
                {
                    var paragraph = new Paragraph(new Run(new Text(cleanText)));
                    body.AppendChild(paragraph);
                }
            }
        }
    }

    private Table? ParseHtmlTable(string htmlContent)
    {
        var table = new Table();
        var tableProperties = new TableProperties(
            new TableBorders(
                new TopBorder { Val = BorderValues.Single, Size = 12 },
                new BottomBorder { Val = BorderValues.Single, Size = 12 },
                new LeftBorder { Val = BorderValues.Single, Size = 12 },
                new RightBorder { Val = BorderValues.Single, Size = 12 },
                new InsideHorizontalBorder { Val = BorderValues.Single, Size = 12 },
                new InsideVerticalBorder { Val = BorderValues.Single, Size = 12 }),
            new TableWidth { Width = "9000", Type = TableWidthUnitValues.Auto });

        table.AppendChild(tableProperties);

        // Extract table rows from HTML
        var rows = System.Text.RegularExpressions.Regex.Matches(htmlContent, @"<tr[^>]*>(.*?)</tr>", System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.Singleline);

        foreach (System.Text.RegularExpressions.Match rowMatch in rows)
        {
            var row = new TableRow();
            var cells = System.Text.RegularExpressions.Regex.Matches(rowMatch.Groups[1].Value, @"<t[dh][^>]*>(.*?)</t[dh]>", System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.Singleline);

            foreach (System.Text.RegularExpressions.Match cellMatch in cells)
            {
                var cellText = System.Net.WebUtility.HtmlDecode(System.Text.RegularExpressions.Regex.Replace(cellMatch.Groups[1].Value, "<[^>]+>", ""));
                var cell = new TableCell(new Paragraph(new Run(new Text(cellText.Trim()))));
                row.AppendChild(cell);
            }

            if (row.Elements<TableCell>().Any())
                table.AppendChild(row);
        }

        return table.Elements<TableRow>().Any() ? table : null;
    }
}