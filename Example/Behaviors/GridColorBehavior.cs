namespace Example.Behaviors;

using Smart.Maui.Interactivity;

// 行データの状態に応じた背景色と選択色をGridStyleへ合成する。
public sealed class GridColorBehavior : BehaviorBase<ClamGridView>
{
    public static readonly BindableProperty ColorSelectorProperty = BindableProperty.Create(
        nameof(ColorSelector),
        typeof(IColorSelector),
        typeof(GridColorBehavior),
        propertyChanged: HandleColorPropertyChanged);

    public static readonly BindableProperty SelectedBackgroundColorProperty = BindableProperty.Create(
        nameof(SelectedBackgroundColor),
        typeof(Color),
        typeof(GridColorBehavior),
        propertyChanged: HandleColorPropertyChanged);

    public static readonly BindableProperty SelectedTextColorProperty = BindableProperty.Create(
        nameof(SelectedTextColor),
        typeof(Color),
        typeof(GridColorBehavior),
        propertyChanged: HandleColorPropertyChanged);

    private bool applying;

    public IColorSelector? ColorSelector
    {
        get => (IColorSelector?)GetValue(ColorSelectorProperty);
        set => SetValue(ColorSelectorProperty, value);
    }

    public Color? SelectedBackgroundColor
    {
        get => (Color?)GetValue(SelectedBackgroundColorProperty);
        set => SetValue(SelectedBackgroundColorProperty, value);
    }

    public Color? SelectedTextColor
    {
        get => (Color?)GetValue(SelectedTextColorProperty);
        set => SetValue(SelectedTextColorProperty, value);
    }

    protected override void OnAttachedTo(ClamGridView bindable)
    {
        base.OnAttachedTo(bindable);

        bindable.PropertyChanged += GridOnPropertyChanged;
        Apply(bindable);
    }

    protected override void OnDetachingFrom(ClamGridView bindable)
    {
        bindable.PropertyChanged -= GridOnPropertyChanged;

        base.OnDetachingFrom(bindable);
    }

    private static void HandleColorPropertyChanged(BindableObject bindable, object? oldValue, object? newValue)
    {
        var behavior = (GridColorBehavior)bindable;
        if (behavior.AssociatedObject is { } grid)
        {
            behavior.Apply(grid);
        }
    }

    private void GridOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        // スタイルが差し替えられても色の設定を維持する
        if (!applying && (e.PropertyName == nameof(ClamGridView.GridStyle)) && (AssociatedObject is { } grid))
        {
            Apply(grid);
        }
    }

    private void Apply(ClamGridView grid)
    {
        applying = true;
        try
        {
            var style = grid.GridStyle;
            grid.GridStyle = style with
            {
                RowBackground = ColorSelector is { } selector ? selector.Resolve : style.RowBackground,
                SelectedBackground = SelectedBackgroundColor ?? style.SelectedBackground,
                SelectedTextColor = SelectedTextColor ?? style.SelectedTextColor
            };
        }
        finally
        {
            applying = false;
        }
    }
}
