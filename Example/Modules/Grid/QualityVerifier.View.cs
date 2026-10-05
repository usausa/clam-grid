namespace Example.Modules.Grid;

public sealed partial class QualityVerifier
{
    private async Task VerifyViewAsync()
    {
        report("表示検証: 絞り込み・保存した列幅");
        grid.ItemsSource = null;
        data?.Dispose();
        var scenario = ListScenario.All[0];
        var view = scenario.CreateData(1000);
        data = view;
        await NextFrameAsync(() => ScenarioGridFactory.Configure(grid, scenario, view)).ConfigureAwait(true);

        await NextFrameAsync(() => grid.ScrollTo(0, Double.MaxValue)).ConfigureAwait(true);
        var frame = await NextFrameAsync(() => view.Filter = static row => (row.Id % 10) == 0).ConfigureAwait(true);
        Check("filter: grid rows follow the filter", (grid.RowCount == 100) && (view.SourceCount == 1000));
        Check("filter: scroll is clamped to the kept rows", frame.Rows.End == 100);
        await NextFrameAsync(() => view.Filter = null).ConfigureAwait(true);
        Check("filter: clearing restores all rows", grid.RowCount == 1000);

        var key = grid.Columns[1].Key;
        var declared = grid.Columns[1].Width;
        await NextFrameAsync(() => grid.ColumnOrders = grid.ColumnOrders.Select(order => order.Key == key ? order with { Width = 222.5 } : order).ToArray()).ConfigureAwait(true);
        Check("widths: saved width applies to the column", grid.Columns[1].Width.Equals(GridColumnWidth.Absolute(222.5)) && grid.ColumnDefinitions.First(column => column.Key == key).Width.Equals(declared));
        await NextFrameAsync(() => grid.ColumnOrders = null).ConfigureAwait(true);
        Check("widths: default orders restore the declared width", grid.Columns[1].Width.Equals(declared) && grid.ColumnOrders.All(static order => order.Width is null));
    }
}
