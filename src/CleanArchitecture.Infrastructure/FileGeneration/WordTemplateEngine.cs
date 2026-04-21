// CleanArchitecture.Infrastructure/FileGeneration/WordTemplateEngine.cs

using CleanArchitecture.Domain.Interfaces.Export;
using MiniSoftware;
using System.IO.Compression;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml;

namespace CleanArchitecture.Infrastructure.FileGeneration;

public class WordTemplateEngine : IWordTemplateEngine
{
    // Regex tìm placeholder bị vỡ bởi Word XML runs
    // VD: <w:t>{{</w:t></w:r><w:r><w:t>Items</w:t></w:r><w:r><w:t>}}</w:t>
    private static readonly Regex BrokenPlaceholderPattern = new(
        @"\{\{[^}]*\}\}",
        RegexOptions.Compiled);

    public Stream FillTemplate(
        string templatePath,
        Dictionary<string, object?>? data)
    {
        if (!File.Exists(templatePath))
            throw new FileNotFoundException($"Word template not found: {templatePath}");

        if (data == null || !data.Any())
            throw new ArgumentException("Data is empty");

        // 🔥 BƯỚC 1: Fix XML bị vỡ run trước khi MiniWord xử lý
        var fixedTemplateBytes = FixBrokenRunsInDocx(templatePath);

        // 🔥 BƯỚC 2: Convert data sang format MiniWord
        var miniWordData = ConvertToMiniWordData(data);

        // 🔥 BƯỚC 3: MiniWord fill từ fixed template bytes
        var outputStream = new MemoryStream();
        using (var tempStream = new MemoryStream(fixedTemplateBytes))
        {
            var tempFilePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.docx");

            // ghi file đã fix XML ra file tạm
            File.WriteAllBytes(tempFilePath, fixedTemplateBytes);

            // dùng MiniWord với path
            MiniWord.SaveAsByTemplate(outputStream, tempFilePath, miniWordData);

            // xoá file temp
            File.Delete(tempFilePath);
        }

        outputStream.Position = 0;
        return outputStream;
    }

    // =========================================================
    // 🔥 FIX BROKEN RUNS TRONG DOCX
    // docx là file ZIP chứa XML — ta unzip, fix XML, rezip
    // =========================================================
    private byte[] FixBrokenRunsInDocx(string templatePath)
    {
        var outputBytes = new MemoryStream();

        using var inputZip = ZipFile.OpenRead(templatePath);
        using var outputZip = new ZipArchive(outputBytes, ZipArchiveMode.Create, leaveOpen: true);

        foreach (var entry in inputZip.Entries)
        {
            var outEntry = outputZip.CreateEntry(entry.FullName, CompressionLevel.Fastest);

            using var inStream = entry.Open();
            using var outStream = outEntry.Open();

            // Chỉ xử lý file XML trong thư mục word/
            if (entry.FullName.StartsWith("word/") && entry.FullName.EndsWith(".xml"))
            {
                var content = new StreamReader(inStream, Encoding.UTF8).ReadToEnd();
                var fixed_ = MergeFragmentedRuns(content);
                var fixedBytes = Encoding.UTF8.GetBytes(fixed_);
                outStream.Write(fixedBytes, 0, fixedBytes.Length);
            }
            else
            {
                inStream.CopyTo(outStream);
            }
        }

        outputZip.Dispose();
        return outputBytes.ToArray();
    }

    // =========================================================
    // 🔥 MERGE FRAGMENTED RUNS
    // Tìm và merge các <w:r> bị vỡ chứa placeholder {{...}}
    // =========================================================
    private string MergeFragmentedRuns(string xml)
    {
        // Pattern tìm chuỗi các <w:r>...</w:r> liên tiếp
        // mà khi ghép text lại thì có chứa {{...}}
        // Strategy: extract tất cả text trong paragraph, merge runs có chứa {{ hoặc }}

        // 🔥 Approach đơn giản và hiệu quả:
        // Replace toàn bộ cụm <w:r>...</w:r> liên tiếp mà text gộp lại = {{...}}
        // bằng 1 <w:r> duy nhất

        // Regex tìm chuỗi <w:r> elements liên tiếp trong cùng 1 paragraph run group
        // mà text ghép lại chứa {{ hoặc }}
        var result = MergeRunsContainingPlaceholders(xml);
        return result;
    }

    private string MergeRunsContainingPlaceholders(string xml)
    {
        // Pattern bắt một <w:r>...</w:r> với optional <w:rPr>
        // Sau đó tìm chuỗi liên tiếp và merge nếu text tổng chứa {{...}}

        // Regex bắt từng paragraph <w:p>...</w:p>
        var paraPattern = new Regex(
            @"<w:p[ >].*?</w:p>",
            RegexOptions.Singleline | RegexOptions.Compiled);

        return paraPattern.Replace(xml, para => FixParagraphRuns(para.Value));
    }

    private string FixParagraphRuns(string paragraphXml)
    {
        // Tìm tất cả <w:r>...</w:r> trong paragraph
        var runPattern = new Regex(
            @"<w:r\b[^>]*>.*?</w:r>",
            RegexOptions.Singleline | RegexOptions.Compiled);

        var runs = runPattern.Matches(paragraphXml);
        if (runs.Count == 0) return paragraphXml;

        // Extract text từ mỗi run
        var textPattern = new Regex(@"<w:t[^>]*>(.*?)</w:t>", RegexOptions.Singleline);
        var rPrPattern = new Regex(@"<w:rPr>.*?</w:rPr>", RegexOptions.Singleline);

        // Gom các run liên tiếp
        var runInfos = runs.Cast<Match>().Select(m => new RunInfo
        {
            FullMatch = m.Value,
            StartIndex = m.Index,
            EndIndex = m.Index + m.Length,
            Text = string.Concat(textPattern.Matches(m.Value)
                .Cast<Match>().Select(t => t.Groups[1].Value)),
            RPr = rPrPattern.Match(m.Value) is { Success: true } rp ? rp.Value : ""
        }).ToList();

        // Tìm nhóm runs liên tiếp mà text gộp lại chứa {{...}}
        var result = paragraphXml;
        var processed = new HashSet<int>();

        // Duyệt từ cuối lên để không lệch index
        for (int i = runInfos.Count - 1; i >= 0; i--)
        {
            if (processed.Contains(i)) continue;

            // Thử gom từ run i với các run tiếp theo
            // nếu text tích lũy chứa {{ mở nhưng chưa đóng }}
            var accumulated = runInfos[i].Text;

            if (!accumulated.Contains("{{") && !accumulated.Contains("}}"))
                continue;

            // Tìm range cần merge
            int startRunIdx = i;
            int endRunIdx = i;
            string mergedText = accumulated;

            // Nếu có {{ chưa có }} — merge với các run tiếp theo
            if (accumulated.Contains("{{") && !accumulated.Contains("}}"))
            {
                for (int j = i + 1; j < runInfos.Count; j++)
                {
                    mergedText += runInfos[j].Text;
                    endRunIdx = j;
                    processed.Add(j);

                    if (mergedText.Contains("}}"))
                        break;
                }
            }
            // Nếu có }} không có {{ — merge với các run trước đó
            else if (accumulated.Contains("}}") && !accumulated.Contains("{{"))
            {
                for (int j = i - 1; j >= 0; j--)
                {
                    mergedText = runInfos[j].Text + mergedText;
                    startRunIdx = j;
                    processed.Add(j);

                    if (mergedText.Contains("{{"))
                        break;
                }
            }

            if (startRunIdx == endRunIdx) continue; // Không cần merge

            // Kiểm tra text gộp có chứa placeholder hoàn chỉnh không
            if (!Regex.IsMatch(mergedText, @"\{\{[^}]+\}\}")) continue;

            // Tạo run mới merged
            var rPr = runInfos[startRunIdx].RPr;
            var escapedText = EscapeXml(mergedText);
            var needsPreserve = mergedText.StartsWith(" ") || mergedText.EndsWith(" ");
            var spaceAttr = needsPreserve ? " xml:space=\"preserve\"" : "";
            var mergedRun = $"<w:r>{rPr}<w:t{spaceAttr}>{escapedText}</w:t></w:r>";

            // Replace trong XML — từ startRun đến endRun
            var startPos = runInfos[startRunIdx].StartIndex;
            var endPos = runInfos[endRunIdx].EndIndex;
            var originalSegment = paragraphXml.Substring(startPos, endPos - startPos);

            result = result.Replace(originalSegment, mergedRun);
        }

        return result;
    }

    private string EscapeXml(string text)
    {
        return text
            .Replace("&", "&amp;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;")
            .Replace("\"", "&quot;")
            .Replace("'", "&apos;");
    }

    private class RunInfo
    {
        public string FullMatch { get; set; } = "";
        public int StartIndex { get; set; }
        public int EndIndex { get; set; }
        public string Text { get; set; } = "";
        public string RPr { get; set; } = "";
    }

    // =========================================================
    // CONVERT DATA — giữ nguyên từ trước
    // =========================================================
    private Dictionary<string, object> ConvertToMiniWordData(Dictionary<string, object?> data)
    {
        var result = new Dictionary<string, object>();

        foreach (var kvp in data)
        {
            result[kvp.Key] = kvp.Value == null ? "" : ConvertValue(kvp.Value);
        }

        return result;
    }

    private object ConvertValue(object value)
    {
        if (value is JsonElement je)
        {
            return je.ValueKind switch
            {
                JsonValueKind.Array => ConvertJsonArray(je),
                JsonValueKind.Object => ConvertJsonObject(je),
                JsonValueKind.String => je.GetString() ?? "",
                JsonValueKind.Number => je.GetDecimal(),
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                JsonValueKind.Null => "",
                _ => je.ToString()
            };
        }

        if (value is IEnumerable<Dictionary<string, object?>> listDict)
        {
            return listDict
                .Select(d => d.ToDictionary(k => k.Key, v => ConvertValue(v.Value ?? "")))
                .ToList();
        }

        if (value is System.Collections.IEnumerable enumerable && value is not string)
        {
            var list = new List<Dictionary<string, object>>();
            foreach (var item in enumerable)
            {
                if (item is Dictionary<string, object?> d)
                    list.Add(d.ToDictionary(k => k.Key, v => ConvertValue(v.Value ?? "")));
            }
            return list.Count > 0 ? (object)list : value.ToString() ?? "";
        }

        return value;
    }

    private List<Dictionary<string, object>> ConvertJsonArray(JsonElement arrayElement)
    {
        var list = new List<Dictionary<string, object>>();
        foreach (var item in arrayElement.EnumerateArray())
        {
            if (item.ValueKind == JsonValueKind.Object)
                list.Add(ConvertJsonObject(item));
        }
        return list;
    }

    private Dictionary<string, object> ConvertJsonObject(JsonElement objElement)
    {
        var dict = new Dictionary<string, object>();
        foreach (var prop in objElement.EnumerateObject())
        {
            dict[prop.Name] = prop.Value.ValueKind switch
            {
                JsonValueKind.String => prop.Value.GetString() ?? "",
                JsonValueKind.Number => (object)prop.Value.GetDecimal(),
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                JsonValueKind.Array => ConvertJsonArray(prop.Value),
                JsonValueKind.Object => ConvertJsonObject(prop.Value),
                _ => ""
            };
        }
        return dict;
    }
}