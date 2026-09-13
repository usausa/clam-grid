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

    public Color TextColor { get; set; } = Color.FromArgb("#1E293B");

    public Color Background { get; set; } = Colors.White;

    public Color HeaderBackground { get; set; } = Color.FromArgb("#F8FAFC");

    public Color HeaderTextColor { get; set; } = Color.FromArgb("#334155");

    public Color RowHeaderBackground { get; set; } = Color.FromArgb("#F8FAFC");

    public Color GridLineColor { get; set; } = Color.FromArgb("#E2E8F0");

    public Color SelectedBackground { get; set; } = Color.FromArgb("#DBEAFE");

    public Color SelectedTextColor { get; set; } = Color.FromArgb("#1E3A8A");

    public Color AscendingHeaderBackground { get; set; } = Color.FromArgb("#E0E7FF");

    public Color DescendingHeaderBackground { get; set; } = Color.FromArgb("#FEF3C7");

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
