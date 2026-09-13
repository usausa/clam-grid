namespace Example.Modules.Grid;

using System.Diagnostics;

public sealed partial class QualityVerifier
{
    private async Task BenchmarkSortCallbackAsync(ListScenario scenario, GridDataView<TicketRow> view, int rows)
    {
        report($"ソート方式の比較: {rows:N0}行");
        var standardTimes = new List<double>();
        var callbackTimes = new List<double>();
        var standardPaintTimes = new List<double>();
        var callbackPaintTimes = new List<double>();
        var standardAllocations = new List<double>();
        var callbackAllocations = new List<double>();
        var baseOrders = scenario.DefaultKeys.Where(view.CanSort).Select(static key => new GridSortOrder(key)).ToArray();
        var selectedRow = view[view.IndexOfKey(42)];
        view.SetSelected(view.IndexOfKey(42), true);
        for (var iteration = 0; iteration < 5; iteration++)
        {
            var orders = new[] { new GridSortOrder("CompanyName", (iteration % 2) != 0) }.Concat(baseOrders).ToArray();
            int[]? expected = null;
            // Alternates the execution order and gives both methods the same input order, primary key and secondary key each round
            bool[] modes = (iteration % 2) == 0 ? [false, true] : [true, false];
            foreach (var useCallback in modes)
            {
                if (useCallback)
                {
                    view.SetSortCallback(scenario.GetSortKeys(), scenario.SortRows);
                }
                else
                {
                    view.ClearSortCallback();
                }

                await NextFrameAsync(() => view.RestoreSortOrders(baseOrders)).ConfigureAwait(true);
                var elapsed = 0d;
                long allocated = 0;
                GridSortResult? result = null;
                var startedAt = Stopwatch.GetTimestamp();
                await NextFrameAsync(() =>
                {
                    var startBytes = GC.GetAllocatedBytesForCurrentThread();
                    var start = Stopwatch.GetTimestamp();
                    result = view.RestoreSortOrders(orders);
                    elapsed = Stopwatch.GetElapsedTime(start).TotalMilliseconds;
                    allocated = GC.GetAllocatedBytesForCurrentThread() - startBytes;
                }).ConfigureAwait(true);
                var paintElapsed = Stopwatch.GetElapsedTime(startedAt).TotalMilliseconds;
                Check($"{rows}: callback={useCallback} round={iteration + 1} commit", result?.Status == GridSortStatus.Applied && view.SortOrders.SequenceEqual(orders) && view.SelectedCount == 1 && ReferenceEquals(view.SelectedItems[0], selectedRow) && ReferenceEquals(view.Items[view.IndexOfKey(42)], selectedRow));
                if (expected is null)
                {
                    expected = view.Select(static row => row.Id).ToArray();
                }
                else
                {
                    Check($"{rows}: callback round={iteration + 1} identical order", expected.SequenceEqual(view.Select(static row => row.Id)));
                }

                (useCallback ? callbackTimes : standardTimes).Add(elapsed);
                (useCallback ? callbackPaintTimes : standardPaintTimes).Add(paintElapsed);
                (useCallback ? callbackAllocations : standardAllocations).Add(allocated);
            }
        }

        Log(FormattableString.Invariant($"SORT_CALLBACK rows={rows};rounds=5;standard_sort_p95_ms={P95(standardTimes):F3};callback_sort_p95_ms={P95(callbackTimes):F3};standard_to_paint_p95_ms={P95(standardPaintTimes):F3};callback_to_paint_p95_ms={P95(callbackPaintTimes):F3};standard_allocated_p95_bytes={P95(standardAllocations):F0};callback_allocated_p95_bytes={P95(callbackAllocations):F0}"));
        var callbackCalls = 0;
        view.SetSortCallback(scenario.GetSortKeys(), (input, orders) =>
        {
            callbackCalls++;
            return scenario.SortRows(input, orders);
        });
        await NextFrameAsync(() =>
        {
            grid.ScrollTo(0, 0);
            view.RestoreSortOrders(baseOrders);
        }).ConfigureAwait(true);
        var beforeTap = callbackCalls;
        await NextFrameAsync(() => DispatchTap(140, 20)).ConfigureAwait(true);
        Check($"{rows}: Android header callback descending", callbackCalls == beforeTap + 1 && view.SortOrders[0] == new GridSortOrder("DeptCode", true) && view.SelectedCount == 1);
        await NextFrameAsync(() => DispatchTap(140, 20)).ConfigureAwait(true);
        Check($"{rows}: Android header callback ascending", callbackCalls == beforeTap + 2 && view.SortOrders[0] == new GridSortOrder("DeptCode"));
        static void Cancel(object? sender, GridSortRequestedEventArgs args) => args.Cancel = true;
        view.SortRequested += Cancel;
        try
        {
            await NextFrameAsync(() => DispatchTap(140, 20)).ConfigureAwait(true);
            Check($"{rows}: Android header callback cancel", callbackCalls == beforeTap + 2 && view.SortOrders[0] == new GridSortOrder("DeptCode"));
        }
        finally
        {
            view.SortRequested -= Cancel;
        }

        view.ClearSortCallback();
        view.ClearSelection();
    }
}
