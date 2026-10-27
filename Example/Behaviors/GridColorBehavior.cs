namespace Example.Behaviors;

using Smart.Maui.Interactivity;

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

        Apply(bindable);
    }

    private static void HandleColorPropertyChanged(BindableObject bindable, object? oldValue, object? newValue)
    {
        var behavior = (GridColorBehavior)bindable;
        if (behavior.AssociatedObject is { } grid)
        {
            behavior.Apply(grid);
        }
    }

    private void Apply(ClamGridView grid)
    {
        if (ColorSelector is { } selector)
        {
            grid.RowBackground = selector.Resolve;
        }

        if (SelectedBackgroundColor is { } background)
        {
            grid.SelectedBackground = background;
        }

        if (SelectedTextColor is { } text)
        {
            grid.SelectedTextColor = text;
        }
    }
}
