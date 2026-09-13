namespace ClamGrid;

using System.Diagnostics.CodeAnalysis;

using ClamGrid.Columns;

[ContentProperty(nameof(ColumnDefinitions))]
public partial class ClamGridView
{
    public static readonly BindableProperty ValueAccessorsProperty = BindableProperty.Create(nameof(ValueAccessors), typeof(IGridValueAccessorProvider), typeof(ClamGridView), propertyChanged: OnValueAccessorsChanged);
    public static readonly BindableProperty ColumnOrdersProperty = BindableProperty.Create(nameof(ColumnOrders), typeof(IReadOnlyList<GridColumnOrder>), typeof(ClamGridView), defaultBindingMode: BindingMode.TwoWay, propertyChanged: OnColumnOrdersChanged);

    private GridColumn[] columnCatalog = [];
    private GridColumnOrder[] columnOrders = [];
    private GridColumnOrder[]? requestedOrders;
    private bool applyingColumns;

    // 非表示列を含む全列の定義。XAML ではコンテンツとして列を並べる。
    public GridColumnCollection ColumnDefinitions { get; } = [];

    // ValueAccessor を持たない列（XAML で宣言した列）の値アクセサを Key から解決する。
    public IGridValueAccessorProvider? ValueAccessors
    {
        get => (IGridValueAccessorProvider?)GetValue(ValueAccessorsProperty);
        set => SetValue(ValueAccessorsProperty, value);
    }

    // 全列の表示と順序。適用後は正規化した値に置き換わり、TwoWay バインディングでは ViewModel へ戻る。null は既定の表示。
    [AllowNull]
    public IReadOnlyList<GridColumnOrder> ColumnOrders
    {
        get => (IReadOnlyList<GridColumnOrder>?)GetValue(ColumnOrdersProperty) ?? Array.AsReadOnly(columnOrders);
        set => SetValue(ColumnOrdersProperty, value);
    }

    public void ConfigureColumns(IEnumerable<GridColumn> definitions, IEnumerable<GridColumnOrder>? orders = null)
    {
        RequireUiThread();
        ArgumentNullException.ThrowIfNull(definitions);
        var catalog = definitions.ToArray();
        requestedOrders = orders?.ToArray();
        applyingColumns = true;
        try
        {
            ColumnDefinitions.ReplaceAll(catalog);
        }
        finally
        {
            applyingColumns = false;
        }

        SetColumnConfiguration(catalog, GridColumnSettings.Normalize(catalog.Select(static column => column.Key), requestedOrders));
    }

    public void ApplyColumnOrders(IEnumerable<GridColumnOrder>? orders)
    {
        RequireUiThread();
        requestedOrders = orders?.ToArray();
        SetColumnConfiguration(columnCatalog, GridColumnSettings.Normalize(columnCatalog.Select(static column => column.Key), requestedOrders));
    }

    public GridColumnEditSession CreateColumnEditSession()
    {
        RequireUiThread();
        return GridColumnEditSession.Create(columnCatalog, columnOrders);
    }

    private static void OnColumnOrdersChanged(BindableObject bindable, object? oldValue, object? newValue)
    {
        var grid = (ClamGridView)bindable;
        if (grid.applyingColumns)
        {
            return;
        }

        // 定義より先に設定された場合は定義の到着時に適用する
        grid.requestedOrders = (newValue as IEnumerable<GridColumnOrder>)?.ToArray();
        if (grid.columnCatalog.Length == 0)
        {
            return;
        }

        var normalized = GridColumnSettings.Normalize(grid.columnCatalog.Select(static column => column.Key), grid.requestedOrders);
        if (!normalized.SequenceEqual(grid.columnOrders))
        {
            grid.SetColumnConfiguration(grid.columnCatalog, normalized);
        }
    }

    private static void OnValueAccessorsChanged(BindableObject bindable, object? oldValue, object? newValue)
    {
        var grid = (ClamGridView)bindable;
        if (grid.ResolveValueAccessors(grid.columnCatalog))
        {
            grid.OnColumnsChanged(grid, EventArgs.Empty);
        }
    }

    private void OnColumnDefinitionsChanged(object? sender, EventArgs e)
    {
        if (applyingColumns)
        {
            return;
        }

        RequireUiThread();
        var catalog = ColumnDefinitions.ToArray();
        SetColumnConfiguration(catalog, GridColumnSettings.Normalize(catalog.Select(static column => column.Key), requestedOrders));
    }

    private void SetColumnConfiguration(GridColumn[] catalog, GridColumnOrder[] orders)
    {
        ResolveValueAccessors(catalog);
        var map = catalog.ToDictionary(static column => column.Key, StringComparer.Ordinal);
        var visible = orders.Where(static order => order.IsVisible).Select(order => map[order.Key]).ToArray();
        columnCatalog = catalog;
        columnOrders = orders;
        applyingColumns = true;
        try
        {
            Columns.ReplaceAll(visible);
            PublishColumnOrders();
        }
        finally
        {
            applyingColumns = false;
        }

        EnsureLayout();
    }

    // Columns が直接編集されたとき、定義と表示・順序を追従させる。
    private void SynchronizeColumnCatalog()
    {
        if (applyingColumns)
        {
            return;
        }

        applyingColumns = true;
        try
        {
            if (Columns.Select(static column => column.Key).SequenceEqual(columnOrders.Where(static order => order.IsVisible).Select(static order => order.Key)))
            {
                // 列幅の変更など。定義側の同じ列を差し替える
                var visible = Columns.ToDictionary(static column => column.Key, StringComparer.Ordinal);
                columnCatalog = columnCatalog.Select(column => visible.GetValueOrDefault(column.Key, column)).ToArray();
                for (var i = 0; i < columnCatalog.Length; i++)
                {
                    if ((i < ColumnDefinitions.Count) && !ReferenceEquals(ColumnDefinitions[i], columnCatalog[i]))
                    {
                        ColumnDefinitions[i] = columnCatalog[i];
                    }
                }
            }
            else
            {
                columnCatalog = Columns.ToArray();
                columnOrders = columnCatalog.Select(static column => new GridColumnOrder(column.Key, true)).ToArray();
                requestedOrders = columnOrders;
                ColumnDefinitions.ReplaceAll(columnCatalog);
                PublishColumnOrders();
            }
        }
        finally
        {
            applyingColumns = false;
        }

        ResolveValueAccessors(columnCatalog);
    }

    // コントロール側の変更としてプロパティへ書き戻す。OneWay バインディングは維持され、TwoWay ではソースへ反映される。
    private void PublishColumnOrders() => SetValueFromRenderer(ColumnOrdersProperty, Array.AsReadOnly(columnOrders));

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
