namespace ClamGrid;

public partial class ClamGridView
{
    public static readonly BindableProperty FontFamilyProperty = BindableProperty.Create(nameof(FontFamily), typeof(string), typeof(ClamGridView), "monospace", propertyChanged: OnStyleFontChanged);
    public static readonly BindableProperty FontSizeProperty = BindableProperty.Create(nameof(FontSize), typeof(double), typeof(ClamGridView), 16d, validateValue: static (_, value) => ValidatePositive(value, nameof(FontSize)), propertyChanged: OnStyleFontChanged);
    public static readonly BindableProperty HorizontalPaddingProperty = BindableProperty.Create(nameof(HorizontalPadding), typeof(double), typeof(ClamGridView), 8d, validateValue: static (_, value) => ValidateSize(value, nameof(HorizontalPadding)), propertyChanged: OnStyleChanged);
    public static readonly BindableProperty VerticalPaddingProperty = BindableProperty.Create(nameof(VerticalPadding), typeof(double), typeof(ClamGridView), 8d, validateValue: static (_, value) => ValidateSize(value, nameof(VerticalPadding)), propertyChanged: OnStyleChanged);
    public static readonly BindableProperty RowHeightProperty = BindableProperty.Create(nameof(RowHeight), typeof(double?), typeof(ClamGridView), validateValue: static (_, value) => ValidatePositive(value, nameof(RowHeight)), propertyChanged: OnStyleChanged);
    public static readonly BindableProperty HeaderHeightProperty = BindableProperty.Create(nameof(HeaderHeight), typeof(double?), typeof(ClamGridView), validateValue: static (_, value) => ValidatePositive(value, nameof(HeaderHeight)), propertyChanged: OnStyleChanged);
    public static readonly BindableProperty RowHeaderWidthProperty = BindableProperty.Create(nameof(RowHeaderWidth), typeof(double), typeof(ClamGridView), 48d, validateValue: static (_, value) => ValidateSize(value, nameof(RowHeaderWidth)), propertyChanged: OnStyleChanged);
    public static readonly BindableProperty ScrollBarThicknessProperty = BindableProperty.Create(nameof(ScrollBarThickness), typeof(double), typeof(ClamGridView), 4d, validateValue: static (_, value) => ValidatePositive(value, nameof(ScrollBarThickness)), propertyChanged: OnStyleChanged);
    public static readonly BindableProperty GridLineWidthProperty = BindableProperty.Create(nameof(GridLineWidth), typeof(double), typeof(ClamGridView), 0.5d, validateValue: static (_, value) => ValidatePositive(value, nameof(GridLineWidth)), propertyChanged: OnStyleChanged);
    public static readonly BindableProperty ColumnHeadersVisibleProperty = BindableProperty.Create(nameof(ColumnHeadersVisible), typeof(bool), typeof(ClamGridView), true, propertyChanged: OnStyleChanged);
    public static readonly BindableProperty RowHeadersVisibleProperty = BindableProperty.Create(nameof(RowHeadersVisible), typeof(bool), typeof(ClamGridView), true, propertyChanged: OnStyleChanged);
    public static readonly BindableProperty HorizontalLinesVisibleProperty = BindableProperty.Create(nameof(HorizontalLinesVisible), typeof(bool), typeof(ClamGridView), true, propertyChanged: OnStyleRenderChanged);
    public static readonly BindableProperty VerticalLinesVisibleProperty = BindableProperty.Create(nameof(VerticalLinesVisible), typeof(bool), typeof(ClamGridView), true, propertyChanged: OnStyleRenderChanged);
    public static readonly BindableProperty ScrollBarVisibleProperty = BindableProperty.Create(nameof(ScrollBarVisible), typeof(bool), typeof(ClamGridView), true, propertyChanged: OnStyleRenderChanged);
    public static readonly BindableProperty CornerTextProperty = BindableProperty.Create(nameof(CornerText), typeof(string), typeof(ClamGridView), "#", propertyChanged: OnStyleChanged);
    public static readonly BindableProperty RowHeaderAlignmentProperty = BindableProperty.Create(nameof(RowHeaderAlignment), typeof(TextAlignment), typeof(ClamGridView), TextAlignment.End, propertyChanged: OnStyleRenderChanged);
    public static readonly BindableProperty TextColorProperty = BindableProperty.Create(nameof(TextColor), typeof(Color), typeof(ClamGridView), Color.FromArgb("#212121"), propertyChanged: OnStyleRenderChanged);
    public static readonly BindableProperty EmptyTextColorProperty = BindableProperty.Create(nameof(EmptyTextColor), typeof(Color), typeof(ClamGridView), Color.FromArgb("#9E9E9E"), propertyChanged: OnStyleRenderChanged);
    public static readonly BindableProperty AlternatingRowBackgroundProperty = BindableProperty.Create(nameof(AlternatingRowBackground), typeof(Color), typeof(ClamGridView), propertyChanged: OnStyleRenderChanged);
    public static readonly BindableProperty HeaderBackgroundProperty = BindableProperty.Create(nameof(HeaderBackground), typeof(Color), typeof(ClamGridView), Color.FromArgb("#1976D2"), propertyChanged: OnStyleRenderChanged);
    public static readonly BindableProperty HeaderTextColorProperty = BindableProperty.Create(nameof(HeaderTextColor), typeof(Color), typeof(ClamGridView), Colors.White, propertyChanged: OnStyleRenderChanged);
    public static readonly BindableProperty RowHeaderBackgroundProperty = BindableProperty.Create(nameof(RowHeaderBackground), typeof(Color), typeof(ClamGridView), Color.FromArgb("#F5F5F5"), propertyChanged: OnStyleRenderChanged);
    public static readonly BindableProperty GridLineColorProperty = BindableProperty.Create(nameof(GridLineColor), typeof(Color), typeof(ClamGridView), Color.FromArgb("#E0E0E0"), propertyChanged: OnStyleRenderChanged);
    public static readonly BindableProperty FrozenLineColorProperty = BindableProperty.Create(nameof(FrozenLineColor), typeof(Color), typeof(ClamGridView), Color.FromArgb("#9E9E9E"), propertyChanged: OnStyleRenderChanged);
    public static readonly BindableProperty ScrollBarColorProperty = BindableProperty.Create(nameof(ScrollBarColor), typeof(Color), typeof(ClamGridView), Color.FromArgb("#8C64748B"), propertyChanged: OnStyleRenderChanged);
    public static readonly BindableProperty SelectedBackgroundProperty = BindableProperty.Create(nameof(SelectedBackground), typeof(Color), typeof(ClamGridView), Color.FromArgb("#BBDEFB"), propertyChanged: OnStyleRenderChanged);
    public static readonly BindableProperty SelectedTextColorProperty = BindableProperty.Create(nameof(SelectedTextColor), typeof(Color), typeof(ClamGridView), Color.FromArgb("#0D47A1"), propertyChanged: OnStyleRenderChanged);
    public static readonly BindableProperty AscendingHeaderBackgroundProperty = BindableProperty.Create(nameof(AscendingHeaderBackground), typeof(Color), typeof(ClamGridView), Color.FromArgb("#0097A7"), propertyChanged: OnStyleRenderChanged);
    public static readonly BindableProperty DescendingHeaderBackgroundProperty = BindableProperty.Create(nameof(DescendingHeaderBackground), typeof(Color), typeof(ClamGridView), Color.FromArgb("#F57C00"), propertyChanged: OnStyleRenderChanged);
    public static readonly BindableProperty AscendingSortMarkProperty = BindableProperty.Create(nameof(AscendingSortMark), typeof(string), typeof(ClamGridView), "↑", propertyChanged: OnStyleFontChanged);
    public static readonly BindableProperty DescendingSortMarkProperty = BindableProperty.Create(nameof(DescendingSortMark), typeof(string), typeof(ClamGridView), "↓", propertyChanged: OnStyleFontChanged);
    public static readonly BindableProperty SortMarkPositionProperty = BindableProperty.Create(nameof(SortMarkPosition), typeof(GridSortMarkPosition), typeof(ClamGridView), GridSortMarkPosition.Start, propertyChanged: OnStyleFontChanged);
    public static readonly BindableProperty SortPriorityVisibleProperty = BindableProperty.Create(nameof(SortPriorityVisible), typeof(bool), typeof(ClamGridView), false, propertyChanged: OnStyleFontChanged);
    public static readonly BindableProperty RowBackgroundProperty = BindableProperty.Create(nameof(RowBackground), typeof(Func<object, Color>), typeof(ClamGridView), propertyChanged: OnStyleRenderChanged);
    public static readonly BindableProperty CellColorsProperty = BindableProperty.Create(nameof(CellColors), typeof(Func<GridCellColorContext, GridColors>), typeof(ClamGridView), propertyChanged: OnStyleRenderChanged);
    public static readonly BindableProperty ColumnHeaderColorsProperty = BindableProperty.Create(nameof(ColumnHeaderColors), typeof(Func<GridColumnHeaderColorContext, GridColors>), typeof(ClamGridView), propertyChanged: OnStyleRenderChanged);
    public static readonly BindableProperty RowHeaderColorsProperty = BindableProperty.Create(nameof(RowHeaderColors), typeof(Func<GridRowHeaderColorContext, GridColors>), typeof(ClamGridView), propertyChanged: OnStyleRenderChanged);
    public static readonly BindableProperty RowHeaderTextProperty = BindableProperty.Create(nameof(RowHeaderText), typeof(Func<GridRowHeaderTextContext, string>), typeof(ClamGridView), propertyChanged: OnStyleRenderChanged);

    public string FontFamily
    {
        get => (string)GetValue(FontFamilyProperty);
        set => SetValue(FontFamilyProperty, value);
    }

    public double FontSize
    {
        get => (double)GetValue(FontSizeProperty);
        set => SetValue(FontSizeProperty, value);
    }

    public double HorizontalPadding
    {
        get => (double)GetValue(HorizontalPaddingProperty);
        set => SetValue(HorizontalPaddingProperty, value);
    }

    public double VerticalPadding
    {
        get => (double)GetValue(VerticalPaddingProperty);
        set => SetValue(VerticalPaddingProperty, value);
    }

    public double? RowHeight
    {
        get => (double?)GetValue(RowHeightProperty);
        set => SetValue(RowHeightProperty, value);
    }

    public double? HeaderHeight
    {
        get => (double?)GetValue(HeaderHeightProperty);
        set => SetValue(HeaderHeightProperty, value);
    }

    public double RowHeaderWidth
    {
        get => (double)GetValue(RowHeaderWidthProperty);
        set => SetValue(RowHeaderWidthProperty, value);
    }

    public double ScrollBarThickness
    {
        get => (double)GetValue(ScrollBarThicknessProperty);
        set => SetValue(ScrollBarThicknessProperty, value);
    }

    public double GridLineWidth
    {
        get => (double)GetValue(GridLineWidthProperty);
        set => SetValue(GridLineWidthProperty, value);
    }

    public bool ColumnHeadersVisible
    {
        get => (bool)GetValue(ColumnHeadersVisibleProperty);
        set => SetValue(ColumnHeadersVisibleProperty, value);
    }

    public bool RowHeadersVisible
    {
        get => (bool)GetValue(RowHeadersVisibleProperty);
        set => SetValue(RowHeadersVisibleProperty, value);
    }

    public bool HorizontalLinesVisible
    {
        get => (bool)GetValue(HorizontalLinesVisibleProperty);
        set => SetValue(HorizontalLinesVisibleProperty, value);
    }

    public bool VerticalLinesVisible
    {
        get => (bool)GetValue(VerticalLinesVisibleProperty);
        set => SetValue(VerticalLinesVisibleProperty, value);
    }

    public bool ScrollBarVisible
    {
        get => (bool)GetValue(ScrollBarVisibleProperty);
        set => SetValue(ScrollBarVisibleProperty, value);
    }

    public string CornerText
    {
        get => (string)GetValue(CornerTextProperty);
        set => SetValue(CornerTextProperty, value);
    }

    public TextAlignment RowHeaderAlignment
    {
        get => (TextAlignment)GetValue(RowHeaderAlignmentProperty);
        set => SetValue(RowHeaderAlignmentProperty, value);
    }

    public Color TextColor
    {
        get => (Color)GetValue(TextColorProperty);
        set => SetValue(TextColorProperty, value);
    }

    public Color EmptyTextColor
    {
        get => (Color)GetValue(EmptyTextColorProperty);
        set => SetValue(EmptyTextColorProperty, value);
    }

    public Color? AlternatingRowBackground
    {
        get => (Color?)GetValue(AlternatingRowBackgroundProperty);
        set => SetValue(AlternatingRowBackgroundProperty, value);
    }

    public Color HeaderBackground
    {
        get => (Color)GetValue(HeaderBackgroundProperty);
        set => SetValue(HeaderBackgroundProperty, value);
    }

    public Color HeaderTextColor
    {
        get => (Color)GetValue(HeaderTextColorProperty);
        set => SetValue(HeaderTextColorProperty, value);
    }

    public Color RowHeaderBackground
    {
        get => (Color)GetValue(RowHeaderBackgroundProperty);
        set => SetValue(RowHeaderBackgroundProperty, value);
    }

    public Color GridLineColor
    {
        get => (Color)GetValue(GridLineColorProperty);
        set => SetValue(GridLineColorProperty, value);
    }

    public Color FrozenLineColor
    {
        get => (Color)GetValue(FrozenLineColorProperty);
        set => SetValue(FrozenLineColorProperty, value);
    }

    public Color ScrollBarColor
    {
        get => (Color)GetValue(ScrollBarColorProperty);
        set => SetValue(ScrollBarColorProperty, value);
    }

    public Color SelectedBackground
    {
        get => (Color)GetValue(SelectedBackgroundProperty);
        set => SetValue(SelectedBackgroundProperty, value);
    }

    public Color SelectedTextColor
    {
        get => (Color)GetValue(SelectedTextColorProperty);
        set => SetValue(SelectedTextColorProperty, value);
    }

    public Color AscendingHeaderBackground
    {
        get => (Color)GetValue(AscendingHeaderBackgroundProperty);
        set => SetValue(AscendingHeaderBackgroundProperty, value);
    }

    public Color DescendingHeaderBackground
    {
        get => (Color)GetValue(DescendingHeaderBackgroundProperty);
        set => SetValue(DescendingHeaderBackgroundProperty, value);
    }

    public string AscendingSortMark
    {
        get => (string)GetValue(AscendingSortMarkProperty);
        set => SetValue(AscendingSortMarkProperty, value);
    }

    public string DescendingSortMark
    {
        get => (string)GetValue(DescendingSortMarkProperty);
        set => SetValue(DescendingSortMarkProperty, value);
    }

    public GridSortMarkPosition SortMarkPosition
    {
        get => (GridSortMarkPosition)GetValue(SortMarkPositionProperty);
        set => SetValue(SortMarkPositionProperty, value);
    }

    public bool SortPriorityVisible
    {
        get => (bool)GetValue(SortPriorityVisibleProperty);
        set => SetValue(SortPriorityVisibleProperty, value);
    }

    public Func<object, Color?>? RowBackground
    {
        get => (Func<object, Color?>?)GetValue(RowBackgroundProperty);
        set => SetValue(RowBackgroundProperty, value);
    }

    public Func<GridCellColorContext, GridColors>? CellColors
    {
        get => (Func<GridCellColorContext, GridColors>?)GetValue(CellColorsProperty);
        set => SetValue(CellColorsProperty, value);
    }

    public Func<GridColumnHeaderColorContext, GridColors>? ColumnHeaderColors
    {
        get => (Func<GridColumnHeaderColorContext, GridColors>?)GetValue(ColumnHeaderColorsProperty);
        set => SetValue(ColumnHeaderColorsProperty, value);
    }

    public Func<GridRowHeaderColorContext, GridColors>? RowHeaderColors
    {
        get => (Func<GridRowHeaderColorContext, GridColors>?)GetValue(RowHeaderColorsProperty);
        set => SetValue(RowHeaderColorsProperty, value);
    }

    public Func<GridRowHeaderTextContext, string?>? RowHeaderText
    {
        get => (Func<GridRowHeaderTextContext, string?>?)GetValue(RowHeaderTextProperty);
        set => SetValue(RowHeaderTextProperty, value);
    }

    internal GridStyle EffectiveStyle
    {
        get => field ??= BuildStyle();
        private set;
    }

    internal static void OnStyleFontChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var grid = (ClamGridView)bindable;
        grid.RequireUiThread();
        grid.EffectiveStyle = null!;
        grid.renderer?.Dispose();
        grid.renderer = null;
        grid.CancelInput();
        grid.InvalidateLayout();
    }

    internal static void OnStyleChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var grid = (ClamGridView)bindable;
        grid.RequireUiThread();
        grid.RefreshStyle();
        grid.CancelInput();
        grid.InvalidateLayout();
    }

    internal static void OnStyleRenderChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var grid = (ClamGridView)bindable;
        grid.RequireUiThread();
        grid.RefreshStyle();
        grid.InvalidateSurface();
    }

    private static bool ValidatePositive(object? value, string name) =>
        (value is not double number) || (Double.IsFinite(number) && (number > 0)) ? true : throw new ArgumentOutOfRangeException(name, value, "The value must be a positive finite value.");

    private static bool ValidateSize(object? value, string name) =>
        (value is not double number) || (Double.IsFinite(number) && (number >= 0)) ? true : throw new ArgumentOutOfRangeException(name, value, "The value must be a finite value of zero or more.");

    private void RefreshStyle()
    {
        EffectiveStyle = null!;
        renderer?.UpdateStyle(EffectiveStyle);
    }

    private GridStyle BuildStyle() =>
        new()
        {
            FontFamily = FontFamily,
            FontSize = FontSize,
            HorizontalPadding = HorizontalPadding,
            VerticalPadding = VerticalPadding,
            RowHeight = RowHeight,
            HeaderHeight = HeaderHeight,
            RowHeaderWidth = RowHeaderWidth,
            ScrollBarThickness = ScrollBarThickness,
            GridLineWidth = GridLineWidth,
            ColumnHeadersVisible = ColumnHeadersVisible,
            RowHeadersVisible = RowHeadersVisible,
            HorizontalLinesVisible = HorizontalLinesVisible,
            VerticalLinesVisible = VerticalLinesVisible,
            ScrollBarVisible = ScrollBarVisible,
            CornerText = CornerText,
            RowHeaderAlignment = RowHeaderAlignment,
            TextColor = TextColor,
            EmptyTextColor = EmptyTextColor,
            Background = BackgroundColor ?? Colors.White,
            AlternatingRowBackground = AlternatingRowBackground,
            HeaderBackground = HeaderBackground,
            HeaderTextColor = HeaderTextColor,
            RowHeaderBackground = RowHeaderBackground,
            GridLineColor = GridLineColor,
            FrozenLineColor = FrozenLineColor,
            ScrollBarColor = ScrollBarColor,
            SelectedBackground = SelectedBackground,
            SelectedTextColor = SelectedTextColor,
            AscendingHeaderBackground = AscendingHeaderBackground,
            DescendingHeaderBackground = DescendingHeaderBackground,
            AscendingSortMark = AscendingSortMark,
            DescendingSortMark = DescendingSortMark,
            SortMarkPosition = SortMarkPosition,
            SortPriorityVisible = SortPriorityVisible,
            RowBackground = RowBackground,
            CellColors = CellColors,
            ColumnHeaderColors = ColumnHeaderColors,
            RowHeaderColors = RowHeaderColors,
            RowHeaderText = RowHeaderText
        };
}
