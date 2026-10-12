namespace ClamGrid;

using ClamGrid.Rendering;

// Has a parameterless constructor for XAML; a column without ValueAccessor is resolved by key from ClamGridView.ValueAccessors
public sealed record GridColumn
{
    public string Key { get; set; } = String.Empty;

    public string Header { get; set; } = String.Empty;

    public IGridValueAccessor ValueAccessor { get; set; } = GridUnresolvedValueAccessor.Instance;

    public GridColumnWidth Width { get; set; } = GridColumnWidth.Auto;

    public double MinWidth { get; set; } = 40;

    public TextAlignment Alignment { get; set; }

    // Composite format applied to any value when it contains '{' such as "{0:N0} pcs", otherwise a format string applied to IFormattable values such as "N0" or "yyyy/MM/dd"
    public string? Format
    {
        get;
        set
        {
            TextFormat = GridTextFormat.Parse(value);
            field = value;
        }
    }

    public IValueConverter? Converter { get; set; }

    public Color? HeaderBackground { get; set; }

    public Color? HeaderTextColor { get; set; }

    public Color? TextColor { get; set; }

    public Color? Background { get; set; }

    public bool IsBoolean { get; set; }

    public bool? IsReadOnly { get; set; }

    public string? SortKey { get; set; }

    public bool AllowSorting { get; set; } = true;

    public bool AllowResizing { get; set; } = true;

    internal bool IsResolved => !ReferenceEquals(ValueAccessor, GridUnresolvedValueAccessor.Instance);

    internal GridTextFormat TextFormat { get; private set; } = GridTextFormat.Default;

    public GridColumn()
    {
    }

    public GridColumn(string key, string header, IGridValueAccessor valueAccessor)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentNullException.ThrowIfNull(header);
        ArgumentNullException.ThrowIfNull(valueAccessor);
        Key = key;
        Header = header;
        ValueAccessor = valueAccessor;
    }
}
