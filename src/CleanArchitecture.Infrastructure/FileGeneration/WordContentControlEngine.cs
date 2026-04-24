using CleanArchitecture.Domain.Interfaces.Export;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using System.Text.Json;

namespace CleanArchitecture.Infrastructure.FileGeneration;

public class WordContentControlEngine : IWordTemplateEngine
{
    public Stream FillTemplate(string templatePath, Dictionary<string, object?>? data)
    {
        if (!File.Exists(templatePath))
            throw new FileNotFoundException($"Template not found: {templatePath}");

        data ??= new Dictionary<string, object?>();

        var ms = new MemoryStream();
        using (var fileStream = File.OpenRead(templatePath))
            fileStream.CopyTo(ms);

        ms.Position = 0;

        using (var doc = WordprocessingDocument.Open(ms, isEditable: true))
        {
            var body = doc.MainDocumentPart?.Document?.Body
                ?? throw new InvalidOperationException("Invalid Word document");

            ProcessRepeatingSections(body, data);
            ProcessSimpleContentControls(body, data);

            doc.MainDocumentPart!.Document.Save();
            ms.Position = 0;
        }

        ms.Position = 0;
        return ms;
    }

    // =========================================================
    // REPEATING SECTION
    // =========================================================
    private void ProcessRepeatingSections(OpenXmlElement root, Dictionary<string, object?> data)
    {
        var sdts = root.Descendants<SdtElement>()
            .Where(IsRepeatingSection)
            .Where(s => !IsInsideRepeatingSection(s))
            .ToList();

        foreach (var sdt in sdts)
        {
            if (sdt.Parent == null) continue;

            var tag = GetTag(sdt);
            if (string.IsNullOrEmpty(tag))
            {
                RemoveSdtKeepContent(sdt);
                continue;
            }

            if (!data.TryGetValue(tag, out var value) || value == null)
            {
                sdt.Remove();
                continue;
            }

            var items = ExtractList(value);
            if (items == null || items.Count == 0)
            {
                sdt.Remove();
                continue;
            }

            RepeatSection(sdt, items);
        }
    }

    private void RepeatSection(SdtElement sdt, List<Dictionary<string, object?>> items)
    {
        var content = sdt.ChildElements
            .FirstOrDefault(e => e is SdtContentBlock
                              || e is SdtContentRow
                              || e is SdtContentCell
                              || e is SdtContentRun);

        if (content == null) return;

        var templateItems = ExtractTemplateItems(content);
        if (templateItems.Count == 0) return;

        var insertAfter = (OpenXmlElement)sdt;

        foreach (var itemData in items)
        {
            foreach (var templateItem in templateItems)
            {
                var clone = (OpenXmlElement)templateItem.CloneNode(true);

                // Xử lý nested Repeating Section TRƯỚC
                ProcessRepeatingSectionsInClone(clone, itemData);

                // 🔥 FIX: Fill tất cả SDT trong clone, không check IsInsideRepeatingSection
                // vì clone chưa có parent trong document
                FillAllSimpleContentControls(clone, itemData);

                insertAfter.InsertAfterSelf(clone);
                insertAfter = clone;
            }
        }

        sdt.Remove();
    }

    /// <summary>
    /// Fill tất cả Simple Content Control trong element, bỏ qua check parent chain.
    /// Dùng khi xử lý clone của Repeating Section (clone chưa có parent).
    /// </summary>
    private void FillAllSimpleContentControls(OpenXmlElement root, Dictionary<string, object?> data)
    {
        // Lấy tất cả SDT không phải repeating
        var sdts = root.Descendants<SdtElement>()
            .Where(s => !IsRepeatingSection(s))
            .Where(s => !IsRepeatingSectionItem(s))
            .ToList();

        // Filter chỉ lấy SDT ở cấp này, không phải trong nested repeating section khác
        foreach (var sdt in sdts)
        {
            if (sdt.Parent == null) continue;

            // Check parent chain trong phạm vi clone
            if (HasNestedRepeatingSectionParent(sdt, root)) continue;

            var tag = GetTag(sdt);
            if (string.IsNullOrEmpty(tag)) continue;

            string textValue = data.TryGetValue(tag, out var value)
                ? ConvertToString(value)
                : "";

            SetContentControlText(sdt, textValue);
        }
    }

    /// <summary>
    /// Check SDT có nằm trong Repeating Section con (không phải root Repeating Section)
    /// trong phạm vi từ SDT lên đến boundary.
    /// </summary>
    private bool HasNestedRepeatingSectionParent(OpenXmlElement sdt, OpenXmlElement boundary)
    {
        var parent = sdt.Parent;
        while (parent != null && parent != boundary)
        {
            if (parent is SdtElement parentSdt && IsRepeatingSection(parentSdt))
                return true;
            parent = parent.Parent;
        }
        return false;
    }

    /// <summary>
    /// Lấy template items từ SDT content.
    /// Nếu content chứa SDT "repeatingSectionItem" → unwrap lấy content thực sự.
    /// </summary>
    private List<OpenXmlElement> ExtractTemplateItems(OpenXmlElement content)
    {
        var result = new List<OpenXmlElement>();

        foreach (var child in content.ChildElements)
        {
            if (child is SdtElement childSdt && IsRepeatingSectionItem(childSdt))
            {
                var innerContent = childSdt.ChildElements
                    .FirstOrDefault(e => e is SdtContentBlock
                                      || e is SdtContentRow
                                      || e is SdtContentCell
                                      || e is SdtContentRun);

                if (innerContent != null)
                {
                    foreach (var innerChild in innerContent.ChildElements)
                        result.Add(innerChild);
                }
            }
            else
            {
                result.Add(child);
            }
        }

        return result;
    }

    private bool IsRepeatingSectionItem(OpenXmlElement sdt)
    {
        var props = sdt.Descendants<SdtProperties>().FirstOrDefault();
        if (props == null) return false;
        return props.Descendants()
            .Any(e => e.LocalName == "repeatingSectionItem");
    }

    private void ProcessRepeatingSectionsInClone(OpenXmlElement clone, Dictionary<string, object?> itemData)
    {
        var nestedRS = clone.Descendants<SdtElement>()
            .Where(IsRepeatingSection)
            .Where(s => !HasParentRepeatingSectionWithin(s, clone))
            .ToList();

        foreach (var nested in nestedRS)
        {
            if (nested.Parent == null) continue;

            var tag = GetTag(nested);
            if (string.IsNullOrEmpty(tag))
            {
                nested.Remove();
                continue;
            }

            if (!itemData.TryGetValue(tag, out var value) || value == null)
            {
                nested.Remove();
                continue;
            }

            var nestedItems = ExtractList(value);
            if (nestedItems == null || nestedItems.Count == 0)
            {
                nested.Remove();
                continue;
            }

            RepeatSection(nested, nestedItems);
        }
    }

    private bool HasParentRepeatingSectionWithin(OpenXmlElement sdt, OpenXmlElement boundary)
    {
        var parent = sdt.Parent;
        while (parent != null && parent != boundary)
        {
            if (parent is SdtElement parentSdt && IsRepeatingSection(parentSdt))
                return true;
            parent = parent.Parent;
        }
        return false;
    }

    private bool IsInsideRepeatingSection(OpenXmlElement sdt)
    {
        var parent = sdt.Parent;
        while (parent != null)
        {
            if (parent is SdtElement parentSdt && IsRepeatingSection(parentSdt))
                return true;
            parent = parent.Parent;
        }
        return false;
    }

    private bool IsRepeatingSection(OpenXmlElement sdt)
    {
        var props = sdt.Descendants<SdtProperties>().FirstOrDefault();
        if (props == null) return false;
        return props.Descendants()
            .Any(e => e.LocalName == "repeatingSection");
    }

    // =========================================================
    // SIMPLE CONTENT CONTROL
    // =========================================================
    private void ProcessSimpleContentControls(OpenXmlElement root, Dictionary<string, object?> data)
    {
        ProcessSimpleContentControlsInElement(root, data);
    }

    private void ProcessSimpleContentControlsInElement(
    OpenXmlElement root,
    Dictionary<string, object?> data)
    {
        var sdts = root.Descendants<SdtElement>()
            .Where(s => !IsRepeatingSection(s))
            .Where(s => !IsRepeatingSectionItem(s))
            .ToList();

        foreach (var sdt in sdts)
        {
            if (sdt.Parent == null) continue;

            var tag = GetTag(sdt);
            if (string.IsNullOrWhiteSpace(tag))
                continue;

            object? value = ResolveValue(data, tag);

            SetContentControlText(
                sdt,
                ConvertToString(value)
            );
        }
    }

    private object? ResolveValue(
    Dictionary<string, object?> data,
    string path)
    {
        object? current = data;

        foreach (var part in path.Split('.'))
        {
            if (current == null)
                return null;

            if (current is Dictionary<string, object?> dict)
            {
                if (!dict.TryGetValue(part, out current))
                    return null;
            }
            else if (current is JsonElement je)
            {
                if (je.ValueKind == JsonValueKind.Object &&
                    je.TryGetProperty(part, out var prop))
                {
                    current = prop;
                }
                else
                {
                    return null;
                }
            }
            else
            {
                var prop = current.GetType().GetProperty(part);
                current = prop?.GetValue(current);
            }
        }

        return current;
    }

    private void SetContentControlText(OpenXmlElement sdt, string value)
    {
        var contentRun = sdt.ChildElements.OfType<SdtContentRun>().FirstOrDefault();
        var contentBlock = sdt.ChildElements.OfType<SdtContentBlock>().FirstOrDefault();
        var contentCell = sdt.ChildElements.OfType<SdtContentCell>().FirstOrDefault();

        OpenXmlElement? content = null;
        if (contentRun != null) content = contentRun;
        else if (contentBlock != null) content = contentBlock;
        else if (contentCell != null) content = contentCell;

        if (content == null) return;

        var firstRun = content.Descendants<Run>().FirstOrDefault();
        RunProperties? rPr = null;
        if (firstRun?.RunProperties != null)
            rPr = (RunProperties)firstRun.RunProperties.CloneNode(true);

        var newRun = new Run();
        if (rPr != null) newRun.AppendChild(rPr);
        newRun.AppendChild(new Text(value ?? "") { Space = SpaceProcessingModeValues.Preserve });

        if (content is SdtContentRun)
        {
            foreach (var r in content.Elements<Run>().ToList()) r.Remove();
            content.AppendChild(newRun);
        }
        else if (content is SdtContentBlock)
        {
            var existingPara = content.Elements<Paragraph>().FirstOrDefault();
            ParagraphProperties? pPr = null;
            if (existingPara?.ParagraphProperties != null)
                pPr = (ParagraphProperties)existingPara.ParagraphProperties.CloneNode(true);

            foreach (var p in content.Elements<Paragraph>().ToList()) p.Remove();

            var newPara = new Paragraph();
            if (pPr != null) newPara.AppendChild(pPr);
            newPara.AppendChild(newRun);
            content.AppendChild(newPara);
        }
        else if (content is SdtContentCell)
        {
            foreach (var cell in content.Elements<TableCell>().ToList())
            {
                foreach (var p in cell.Elements<Paragraph>().ToList()) p.Remove();
                cell.AppendChild(new Paragraph(newRun));
            }
        }
    }

    private string? GetTag(OpenXmlElement sdt)
    {
        var props = sdt.Descendants<SdtProperties>().FirstOrDefault();
        return props?.Descendants<Tag>().FirstOrDefault()?.Val?.Value;
    }

    private void RemoveSdtKeepContent(SdtElement sdt)
    {
        var content = sdt.ChildElements
            .FirstOrDefault(e => e is SdtContentBlock
                              || e is SdtContentRow
                              || e is SdtContentCell
                              || e is SdtContentRun);

        if (content == null)
        {
            sdt.Remove();
            return;
        }

        foreach (var child in content.ChildElements.ToList())
        {
            child.Remove();
            sdt.InsertBeforeSelf(child);
        }

        sdt.Remove();
    }

    // =========================================================
    // DATA HELPERS (giữ nguyên)
    // =========================================================
    private List<Dictionary<string, object?>>? ExtractList(object value)
    {
        if (value is JsonElement je)
        {
            if (je.ValueKind != JsonValueKind.Array) return null;
            return je.EnumerateArray()
                .Where(e => e.ValueKind == JsonValueKind.Object)
                .Select(JsonElementToDict)
                .ToList();
        }

        if (value is IEnumerable<Dictionary<string, object?>> listDict)
            return listDict.ToList();

        if (value is System.Collections.IEnumerable enumerable && value is not string)
        {
            var result = new List<Dictionary<string, object?>>();
            foreach (var item in enumerable)
            {
                if (item is Dictionary<string, object?> d) result.Add(d);
                else if (item is JsonElement jeItem && jeItem.ValueKind == JsonValueKind.Object)
                    result.Add(JsonElementToDict(jeItem));
            }
            return result.Count > 0 ? result : null;
        }

        return null;
    }

    private Dictionary<string, object?> JsonElementToDict(JsonElement obj)
    {
        var dict = new Dictionary<string, object?>();
        foreach (var prop in obj.EnumerateObject())
        {
            dict[prop.Name] = prop.Value.ValueKind switch
            {
                JsonValueKind.String => prop.Value.GetString(),
                JsonValueKind.Number => prop.Value.GetDecimal(),
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                JsonValueKind.Null => null,
                JsonValueKind.Array => prop.Value,
                JsonValueKind.Object => prop.Value,
                _ => prop.Value.ToString()
            };
        }
        return dict;
    }

    private string ConvertToString(object? value)
    {
        if (value == null) return "";

        if (value is JsonElement je)
        {
            return je.ValueKind switch
            {
                JsonValueKind.String => je.GetString() ?? "",
                JsonValueKind.Number => FormatNumber(je.GetDecimal()),
                JsonValueKind.True => "✓",
                JsonValueKind.False => "",
                JsonValueKind.Null => "",
                _ => je.ToString()
            };
        }

        return value switch
        {
            string s => s,
            decimal dec => FormatNumber(dec),
            double dbl => FormatNumber((decimal)dbl),
            float flt => FormatNumber((decimal)flt),
            int i => i.ToString(),
            long l => l.ToString(),
            DateTime dt => dt.ToString("dd/MM/yyyy"),
            bool b => b ? "✓" : "",
            _ => value.ToString() ?? ""
        };
    }

    private string FormatNumber(decimal num)
    {
        if (num == Math.Floor(num)) return num.ToString("N0");
        return num.ToString("N2");
    }
}