namespace Example.Modules.Grid;

public sealed partial class QualityVerifier
{
    private async Task VerifyViewAsync()
    {
        report("表示検証: 絞り込み・スクロール位置・保存した列幅・複数行の見出し");
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
        Check("filter: hidden key is not scrolled to", !grid.ScrollIntoViewByKey(1));
        await NextFrameAsync(() => view.Filter = null).ConfigureAwait(true);
        Check("filter: clearing restores all rows", grid.RowCount == 1000);

        frame = await NextFrameAsync(() => grid.ScrollIntoView(500, null, ScrollToPosition.Center)).ConfigureAwait(true);
        Check("scroll: center places the row in the middle", Math.Abs(((frame.Rows.Start + frame.Rows.End) / 2d) - 500.5) <= 1);
        frame = await NextFrameAsync(() => grid.ScrollIntoView(300, null, ScrollToPosition.Start)).ConfigureAwait(true);
        Check("scroll: start aligns the row with the top", frame.Rows.Start == 300);
        await NextFrameAsync(() => grid.ScrollIntoView(300, null, ScrollToPosition.End)).ConfigureAwait(true);
        Check("scroll: end aligns the row with the bottom", grid.HitTest(10, grid.Height - 1).RowIndex == 300);
        await NextFrameAsync(() => grid.ScrollTo(150, 0)).ConfigureAwait(true);
        var offset = grid.ScrollX;
        frame = await NextFrameAsync(() => grid.ScrollIntoView(700)).ConfigureAwait(true);
        Check("scroll: row only keeps the horizontal offset", (offset > 0) && grid.ScrollX.Equals(offset) && (frame.Rows.Start <= 700) && (frame.Rows.End > 700));
        frame = await NextFrameAsync(() => grid.ScrollIntoViewByKey(42, ScrollToPosition.Start)).ConfigureAwait(true);
        Check("scroll: key moves only vertically", (frame.Rows.Start == view.IndexOfKey(42)) && grid.ScrollX.Equals(offset));
        view.ClearSelection();
        frame = await NextFrameAsync(() => grid.TryToggleSelectionByKey(800, out _)).ConfigureAwait(true);
        Check("scroll: selection by key keeps the horizontal offset", (view.SelectedCount == 1) && grid.ScrollX.Equals(offset) && (frame.Rows.Start <= view.IndexOfKey(800)) && (frame.Rows.End > view.IndexOfKey(800)));

        var key = grid.Columns[1].Key;
        var declared = grid.Columns[1].Width;
        await NextFrameAsync(() => grid.ColumnOrders = grid.ColumnOrders.Select(order => order.Key == key ? order with { Width = 222.5 } : order).ToArray()).ConfigureAwait(true);
        Check("widths: saved width applies to the column", grid.Columns[1].Width.Equals(GridColumnWidth.Absolute(222.5)) && grid.ColumnDefinitions.First(column => column.Key == key).Width.Equals(declared));
        await NextFrameAsync(() => grid.ColumnOrders = null).ConfigureAwait(true);
        Check("widths: default orders restore the declared width", grid.Columns[1].Width.Equals(declared) && grid.ColumnOrders.All(static order => order.Width is null));

        var lastKey = grid.ColumnDefinitions[^1].Key;
        var single = HeaderBottom();
        await NextFrameAsync(() => grid.ColumnDefinitions[^1] = grid.ColumnDefinitions[^1] with { Header = "二行の\n見出し" }).ConfigureAwait(true);
        var doubled = HeaderBottom();
        Check("header: two lines make the header taller", doubled > single);
        await NextFrameAsync(() => grid.ColumnOrders = grid.ColumnOrders.Select(order => order.Key == lastKey ? order with { IsVisible = false } : order).ToArray()).ConfigureAwait(true);
        Check("header: a hidden two line header keeps the height", HeaderBottom() == doubled);

        host.Content = null;
        grid.Dispose();
        grid.Handler?.DisconnectHandler();
        grid.Handler = null;
        grid = new ClamGridView { AutomationId = "QualityGrid" };
        ScenarioGridFactory.Configure(grid, scenario, view);
        Check("scroll: request before the first layout is accepted", grid.ScrollIntoView(400, null, ScrollToPosition.Start) && grid.ScrollY.Equals(0d));
        frame = await NextFrameAsync(() => host.Content = grid).ConfigureAwait(true);
        Check("scroll: kept request is applied by the first layout", frame.Rows.Start == 400);
        return;

        int HeaderBottom() => Enumerable.Range(0, 400).First(y => grid.HitTest(10, y).CellType != GridCellType.ColumnHeader);
    }
}
