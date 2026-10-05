namespace Example.Components;

using System.Text.Json;
using System.Text.Json.Serialization;

// Compatible with the legacy ColumnOrder[] JSON (ColumnName / IsVisible); Width is written only for saved widths
public static class ColumnSettingsCodec
{
    public static GridColumnOrder[]? Read(string? json)
    {
        if (String.IsNullOrEmpty(json))
        {
            return null;
        }

        var entries = JsonSerializer.Deserialize(json, ColumnSettingsJsonContext.Default.ColumnSettingEntryArray);
        return entries?.Where(static x => !String.IsNullOrWhiteSpace(x.ColumnName)).Select(static x => new GridColumnOrder(x.ColumnName!, x.IsVisible) { Width = x.Width }).ToArray();
    }

    public static string Write(IEnumerable<GridColumnOrder> orders)
    {
        var entries = orders.Select(static x => new ColumnSettingEntry(x.Key, x.IsVisible, x.Width)).ToArray();
        return JsonSerializer.Serialize(entries, ColumnSettingsJsonContext.Default.ColumnSettingEntryArray);
    }
}

public sealed record ColumnSettingEntry(string? ColumnName, bool IsVisible, double? Width = null);

[JsonSourceGenerationOptions(DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(ColumnSettingEntry[]))]
internal sealed partial class ColumnSettingsJsonContext : JsonSerializerContext;
