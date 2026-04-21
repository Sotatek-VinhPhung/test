using CleanArchitecture.Domain.Interfaces.Export;
using ClosedXML.Excel;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace CleanArchitecture.Infrastructure.FileGeneration;

public class ExcelTemplateEngine : IExcelTemplateEngine
{
    private static readonly Regex PlaceholderPattern =
        new(@"\{\{([^}]+)\}\}", RegexOptions.Compiled);

    public Stream FillTemplate(
        string templatePath,
        Dictionary<string, object?>? data,
        string? sheetName = null)
    {
        if (!File.Exists(templatePath))
            throw new FileNotFoundException($"Template not found: {templatePath}");

        if (data == null || !data.Any())
            throw new ArgumentException("Data is empty");

        using var workbook = new XLWorkbook(templatePath);

        var sheets = sheetName != null
            ? workbook.Worksheets.Where(x => x.Name == sheetName)
            : workbook.Worksheets;

        foreach (var ws in sheets)
        {
            ProcessWorksheet(ws, data);
        }

        var stream = new MemoryStream();
        workbook.SaveAs(stream);
        stream.Position = 0;

        return stream;
    }

    private void ProcessWorksheet(IXLWorksheet ws, Dictionary<string, object?> data)
    {
        var tableRows = FindTableRows(ws, data);

        // 🔥 Fill table TRƯỚC (từ dưới lên để tránh lệch row number)
        foreach (var row in tableRows.OrderByDescending(x => x.Row))
        {
            FillTable(ws, row, data);
        }

        // Fill field thường SAU
        FillSingleFields(ws, data);
    }

    // =============================
    // 🔥 FIELD THƯỜNG
    private void FillSingleFields(IXLWorksheet ws, Dictionary<string, object?> data)
    {
        foreach (var row in ws.RowsUsed())
        {
            foreach (var cell in row.CellsUsed())
            {
                var text = cell.GetString();
                if (!text.Contains("{{")) continue;

                if (IsOnlyPlaceholder(text, out var key))
                {
                    if (key.Contains(".")) continue;
                    var value = ResolveValue(data, key);
                    cell.Value = value?.ToString() ?? "";
                }
                else
                {
                    cell.Value = ReplacePlaceholders(text, data);
                }
            }
        }
    }

    // =============================
    // 🔥 TABLE — FIX CHÍNH
    private void FillTable(
        IXLWorksheet ws,
        TableRow table,
        Dictionary<string, object?> data)
    {
        var items = ResolveArray(data, table.ArrayName);
        var templateRowNumber = table.Row;

        // 🔥 1. Snapshot toàn bộ thông tin cần từ template row TRƯỚC KHI insert
        var templateSnapshot = SnapshotRow(ws, templateRowNumber);

        // 🔥 2. Snapshot merge ranges liên quan đến template row
        var templateMerges = GetMergesForRow(ws, templateRowNumber);

        if (!items.Any())
        {
            ws.Row(templateRowNumber).Delete();
            return;
        }

        // 🔥 3. Insert thêm rows nếu cần (TRƯỚC khi fill để tránh lệch)
        if (items.Count > 1)
        {
            ws.Row(templateRowNumber).InsertRowsBelow(items.Count - 1);
        }

        // 🔥 4. Fill từng row
        for (int i = 0; i < items.Count; i++)
        {
            var rowNumber = templateRowNumber + i;

            // 4a. Restore style + value từ snapshot vào row mới
            RestoreRowFromSnapshot(ws, rowNumber, templateSnapshot);

            // 4b. Apply merge cells từ template (QUAN TRỌNG)
            ApplyMergesForRow(ws, templateMerges, templateRowNumber, rowNumber);

            // 4c. Fill data vào
            FillRowFromSnapshot(ws, rowNumber, table.ArrayName, items[i], templateSnapshot);
        }
    }

    // =============================
    // 🔥 SNAPSHOT — lưu toàn bộ cell info từ template row
    private List<CellSnapshot> SnapshotRow(IXLWorksheet ws, int rowNumber)
    {
        var snapshots = new List<CellSnapshot>();
        var lastCol = ws.LastColumnUsed()?.ColumnNumber() ?? 1;

        for (int col = 1; col <= lastCol; col++)
        {
            var cell = ws.Cell(rowNumber, col);
            snapshots.Add(new CellSnapshot
            {
                Column = col,
                Value = cell.GetString(),
                // 🔥 Clone style bằng cách lưu từng property riêng lẻ
                FontBold = cell.Style.Font.Bold,
                FontSize = cell.Style.Font.FontSize,
                FontName = cell.Style.Font.FontName,
                FontColor = cell.Style.Font.FontColor,
                HAlign = cell.Style.Alignment.Horizontal,
                VAlign = cell.Style.Alignment.Vertical,
                WrapText = cell.Style.Alignment.WrapText,
                FillColor = cell.Style.Fill.BackgroundColor,
                BorderTop = cell.Style.Border.TopBorder,
                BorderBottom = cell.Style.Border.BottomBorder,
                BorderLeft = cell.Style.Border.LeftBorder,
                BorderRight = cell.Style.Border.RightBorder,
                BorderTopColor = cell.Style.Border.TopBorderColor,
                BorderBottomColor = cell.Style.Border.BottomBorderColor,
                BorderLeftColor = cell.Style.Border.LeftBorderColor,
                BorderRightColor = cell.Style.Border.RightBorderColor,
            });
        }

        return snapshots;
    }

    // 🔥 Restore style từ snapshot vào row — KHÔNG dùng cell.Style = ... để tránh mất border
    private void RestoreRowFromSnapshot(IXLWorksheet ws, int rowNumber, List<CellSnapshot> snapshots)
    {
        foreach (var snap in snapshots)
        {
            var cell = ws.Cell(rowNumber, snap.Column);

            // Apply từng property riêng lẻ
            cell.Style.Font.Bold = snap.FontBold;
            cell.Style.Font.FontSize = snap.FontSize;
            cell.Style.Font.FontName = snap.FontName;
            cell.Style.Font.FontColor = snap.FontColor;
            cell.Style.Alignment.Horizontal = snap.HAlign;
            cell.Style.Alignment.Vertical = snap.VAlign;
            cell.Style.Alignment.WrapText = false; // tắt wrap
            cell.Style.Fill.BackgroundColor = snap.FillColor;

            // 🔥 Border phải set riêng lẻ — KHÔNG dùng Style = snap.Style
            cell.Style.Border.TopBorder = snap.BorderTop;
            cell.Style.Border.BottomBorder = snap.BorderBottom;
            cell.Style.Border.LeftBorder = snap.BorderLeft;
            cell.Style.Border.RightBorder = snap.BorderRight;
            cell.Style.Border.TopBorderColor = snap.BorderTopColor;
            cell.Style.Border.BottomBorderColor = snap.BorderBottomColor;
            cell.Style.Border.LeftBorderColor = snap.BorderLeftColor;
            cell.Style.Border.RightBorderColor = snap.BorderRightColor;

            // Restore template text (để FillRow replace sau)
            cell.Value = snap.Value;
        }
    }

    // 🔥 Fill data vào row dựa trên snapshot
    private void FillRowFromSnapshot(
        IXLWorksheet ws,
        int rowNumber,
        string arrayName,
        Dictionary<string, object?> item,
        List<CellSnapshot> snapshots)
    {
        foreach (var snap in snapshots)
        {
            if (!snap.Value.Contains("{{")) continue;

            var cell = ws.Cell(rowNumber, snap.Column);
            var text = snap.Value;

            var matches = PlaceholderPattern.Matches(text);
            foreach (Match m in matches)
            {
                var key = m.Groups[1].Value;
                if (!key.StartsWith(arrayName + ".")) continue;

                var field = key.Substring(arrayName.Length + 1);
                var value = ResolveValue(item, field);
                text = text.Replace(m.Value, value?.ToString() ?? "");
            }

            // Nếu không có placeholder nào match → xóa placeholder còn sót
            text = PlaceholderPattern.Replace(text, "");
            cell.Value = text;
        }
    }

    // =============================
    // 🔥 MERGE CELLS — snapshot và apply
    private List<MergeSnapshot> GetMergesForRow(IXLWorksheet ws, int rowNumber)
    {
        var result = new List<MergeSnapshot>();

        foreach (var mergedRange in ws.MergedRanges)
        {
            var firstRow = mergedRange.FirstRow().RowNumber();
            var lastRow = mergedRange.LastRow().RowNumber();

            // Chỉ lấy merge thuộc đúng template row (single-row merge)
            if (firstRow == rowNumber && lastRow == rowNumber)
            {
                result.Add(new MergeSnapshot
                {
                    FirstCol = mergedRange.FirstColumn().ColumnNumber(),
                    LastCol = mergedRange.LastColumn().ColumnNumber(),
                    RowOffset = 0
                });
            }
        }

        return result;
    }

    private void ApplyMergesForRow(
        IXLWorksheet ws,
        List<MergeSnapshot> templateMerges,
        int templateRowNumber,
        int targetRowNumber)
    {
        if (targetRowNumber == templateRowNumber) return; // template row đã có merge sẵn

        foreach (var merge in templateMerges)
        {
            var range = ws.Range(targetRowNumber, merge.FirstCol, targetRowNumber, merge.LastCol);
            range.Merge();
        }
    }

    // =============================
    private List<TableRow> FindTableRows(IXLWorksheet ws, Dictionary<string, object?> data)
    {
        var result = new List<TableRow>();
        var visited = new HashSet<int>();

        foreach (var row in ws.RowsUsed())
        {
            foreach (var cell in row.CellsUsed())
            {
                var text = cell.GetString();
                if (!text.Contains("{{")) continue;

                var matches = PlaceholderPattern.Matches(text);
                foreach (Match m in matches)
                {
                    var key = m.Groups[1].Value;
                    if (!key.Contains(".")) continue;

                    var arrayName = key.Split('.')[0];
                    if (!data.ContainsKey(arrayName)) continue;

                    var value = data[arrayName];
                    bool isTable =
                        (value is System.Collections.IEnumerable && value is not string)
                        || (value is JsonElement je && je.ValueKind == JsonValueKind.Array);

                    if (isTable && !visited.Contains(row.RowNumber()))
                    {
                        result.Add(new TableRow
                        {
                            Row = row.RowNumber(),
                            ArrayName = arrayName
                        });
                        visited.Add(row.RowNumber());
                    }
                }
            }
        }

        return result;
    }

    // =============================
    private object? ResolveValue(object? current, string key)
    {
        if (current == null) return null;

        var parts = key.Split('.');

        foreach (var part in parts)
        {
            if (current is Dictionary<string, object?> dict)
                current = dict.ContainsKey(part) ? dict[part] : null;
            else if (current is JsonElement je)
            {
                if (je.ValueKind == JsonValueKind.Object && je.TryGetProperty(part, out var prop))
                    current = prop.ValueKind switch
                    {
                        JsonValueKind.String => prop.GetString(),
                        JsonValueKind.Number => prop.GetDecimal(),
                        _ => prop.ToString()
                    };
            }
            else
            {
                var p = current.GetType().GetProperty(part);
                current = p?.GetValue(current);
            }

            if (current == null) return null;
        }

        return current;
    }

    private List<Dictionary<string, object?>> ResolveArray(
        Dictionary<string, object?> data,
        string key)
    {
        if (!data.ContainsKey(key)) return new();

        var value = data[key];

        if (value is JsonElement je && je.ValueKind == JsonValueKind.Array)
        {
            var list = new List<Dictionary<string, object?>>();
            foreach (var item in je.EnumerateArray())
            {
                var dict = new Dictionary<string, object?>();
                foreach (var prop in item.EnumerateObject())
                {
                    dict[prop.Name] = prop.Value.ValueKind switch
                    {
                        JsonValueKind.Number => prop.Value.GetDecimal(),
                        JsonValueKind.String => prop.Value.GetString(),
                        _ => prop.Value.ToString()
                    };
                }
                list.Add(dict);
            }
            return list;
        }

        if (value is System.Collections.IEnumerable enumerable && value is not string)
        {
            var list = new List<Dictionary<string, object?>>();
            foreach (var item in enumerable)
            {
                if (item is Dictionary<string, object?> d)
                    list.Add(d);
            }
            return list;
        }

        return new();
    }

    private string ReplacePlaceholders(string text, Dictionary<string, object?> data)
    {
        return PlaceholderPattern.Replace(text, m =>
        {
            var val = ResolveValue(data, m.Groups[1].Value);
            return val?.ToString() ?? "";
        });
    }

    private bool IsOnlyPlaceholder(string text, out string key)
    {
        key = "";
        var m = PlaceholderPattern.Match(text);
        if (m.Success && m.Value == text.Trim())
        {
            key = m.Groups[1].Value;
            return true;
        }
        return false;
    }

    // =============================
    // 🔥 SNAPSHOT CLASSES
    private class CellSnapshot
    {
        public int Column { get; set; }
        public string Value { get; set; } = "";
        public bool FontBold { get; set; }
        public double FontSize { get; set; }
        public string FontName { get; set; } = "";
        public XLColor FontColor { get; set; } = XLColor.Black;
        public XLAlignmentHorizontalValues HAlign { get; set; }
        public XLAlignmentVerticalValues VAlign { get; set; }
        public bool WrapText { get; set; }
        public XLColor FillColor { get; set; } = XLColor.NoColor;
        public XLBorderStyleValues BorderTop { get; set; }
        public XLBorderStyleValues BorderBottom { get; set; }
        public XLBorderStyleValues BorderLeft { get; set; }
        public XLBorderStyleValues BorderRight { get; set; }
        public XLColor BorderTopColor { get; set; } = XLColor.Black;
        public XLColor BorderBottomColor { get; set; } = XLColor.Black;
        public XLColor BorderLeftColor { get; set; } = XLColor.Black;
        public XLColor BorderRightColor { get; set; } = XLColor.Black;
    }

    private class MergeSnapshot
    {
        public int FirstCol { get; set; }
        public int LastCol { get; set; }
        public int RowOffset { get; set; }
    }

    private class TableRow
    {
        public int Row { get; set; }
        public string ArrayName { get; set; } = "";
    }
}