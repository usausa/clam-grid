namespace Example.Modules.Grid;

public sealed partial class QualityVerifier
{
    // XAML と同じ順序（ColumnOrders → 列定義を1件ずつ追加 → ValueAccessors）で構成したときの動作と、ColumnOrders の TwoWay バインディングを検証する。
    private async Task VerifyBindingAsync()
    {
        report("列定義と列設定バインディングの検証");
        grid.ItemsSource = null;
        data?.Dispose();
        var scenario = ListScenario.All[0];
        data = scenario.CreateData(30);
        var accessors = new GridValueAccessorCollection<TicketRow>();
        foreach (var column in scenario.Columns)
        {
            accessors.Add(column.Key, row => row.GetText(column.Key));
        }

        accessors.Add("GroupId", static row => row.GetText("GroupId"));

        var orders = new OrderSource { Orders = [new("DeptCode", true), new("unknown", true)] };
        await NextFrameAsync(() =>
        {
            grid.ValueAccessors = null;
            grid.ConfigureColumns([]);
            grid.GridStyle = ScenarioGridFactory.CreateStyle(scenario);
            grid.ItemsSource = data;
            // 定義より先に表示・順序をバインドしても、定義の追加時に適用される
            grid.SetBinding(ClamGridView.ColumnOrdersProperty, static (OrderSource source) => source.Orders, BindingMode.TwoWay, source: orders);
            foreach (var column in scenario.Columns)
            {
                grid.ColumnDefinitions.Add(new GridColumn { Key = column.Key, Header = column.Header, Width = GridColumnWidth.Absolute(column.Width) });
            }
        }).ConfigureAwait(true);
        Check("xaml: orders set before definitions are applied", (grid.Columns.Count == 1) && (grid.Columns[0].Key == "DeptCode") && (grid.ColumnDefinitions.Count == scenario.Columns.Count));
        Check("xaml: unresolved column renders empty", grid.Columns[0].ValueAccessor.GetValue(data[0]) is null);
        Check("binding: normalized orders are written back to the source", (orders.Orders?.Count == scenario.Columns.Count) && (orders.Orders[0] == new GridColumnOrder("DeptCode", true)) && orders.Orders.Skip(1).All(static order => !order.IsVisible) && ReferenceEquals(orders.Orders, grid.ColumnOrders));

        var frame = await NextFrameAsync(() => grid.ValueAccessors = accessors).ConfigureAwait(true);
        Check("xaml: value accessors are resolved by key", (frame.RenderedCells > 0) && grid.ColumnDefinitions.All(column => column.ValueAccessor.GetValue(data[0]) is string));

        await NextFrameAsync(() => orders.Orders = [new("CustomerName", true), new("DeptCode", true)]).ConfigureAwait(true);
        Check("binding: source change reorders columns", grid.Columns.Select(static column => column.Key).SequenceEqual(["CustomerName", "DeptCode"]));

        await NextFrameAsync(() => grid.ApplyColumnOrders(null)).ConfigureAwait(true);
        Check("binding: grid side reset reaches the source", (grid.Columns.Count == scenario.Columns.Count) && (orders.Orders?.All(static order => order.IsVisible) ?? false));

        await NextFrameAsync(() => orders.Orders = [new("DeptCode", true)]).ConfigureAwait(true);
        Check("binding: two way binding survives grid side updates", (grid.Columns.Count == 1) && (orders.Orders?.Count == scenario.Columns.Count));

        var session = grid.CreateColumnEditSession();
        var args = new GridColumnConfigurationEventArgs(grid.ColumnDefinitions.ToArray(), null, grid.ColumnOrders).CreateEditSession();
        Check("binding: edit session from definitions and orders", session.Columns.Select(static column => (column.Key, column.IsVisible)).SequenceEqual(args.Columns.Select(static column => (column.Key, column.IsVisible))) && (session.Columns[0].Key == "DeptCode") && session.Columns[0].IsVisible && !session.Columns[1].IsVisible);

        await NextFrameAsync(() => grid.ColumnDefinitions.Add(new GridColumn { Key = "GroupId", Header = "案件グループ" })).ConfigureAwait(true);
        Check("xaml: added definition keeps orders and is hidden until enabled", (grid.Columns.Count == 1) && (orders.Orders?.Count == scenario.Columns.Count + 1) && (orders.Orders[^1] == new GridColumnOrder("GroupId", false)) && grid.ColumnDefinitions[^1].ValueAccessor.GetValue(data[0]) is string);

        await NextFrameAsync(() => grid.Columns[0] = grid.Columns[0] with { Width = GridColumnWidth.Absolute(321) }).ConfigureAwait(true);
        Check("xaml: resized width is reflected to the definition", grid.ColumnDefinitions.First(static column => column.Key == "DeptCode").Width.Equals(GridColumnWidth.Absolute(321)) && (orders.Orders?.Count == scenario.Columns.Count + 1));

        var missing = false;
        try
        {
            grid.ColumnDefinitions.Add(new GridColumn { Key = "Missing", Header = "?" });
        }
        catch (InvalidOperationException)
        {
            missing = true;
        }

        Check("xaml: unknown key is reported when accessors are set", missing);
        grid.RemoveBinding(ClamGridView.ColumnOrdersProperty);
        grid.ValueAccessors = null;
        grid.ItemsSource = null;
    }
}

// Binding.Create のソース生成はラムダ引数の型に internal 以上の可視性を要求する
internal sealed class OrderSource : NotificationObject
{
    public IReadOnlyList<GridColumnOrder>? Orders
    {
        get;
        set => SetProperty(ref field, value);
    }
}
