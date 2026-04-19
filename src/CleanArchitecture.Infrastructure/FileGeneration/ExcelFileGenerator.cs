using CleanArchitecture.Domain.Interfaces.Export;
using ClosedXML.Excel;

namespace CleanArchitecture.Infrastructure.FileGeneration;

/// <summary>
/// Excel file generator using ClosedXML library.
/// Generates .xlsx files from generic data collections.
/// </summary>
public class ExcelFileGenerator : IExcelFileGenerator
{
    public async Task<Stream> GenerateAsync<T>(
        string fileName,
        IEnumerable<T> data,
        string sheetName = "Data",
        CancellationToken cancellationToken = default) where T : class
    {
        return await Task.Run(() =>
        {
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add(sheetName);

            var dataList = data.ToList();
            if (!dataList.Any())
            {
                var stream = new MemoryStream();
                workbook.SaveAs(stream);
                stream.Position = 0;
                return stream;
            }

            // Get properties from first object
            var properties = typeof(T).GetProperties();

            // Write headers
            for (int i = 0; i < properties.Length; i++)
            {
                var header = properties[i].Name;
                worksheet.Cell(1, i + 1).Value = header;
                worksheet.Cell(1, i + 1).Style.Font.Bold = true;
                worksheet.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.LightGray;
            }

            // Write data rows
            for (int row = 0; row < dataList.Count; row++)
            {
                var item = dataList[row];
                for (int col = 0; col < properties.Length; col++)
                {
                    var value = properties[col].GetValue(item);
                    worksheet.Cell(row + 2, col + 1).Value = value?.ToString() ?? string.Empty;
                }
            }

            // Auto-fit columns
            worksheet.Columns().AdjustToContents();

            // Save to stream
            var outputStream = new MemoryStream();
            workbook.SaveAs(outputStream);
            outputStream.Position = 0;
            return outputStream;
        }, cancellationToken);
    }
}
