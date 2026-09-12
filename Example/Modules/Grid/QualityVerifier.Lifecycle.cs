namespace Example.Modules.Grid;

public sealed partial class QualityVerifier
{
    private async Task VerifyLifecycleAsync()
    {
        report("寿命検証: Handler再接続・画面の生成と破棄");
        grid.ItemsSource = null;
        data?.Dispose();
        var scenario = ListScenario.All[2];
        data = scenario.CreateData(1000);
        await NextFrameAsync(() => ScenarioGridFactory.Configure(grid, scenario, data)).ConfigureAwait(true);
        var notifications = 0;
        void OnSelection(object? sender, EventArgs args) => Interlocked.Increment(ref notifications);
        grid.SelectionChanged += OnSelection;
        for (var iteration = 0; iteration < 10; iteration++)
        {
            data.ClearSelection();
            data.SetSelected(0, true);
            grid.ScrollIntoView(40, 3);
            var selected = data.SelectedItems.Single();
            host.Content = null;
            grid.Handler?.DisconnectHandler();
            grid.Handler = null;
            await Task.Delay(20, cancellation.Token).ConfigureAwait(true);
            await NextFrameAsync(() => host.Content = grid).ConfigureAwait(true);
            Check($"reconnect {iteration + 1}: selection retained", ReferenceEquals(selected, grid.SelectedItems.Single()));
            Interlocked.Exchange(ref notifications, 0);
            data.ClearSelection();
            Check($"reconnect {iteration + 1}: one subscription", Volatile.Read(ref notifications) == 1);
        }

        grid.SelectionChanged -= OnSelection;
        var weakViews = new List<WeakReference>();
        long baseline = 0;
        for (var iteration = 0; iteration < 25; iteration++)
        {
            host.Content = null;
            grid.Dispose();
            grid.Handler?.DisconnectHandler();
            grid.Handler = null;
            weakViews.Add(new WeakReference(grid));
            data.Dispose();
            data = scenario.CreateData(1000);
            grid = new ClamGridView { AutomationId = "QualityGrid" };
            await NextFrameAsync(() =>
            {
                ScenarioGridFactory.Configure(grid, scenario, data);
                host.Content = grid;
            }).ConfigureAwait(true);
            grid.TryToggleSelection(0, out _);
            Check($"new screen {iteration + 1}: usable after disposal", grid.SelectedCount == 1);
            if (iteration == 4)
            {
                baseline = GC.GetTotalMemory(true);
            }
        }

        await Task.Delay(150, cancellation.Token).ConfigureAwait(true);
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        var retained = GC.GetTotalMemory(false);
        var alive = weakViews.Count(static reference => reference.IsAlive);
        var pss = global::Android.OS.Debug.Pss;
        Log($"LIFETIME cycles=25;reconnections=10;weak_alive={alive};managed_baseline_bytes={baseline};managed_final_bytes={retained};managed_growth_bytes={retained - baseline};pss_kb={pss}");
        Check("disposed grids collect", alive <= 1);

        grid.ItemsSource = Array.Empty<TicketRow>();
        await NextFrameAsync(() => grid.Columns.ReplaceAll([])).ConfigureAwait(true);
        Check("zero rows and columns", grid.RowCount == 0 && grid.Columns.Count == 0 && !grid.ScrollIntoView(0, 0));
        await NextFrameAsync(() => ScenarioGridFactory.Configure(grid, scenario, data)).ConfigureAwait(true);
        Check("source replacement restores usable grid", grid.RowCount == 1000 && grid.Columns.Count == 18);
        foreach (var size in new[] { 12f, 22f, 32f })
        {
            var frame = await NextFrameAsync(() => grid.GridStyle = grid.GridStyle with { FontSize = size }).ConfigureAwait(true);
            Check($"font {size}: rendered and bounded", frame.RenderedCells > 0 && frame.RenderedCells < 500);
        }
    }
}
