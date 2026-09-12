namespace Example.Modules.Grid;

using System.Diagnostics;

using Android.OS;
using Android.Views;

public sealed partial class QualityVerifier
{
    private async Task BenchmarkAsync(int rows, int columns)
    {
        report($"性能計測: {rows:N0}行 × {columns}列");
        grid.ItemsSource = null;
        data?.Dispose();
        data = null;
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        var baseline = GC.GetTotalMemory(false);
        var scenario = ListScenario.All[2];
        var startedAt = Stopwatch.GetTimestamp();
        var first = await NextFrameAsync(() =>
        {
            data = scenario.CreateData(rows);
            ScenarioGridFactory.Configure(grid, scenario, data);
            grid.GridStyle = grid.GridStyle with { RowHeight = 40, HeaderHeight = 40 };
            if (columns > grid.Columns.Count)
            {
                grid.Columns.ReplaceAll(grid.Columns.Concat(Enumerable.Range(0, columns - grid.Columns.Count).Select(static index => new GridColumn($"extra{index}", $"追加項目{index + 1}", new GridValueAccessor<TicketRow, string>(static row => row.GetText("Department"))) { Width = GridColumnWidth.Absolute(150) })).ToArray());
            }
        }).ConfigureAwait(true);
        var initialMs = Stopwatch.GetElapsedTime(startedAt).TotalMilliseconds;
        Check($"{rows}x{columns}: initial frame", first.RenderedCells > 0 && grid.RowCount == rows && grid.Columns.Count == columns);

        var sortTimes = new List<double>();
        for (var iteration = 0; iteration < 5; iteration++)
        {
            startedAt = Stopwatch.GetTimestamp();
            await NextFrameAsync(() => data!.SortBy("CompanyName")).ConfigureAwait(true);
            sortTimes.Add(Stopwatch.GetElapsedTime(startedAt).TotalMilliseconds);
        }

        await NextFrameAsync(() =>
        {
            data!.ClearSelection();
            grid.ScrollTo(0, 0);
        }).ConfigureAwait(true);
        var tapTimes = new List<double>();
        for (var iteration = 0; iteration < 20; iteration++)
        {
            startedAt = Stopwatch.GetTimestamp();
            await NextFrameAsync(() => DispatchTap(20, 60)).ConfigureAwait(true);
            tapTimes.Add(Stopwatch.GetElapsedTime(startedAt).TotalMilliseconds);
            Check($"{rows}x{columns}: Android tap {iteration + 1}", data!.SelectedCount == ((iteration % 2) == 0 ? 1 : 0));
        }

        var frames = new List<GridFrameEventArgs>();
        var intervals = new List<double>();
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var frameCount = 0;
        var previous = Stopwatch.GetTimestamp();
        void OnFrame(object? sender, GridFrameEventArgs frame)
        {
            var now = Stopwatch.GetTimestamp();
            frameCount++;
            if (frameCount > 30)
            {
                frames.Add(frame);
                intervals.Add(Stopwatch.GetElapsedTime(previous, now).TotalMilliseconds);
            }

            previous = now;
            if (frameCount >= 270)
            {
                completion.TrySetResult();
            }
        }

        using var pump = new ScrollFrameCallback(() => grid.ScrollBy(2, 12));
        grid.FrameRendered += OnFrame;
        try
        {
            pump.Start();
            await completion.Task.WaitAsync(TimeSpan.FromSeconds(30), cancellation.Token).ConfigureAwait(true);
        }
        finally
        {
            pump.Stop();
            grid.FrameRendered -= OnFrame;
        }

        Check($"{rows}x{columns}: viewport culling", frames.All(static frame => frame.RenderedCells < 500 && frame.TextMeasurements < 500));
        var retained = GC.GetTotalMemory(true);
        var paint = P95(frames.Select(static frame => frame.Milliseconds));
        var tap = P95(tapTimes);
        var pss = global::Android.OS.Debug.Pss;
        Log(FormattableString.Invariant($"METRIC rows={rows};columns={columns};frames={frames.Count};initial_ms={initialMs:F3};sort_to_paint_p95_ms={P95(sortTimes):F3};tap_to_paint_p95_ms={tap:F3};paint_p95_ms={paint:F3};paint_interval_p95_ms={P95(intervals):F3};max_cells={frames.Max(static frame => frame.RenderedCells)};max_measurements={frames.Max(static frame => frame.TextMeasurements)};managed_bytes={retained};managed_delta_bytes={retained - baseline};pss_kb={pss};paint_budget_met={paint <= 16.7};tap_budget_met={tap <= 100}"));
        summaries.Add($"{rows:N0}×{columns}: 描画p95 {paint:F2}ms / 選択 {tap:F1}ms");
        await BenchmarkSortCallbackAsync(scenario, data!, rows).ConfigureAwait(true);
    }

    private void DispatchTap(float x, float y)
    {
        if (grid.Handler?.PlatformView is not View native)
        {
            throw new InvalidOperationException("Grid Android handler is missing.");
        }

        var scale = (float)(native.Width / grid.Width);
        var now = SystemClock.UptimeMillis();
        using var down = MotionEvent.Obtain(now, now, MotionEventActions.Down, x * scale, y * scale, 0);
        using var up = MotionEvent.Obtain(now, now + 1, MotionEventActions.Up, x * scale, y * scale, 0);
        native.DispatchTouchEvent(down);
        native.DispatchTouchEvent(up);
    }

    private sealed class ScrollFrameCallback(Action action) : Java.Lang.Object, Choreographer.IFrameCallback
    {
        private bool running;

        public void Start()
        {
            running = true;
            Choreographer.Instance!.PostFrameCallback(this);
        }

        public void Stop()
        {
            running = false;
            Choreographer.Instance!.RemoveFrameCallback(this);
        }

        public void DoFrame(long frameTimeNanos)
        {
            if (running)
            {
                action();
                Choreographer.Instance!.PostFrameCallback(this);
            }
        }
    }
}
