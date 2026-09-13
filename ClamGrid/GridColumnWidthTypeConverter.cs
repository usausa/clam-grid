namespace ClamGrid;

using System.ComponentModel;
using System.Globalization;

// Converts XAML strings such as "Auto", "*", "2*" and "85" to a column width
public sealed class GridColumnWidthTypeConverter : TypeConverter
{
    public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType) => sourceType == typeof(string);

    public override bool CanConvertTo(ITypeDescriptorContext? context, Type? destinationType) => destinationType == typeof(string);

    public override object ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value)
    {
        if (value is not string text)
        {
            throw new NotSupportedException($"Cannot convert {value.GetType().Name} to {nameof(GridColumnWidth)}.");
        }

        text = text.Trim();
        if (text.Equals("Auto", StringComparison.OrdinalIgnoreCase))
        {
            return GridColumnWidth.Auto;
        }

        if (text.EndsWith('*'))
        {
            var weight = text[..^1].Trim();
            return GridColumnWidth.Star(weight.Length == 0 ? 1 : Double.Parse(weight, NumberStyles.Float, CultureInfo.InvariantCulture));
        }

        return GridColumnWidth.Absolute(Double.Parse(text, NumberStyles.Float, CultureInfo.InvariantCulture));
    }

    public override object ConvertTo(ITypeDescriptorContext? context, CultureInfo? culture, object? value, Type destinationType)
    {
        if ((value is not GridColumnWidth width) || (destinationType != typeof(string)))
        {
            throw new NotSupportedException($"Cannot convert {value?.GetType().Name ?? "null"} to {destinationType.Name}.");
        }

        return width.Unit switch
        {
            GridColumnWidthUnit.Auto => "Auto",
            GridColumnWidthUnit.Star => width.Value.Equals(1d) ? "*" : width.Value.ToString(CultureInfo.InvariantCulture) + "*",
            _ => width.Value.ToString(CultureInfo.InvariantCulture)
        };
    }
}
