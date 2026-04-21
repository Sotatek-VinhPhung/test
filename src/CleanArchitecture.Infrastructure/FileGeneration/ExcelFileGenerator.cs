using CleanArchitecture.Domain.Interfaces.Export;

using ClosedXML.Excel;

namespace CleanArchitecture.Infrastructure.FileGeneration;

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

            var normalized = Normalize(data);

            if (!normalized.Any())
            {
                var emptyStream = new MemoryStream();
                workbook.SaveAs(emptyStream);
                emptyStream.Position = 0;
                return (Stream)emptyStream;
            }

            var headers = normalized.First().Keys.ToList();

            // 🔥 Header
            for (int col = 0; col < headers.Count; col++)
            {
                var cell = worksheet.Cell(1, col + 1);
                cell.Value = headers[col];
                cell.Style.Font.Bold = true;
                cell.Style.Fill.BackgroundColor = XLColor.LightGray;
            }

            // 🔥 Data
            for (int row = 0; row < normalized.Count; row++)
            {
                var rowData = normalized[row];

                for (int col = 0; col < headers.Count; col++)
                {
                    var value = rowData.ContainsKey(headers[col])
                        ? rowData[headers[col]]
                        : null;

                    var cell = worksheet.Cell(row + 2, col + 1);

                    if (value is DateTime dt)
                    {
                        cell.Value = dt;
                        cell.Style.DateFormat.Format = "yyyy-MM-dd HH:mm:ss";
                    }
                    else
                    {
                        cell.SetValue(value?.ToString() ?? "");
                    }
                }
            }

            worksheet.Columns().AdjustToContents();

            var stream = new MemoryStream();
            workbook.SaveAs(stream);

            // 🔥 QUAN TRỌNG
            stream.Position = 0;

            return (Stream)stream;
        }, cancellationToken);
    }

    // ================================
    // 🔥 Normalize tất cả về Dictionary
    // ================================
    private List<Dictionary<string, object?>> Normalize<T>(IEnumerable<T> data)
    {
        var result = new List<Dictionary<string, object?>>();

        foreach (var item in data)
        {
            if (item == null)
                continue;

            // 👉 Nếu đã là Dictionary
            if (item is Dictionary<string, object?> dict)
            {
                result.Add(dict);
                continue;
            }

            // 👉 Nếu là object (DTO)
            var row = new Dictionary<string, object?>();

            var props = item.GetType().GetProperties();
            foreach (var prop in props)
            {
                row[prop.Name] = prop.GetValue(item);
            }

            result.Add(row);
        }

        return result;
    }


    private object FormatValue(object? value)
    {
        if (value == null)
            return "";

        return value switch
        {
            DateTime dt => dt,
            bool b => b,
            _ => value
        };
    }
}