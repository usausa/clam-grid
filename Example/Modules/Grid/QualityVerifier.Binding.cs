namespace Example.Modules.Grid;

public sealed partial class QualityVerifier
{
    // Verifies the XAML construction order (ColumnStates, then definitions one by one, then ValueAccessors) and the TwoWay ColumnStates binding
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
            grid.ColumnStates = null;
            grid.ColumnDefinitions.Clear();
            grid.GridStyle = ScenarioGridFactory.CreateStyle(scenario);
            grid.ItemsSource = data;
            // Orders bound before the definitions are applied when the definitions are added
            grid.SetBinding(ClamGridView.ColumnStatesProperty, static (OrderSource source) => source.Orders, BindingMode.TwoWay, source: orders);
            foreach (var column in scenario.Columns)
            {
                grid.ColumnDefinitions.Add(new GridColumn { Key = column.Key, Header = column.Header, Width = GridColumnWidth.Absolute(column.Width), Format = "N0" });
            }
        }).ConfigureAwait(true);
        Check("xaml: orders set before definitions are applied", (grid.VisibleColumns().Count == 1) && (grid.VisibleColumns()[0].Key == "DeptCode") && (grid.ColumnDefinitions.Count == scenario.Columns.Count));
        Check("xaml: unresolved column renders empty", grid.VisibleColumns()[0].ValueAccessor.GetValue(data[0]) is null);
        Check("binding: normalized orders are written back to the source", (orders.Orders?.Count == scenario.Columns.Count) && (orders.Orders[0] == new GridColumnState("DeptCode", true)) && orders.Orders.Skip(1).All(static order => !order.IsVisible) && ReferenceEquals(orders.Orders, grid.ColumnStates));

        var frame = await NextFrameAsync(() => grid.ValueAccessors = accessors).ConfigureAwait(true);
        Check("xaml: value accessors are resolved by key", (frame.RenderedCells > 0) && grid.ColumnDefinitions.All(column => column.ValueAccessor.GetValue(data[0]) is string));
        Check("xaml: format survives accessor resolution", grid.VisibleColumns().All(static column => column.Format == "N0"));

        await NextFrameAsync(() => orders.Orders = [new("CustomerName", true), new("DeptCode", true)]).ConfigureAwait(true);
        Check("binding: source change reorders columns", grid.VisibleColumns().Select(static column => column.Key).SequenceEqual(["CustomerName", "DeptCode"]));

        await NextFrameAsync(() => grid.ColumnStates = null).ConfigureAwait(true);
        Check("binding: grid side reset reaches the source", (grid.VisibleColumns().Count == scenario.Columns.Count) && (orders.Orders?.All(static order => order.IsVisible) ?? false));

        await NextFrameAsync(() => orders.Orders = [new("DeptCode", true)]).ConfigureAwait(true);
        Check("binding: two way binding survives grid side updates", (grid.VisibleColumns().Count == 1) && (orders.Orders?.Count == scenario.Columns.Count));

        var session = grid.CreateColumnEditSession();
        var args = new GridColumnConfigurationEventArgs(grid.ColumnDefinitions.ToArray(), null, grid.ColumnStates).CreateEditSession();
        Check("binding: edit session from definitions and orders", session.Columns.Select(static column => (column.Key, column.IsVisible)).SequenceEqual(args.Columns.Select(static column => (column.Key, column.IsVisible))) && (session.Columns[0].Key == "DeptCode") && session.Columns[0].IsVisible && !session.Columns[1].IsVisible);

        await NextFrameAsync(() => grid.ColumnDefinitions.Add(new GridColumn { Key = "GroupId", Header = "案件グループ" })).ConfigureAwait(true);
        Check("xaml: added definition keeps orders and is hidden until enabled", (grid.VisibleColumns().Count == 1) && (orders.Orders?.Count == scenario.Columns.Count + 1) && (orders.Orders[^1] == new GridColumnState("GroupId", false)) && grid.ColumnDefinitions[^1].ValueAccessor.GetValue(data[0]) is string);

        await NextFrameAsync(() =>
        {
            var target = grid.ColumnDefinitions.First(static column => column.Key == "DeptCode");
            grid.ColumnDefinitions[grid.ColumnDefinitions.IndexOf(target)] = target with { Width = GridColumnWidth.Absolute(321) };
        }).ConfigureAwait(true);
        Check("xaml: a definition edit changes the width", grid.ColumnDefinitions.First(static column => column.Key == "DeptCode").Width.Equals(GridColumnWidth.Absolute(321)) && (orders.Orders?.Count == scenario.Columns.Count + 1));

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
        var sorts = new OrderSource { Sorts = [new("DeptCode", true), new("Unknown")] };
        using var other = scenario.CreateData(10);
        await NextFrameAsync(() =>
        {
            grid.ItemsSource = null;
            grid.SetBinding(ClamGridView.SortOrdersProperty, static (OrderSource source) => source.Sorts, BindingMode.TwoWay, source: sorts);
            grid.ItemsSource = data;
        }).ConfigureAwait(true);
        Check("binding: sort orders set before the data view are applied and written back", data.SortOrders.SequenceEqual([new GridSortOrder("DeptCode", true)]) && sorts.Sorts is { Count: 1 } applied && ReferenceEquals(applied, grid.SortOrders));
        await NextFrameAsync(() => data.SortBy("CustomerName")).ConfigureAwait(true);
        Check("binding: grid side sort reaches the source", sorts.Sorts is { Count: 2 } written && (written[0].Key == "CustomerName"));
        await NextFrameAsync(() => sorts.Sorts = []).ConfigureAwait(true);
        Check("binding: empty sort orders clear the sort", (data.SortOrders.Count == 0) && sorts.Sorts is { Count: 0 });
        await NextFrameAsync(() =>
        {
            sorts.Sorts = [new("DeptCode")];
            grid.ItemsSource = other;
        }).ConfigureAwait(true);
        Check("binding: sort orders follow a new data view", other.SortOrders.SequenceEqual([new GridSortOrder("DeptCode")]) && (data.SortOrders.Count == 1));
        grid.SortCycle = GridSortCycle.AscendingDescendingNone;
        grid.SortByColumn(grid.VisibleColumns()[0].Key);
        Check("cycle: second tap sorts descending", other.SortOrders[0] == new GridSortOrder("DeptCode", true));
        grid.SortByColumn(grid.VisibleColumns()[0].Key);
        Check("cycle: third tap removes the key and reaches the source", (other.SortOrders.Count == 0) && sorts.Sorts is { Count: 0 });
        grid.SortCycle = GridSortCycle.DescendingAscending;
        grid.SortByColumn(grid.VisibleColumns()[0].Key);
        Check("cycle: descending first", other.SortOrders[0] == new GridSortOrder("DeptCode", true));
        grid.SortCycle = GridSortCycle.AscendingDescending;
        grid.RemoveBinding(ClamGridView.SortOrdersProperty);
        grid.RemoveBinding(ClamGridView.ColumnStatesProperty);
        grid.ValueAccessors = null;
        grid.ItemsSource = null;
    }
}

// The Binding.Create source generator requires the lambda parameter type to be at least internal
internal sealed class OrderSource : NotificationObject
{
    public IReadOnlyList<GridColumnState>? Orders
    {
        get;
        set => SetProperty(ref field, value);
    }

    public IReadOnlyList<GridSortOrder>? Sorts
    {
        get;
        set => SetProperty(ref field, value);
    }
}
