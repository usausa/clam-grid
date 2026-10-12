namespace ClamGrid.Rendering;

using System.Text;

internal sealed class GridTextFormat
{
    private readonly string? format;
    private readonly CompositeFormat? composite;

    public static GridTextFormat Default { get; } = new(null, null);

    public bool IsDefault => ReferenceEquals(this, Default);

    private GridTextFormat(string? format, CompositeFormat? composite)
    {
        this.format = format;
        this.composite = composite;
    }

    public static GridTextFormat Parse(string? format)
    {
        if (String.IsNullOrEmpty(format))
        {
            return Default;
        }

        return format.Contains('{', StringComparison.Ordinal) ? new GridTextFormat(null, CompositeFormat.Parse(format)) : new GridTextFormat(format, null);
    }

    public string Format<T>(T value, IFormatProvider provider)
        where T : IFormattable =>
        composite is null ? value.ToString(format, provider) : String.Format(provider, composite, value);

    public string Format(object? value, IFormatProvider provider)
    {
        if (value is null)
        {
            return String.Empty;
        }

        if (composite is not null)
        {
            return String.Format(provider, composite, value);
        }

        return value is IFormattable formattable ? formattable.ToString(format, provider) : Convert.ToString(value, provider) ?? String.Empty;
    }
}
