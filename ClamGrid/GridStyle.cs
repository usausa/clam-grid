namespace ClamGrid;

// XAML のリソースとしても定義できる。割り当て後は不変として扱い、変更するときは with 式で新しいインスタンスを設定する。
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

    public Func<object, Color?>? RowBackground { get; set; }

    public Func<GridCellColorContext, GridColors>? CellColors { get; set; }

    public Func<GridColumnHeaderColorContext, GridColors>? ColumnHeaderColors { get; set; }

    public Func<GridRowHeaderColorContext, GridColors>? RowHeaderColors { get; set; }

    internal void Validate()
    {
        if (!Single.IsFinite(FontSize) || (FontSize <= 0) || !Single.IsFinite(HorizontalPadding) || (HorizontalPadding < 0) || !Single.IsFinite(VerticalPadding) || (VerticalPadding < 0))
        {
            throw new ArgumentException("Font size and padding must be valid finite values.");
        }
    }
}
