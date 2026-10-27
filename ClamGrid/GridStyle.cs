namespace ClamGrid;

internal sealed record GridStyle
{
    public string FontFamily { get; set; } = "monospace";

    public double FontSize { get; set; } = 16;

    public double HorizontalPadding { get; set; } = 8;

    public double VerticalPadding { get; set; } = 8;

    public double? RowHeight { get; set; }

    public double? HeaderHeight { get; set; }

    public double RowHeaderWidth { get; set; } = 48;

    public double ScrollBarThickness { get; set; } = 4;

    public double GridLineWidth { get; set; } = 0.5;

    public bool ColumnHeadersVisible { get; set; } = true;

    public bool RowHeadersVisible { get; set; } = true;

    public bool HorizontalLinesVisible { get; set; } = true;

    public bool VerticalLinesVisible { get; set; } = true;

    public bool ScrollBarVisible { get; set; } = true;

    public string CornerText { get; set; } = "#";

    public TextAlignment RowHeaderAlignment { get; set; } = TextAlignment.End;

    // Default colors follow the Material palette: Blue 700 header, Cyan 700 and Orange 700 for the sorted column, Blue 100 selection and Gray tints
    public Color TextColor { get; set; } = Color.FromArgb("#212121");

    public Color EmptyTextColor { get; set; } = Color.FromArgb("#9E9E9E");

    public Color Background { get; set; } = Colors.White;

    // Background of every second row in display order; null keeps Background
    public Color? AlternatingRowBackground { get; set; }

    public Color HeaderBackground { get; set; } = Color.FromArgb("#1976D2");

    public Color HeaderTextColor { get; set; } = Colors.White;

    public Color RowHeaderBackground { get; set; } = Color.FromArgb("#F5F5F5");

    public Color GridLineColor { get; set; } = Color.FromArgb("#E0E0E0");

    public Color FrozenLineColor { get; set; } = Color.FromArgb("#9E9E9E");

    public Color ScrollBarColor { get; set; } = Color.FromArgb("#8C64748B");

    public Color SelectedBackground { get; set; } = Color.FromArgb("#BBDEFB");

    public Color SelectedTextColor { get; set; } = Color.FromArgb("#0D47A1");

    public Color AscendingHeaderBackground { get; set; } = Color.FromArgb("#0097A7");

    public Color DescendingHeaderBackground { get; set; } = Color.FromArgb("#F57C00");

    // Sort marks are drawn next to the header text; SortPriorityVisible also marks secondary keys with their priority
    public string AscendingSortMark { get; set; } = "↑";

    public string DescendingSortMark { get; set; } = "↓";

    public GridSortMarkPosition SortMarkPosition { get; set; }

    public bool SortPriorityVisible { get; set; }

    public Func<object, Color?>? RowBackground { get; set; }

    public Func<GridCellColorContext, GridColors>? CellColors { get; set; }

    public Func<GridColumnHeaderColorContext, GridColors>? ColumnHeaderColors { get; set; }

    public Func<GridRowHeaderColorContext, GridColors>? RowHeaderColors { get; set; }

    // Row header text by item; null falls back to the row number
    public Func<GridRowHeaderTextContext, string?>? RowHeaderText { get; set; }
}
