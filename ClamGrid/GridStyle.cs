namespace ClamGrid;

// Can be a XAML resource; treat it as immutable once assigned and replace it with a with expression
public sealed record GridStyle
{
    public string FontFamily { get; set; } = "monospace";

    public float FontSize { get; set; } = 16;

    public float HorizontalPadding { get; set; } = 8;

    public float VerticalPadding { get; set; } = 8;

    public double? RowHeight { get; set; }

    public double? HeaderHeight { get; set; }

    public double RowHeaderWidth { get; set; } = 48;

    public bool ShowColumnHeaders { get; set; } = true;

    public bool ShowRowHeaders { get; set; } = true;

    public bool ShowVerticalLines { get; set; } = true;

    public string CornerText { get; set; } = "#";

    public TextAlignment RowHeaderAlignment { get; set; } = TextAlignment.End;

    // Default colors follow the Material palette: Blue 700 header, Cyan 700 and Orange 700 for the sorted column, Blue 100 selection and Gray tints
    public Color TextColor { get; set; } = Color.FromArgb("#212121");

    public Color Background { get; set; } = Colors.White;

    public Color HeaderBackground { get; set; } = Color.FromArgb("#1976D2");

    public Color HeaderTextColor { get; set; } = Colors.White;

    public Color RowHeaderBackground { get; set; } = Color.FromArgb("#F5F5F5");

    public Color GridLineColor { get; set; } = Color.FromArgb("#E0E0E0");

    public Color FrozenLineColor { get; set; } = Color.FromArgb("#9E9E9E");

    public Color SelectedBackground { get; set; } = Color.FromArgb("#BBDEFB");

    public Color SelectedTextColor { get; set; } = Color.FromArgb("#0D47A1");

    public Color AscendingHeaderBackground { get; set; } = Color.FromArgb("#0097A7");

    public Color DescendingHeaderBackground { get; set; } = Color.FromArgb("#F57C00");

    // Sort marks are drawn next to the header text; ShowSortPriority also marks secondary keys with their priority
    public string AscendingSortMark { get; set; } = "↑";

    public string DescendingSortMark { get; set; } = "↓";

    public GridSortMarkPosition SortMarkPosition { get; set; }

    public bool ShowSortPriority { get; set; }

    public Func<object, Color?>? RowBackground { get; set; }

    public Func<GridCellColorContext, GridColors>? CellColors { get; set; }

    public Func<GridColumnHeaderColorContext, GridColors>? ColumnHeaderColors { get; set; }

    public Func<GridRowHeaderColorContext, GridColors>? RowHeaderColors { get; set; }

    // Row header text by item; null falls back to the row number
    public Func<GridRowHeaderTextContext, string?>? RowHeaderText { get; set; }

    internal void Validate()
    {
        if (!Single.IsFinite(FontSize) || (FontSize <= 0) || !Single.IsFinite(HorizontalPadding) || (HorizontalPadding < 0) || !Single.IsFinite(VerticalPadding) || (VerticalPadding < 0))
        {
            throw new ArgumentException("Font size and padding must be valid finite values.");
        }
    }
}
