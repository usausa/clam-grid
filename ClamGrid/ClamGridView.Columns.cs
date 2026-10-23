namespace ClamGrid;

using System.Diagnostics.CodeAnalysis;

using ClamGrid.Columns;

[ContentProperty(nameof(ColumnDefinitions))]
public partial class ClamGridView
{
    public static readonly BindableProperty ValueAccessorsProperty = BindableProperty.Create(nameof(ValueAccessors), typeof(IGridValueAccessorProvider), typeof(ClamGridView), propertyChanged: OnValueAccessorsChanged);
    public static readonly BindableProperty FrozenColumnCountProperty = BindableProperty.Create(nameof(FrozenColumnCount), typeof(int), typeof(ClamGridView), 0, propertyChanged: OnFrozenColumnCountChanged);
    public static readonly BindableProperty ColumnStatesProperty = BindableProperty.Create(nameof(ColumnStates), typeof(IReadOnlyList<GridColumnState>), typeof(ClamGridView), defaultBindingMode: BindingMode.TwoWay, propertyChanged: OnColumnStatesChanged);

    private GridColumn[] columnCatalog = [];
    private GridColumnState[] columnStates = [];
    private GridColumnState[]? requestedStates;
    private GridColumn[] visibleColumns = [];
    private bool columnsDirty = true;
    private bool publishingStates;

    // All column definitions including hidden ones, declared as XAML content
    public GridColumnCollection ColumnDefinitions { get; } = [];

    // Visible columns in display order, built from the definitions and the states
    internal IReadOnlyList<GridColumn> ArrangedColumns
    {
        get
        {
            EnsureColumns();
            return visibleColumns;
        }
    }

    // Resolves the value accessor of columns declared without one by key
    public IGridValueAccessorProvider? ValueAccessors
    {
        get => (IGridValueAccessorProvider?)GetValue(ValueAccessorsProperty);
        set => SetValue(ValueAccessorsProperty, value);
    }

    // Number of leading columns that stay in place while the others scroll horizontally
    public int FrozenColumnCount
    {
        get => (int)GetValue(FrozenColumnCountProperty);
        set => SetValue(FrozenColumnCountProperty, value);
    }

    // Visibility, order and saved width of all columns; the normalized value is written back for TwoWay bindings and null restores the default
    [AllowNull]
    public IReadOnlyList<GridColumnState> ColumnStates
    {
        get
        {
            EnsureColumns();
            return (IReadOnlyList<GridColumnState>?)GetValue(ColumnStatesProperty) ?? Array.AsReadOnly(columnStates);
        }
        set => SetValue(ColumnStatesProperty, value);
    }

    public GridColumnEditSession CreateColumnEditSession()
    {
        RequireUiThread();
        EnsureColumns();
        return GridColumnEditSession.Create(columnCatalog, columnStates);
    }

    private static void OnColumnStatesChanged(BindableObject bindable, object? oldValue, object? newValue)
    {
        var grid = (ClamGridView)bindable;
        if (grid.publishingStates)
        {
            return;
        }

        grid.requestedStates = (newValue as IEnumerable<GridColumnState>)?.ToArray();
        grid.InvalidateColumns();
    }

    private static void OnFrozenColumnCountChanged(BindableObject bindable, object? oldValue, object? newValue)
    {
        var grid = (ClamGridView)bindable;
        grid.RequireUiThread();
        grid.CancelInput();
        grid.InvalidateLayout();
    }

    private static void OnValueAccessorsChanged(BindableObject bindable, object? oldValue, object? newValue)
    {
        var grid = (ClamGridView)bindable;
        if (grid.ResolveValueAccessors(grid.ColumnDefinitions))
        {
            grid.InvalidateColumns();
        }
    }

    // Accessors are resolved at once so an unregistered key is reported where the column is declared
    private void OnColumnDefinitionsChanged(object? sender, EventArgs e)
    {
        RequireUiThread();
        ResolveValueAccessors(ColumnDefinitions);
        InvalidateColumns();
    }

    private void InvalidateColumns()
    {
        columnsDirty = true;
        InvalidateLayout();
    }

    // Definitions and states are folded into a single normalization, so declaring columns in XAML arranges them once
    private void EnsureColumns()
    {
        if (!columnsDirty || disposed)
        {
            return;
        }

        columnsDirty = false;
        var catalog = ColumnDefinitions.ToArray();
        var states = GridColumnSettings.Normalize(catalog, requestedStates);
        ResolveValueAccessors(catalog);
        columnCatalog = catalog;
        columnStates = states;
        visibleColumns = GridColumnSettings.Arrange(catalog, states);
        PublishColumnStates();
    }

    // A dragged width is saved in the column states, so the definitions keep their declared width
    private void SetColumnWidth(string key, double width)
    {
        EnsureColumns();
        requestedStates = columnStates.Select(state => state.Key == key ? state with { Width = width } : state).ToArray();
        InvalidateColumns();
    }

    // Writes the value back as a control side change so OneWay bindings survive and TwoWay bindings update the source
    private void PublishColumnStates()
    {
        if ((GetValue(ColumnStatesProperty) is IReadOnlyList<GridColumnState> current) && current.SequenceEqual(columnStates))
        {
            return;
        }

        publishingStates = true;
        try
        {
            SetValueFromRenderer(ColumnStatesProperty, Array.AsReadOnly(columnStates));
        }
        finally
        {
            publishingStates = false;
        }
    }

    private bool ResolveValueAccessors(IEnumerable<GridColumn> catalog)
    {
        if (ValueAccessors is not { } provider)
        {
            return false;
        }

        var resolved = false;
        foreach (var column in catalog)
        {
            if (!column.IsResolved)
            {
                column.ValueAccessor = provider.GetValueAccessor(column.Key) ?? throw new InvalidOperationException($"The value accessor is not registered. key=[{column.Key}]");
                resolved = true;
            }
        }

        return resolved;
    }
}
