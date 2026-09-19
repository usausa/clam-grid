namespace ClamGrid;

// Has a parameterless constructor for XAML; a column without ValueAccessor is resolved by key from ClamGridView.ValueAccessors
public sealed record GridColumn
{
    public string Key { get; set; } = String.Empty;

    public string Header { get; set; } = String.Empty;

    public IGridValueAccessor ValueAccessor { get; set; } = GridUnresolvedValueAccessor.Instance;

    public GridColumnWidth Width { get; set; } = GridColumnWidth.Auto;

    public double MinWidth { get; set; } = 40;

    public TextAlignment Alignment { get; set; }

    // Format string applied to IFormattable values such as "N0" or "yyyy/MM/dd"; strings and booleans are not affected
    public string? Format { get; set; }

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
