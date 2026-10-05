namespace Example.Modules.Grid;

public sealed partial class QualityVerifier
{
    private async Task VerifyViewAsync()
    {
        report("表示検証: 絞り込み");
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
    }
}
