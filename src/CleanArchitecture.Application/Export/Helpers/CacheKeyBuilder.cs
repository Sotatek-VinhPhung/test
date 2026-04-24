using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace CleanArchitecture.Application.Export.Helpers;

public static class CacheKeyBuilder
{
    public static string Build(
        string templateType,
        string templateName,
        int templateVersion,
        object? rawData,
        string format,
        string[]? ignoreFields = null)
    {
        var dataJson = rawData == null ? "" : JsonSerializer.Serialize(rawData);
        dataJson = NormalizeJson(dataJson, ignoreFields);

        var raw = $"{format}|{templateType}|{templateName}|v{templateVersion}|{dataJson}";

        using var sha = SHA256.Create();
        var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(raw));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private static string NormalizeJson(string json, string[]? ignoreFields)
    {
        if (string.IsNullOrWhiteSpace(json)) return "";
        try
        {
            using var doc = JsonDocument.Parse(json);
            using var ms = new MemoryStream();
            using (var writer = new Utf8JsonWriter(ms))
                WriteSorted(doc.RootElement, writer, ignoreFields);
            return Encoding.UTF8.GetString(ms.ToArray());
        }
        catch { return json; }
    }

    private static void WriteSorted(JsonElement element, Utf8JsonWriter writer, string[]? ignoreFields)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                writer.WriteStartObject();
                foreach (var prop in element.EnumerateObject().OrderBy(p => p.Name, StringComparer.Ordinal))
                {
                    if (ignoreFields != null && ignoreFields.Contains(prop.Name, StringComparer.OrdinalIgnoreCase))
                        continue;
                    writer.WritePropertyName(prop.Name);
                    WriteSorted(prop.Value, writer, ignoreFields);
                }
                writer.WriteEndObject();
                break;
            case JsonValueKind.Array:
                writer.WriteStartArray();
                foreach (var item in element.EnumerateArray())
                    WriteSorted(item, writer, ignoreFields);
                writer.WriteEndArray();
                break;
            default:
                element.WriteTo(writer);
                break;
        }
    }
}