namespace Example.Modules.Grid;

public sealed partial class QualityVerifier
{
    private async Task BenchmarkColorsAsync()
    {
        report("50,000行: 条件色コールバックの描画負荷");
        grid.RowHeadersVisible = true;
        grid.RowHeaderWidth = 48;
        var warning = new GridColors(Colors.DarkRed, Colors.MistyRose);
        var header = new GridColors(Colors.Yellow, Colors.DarkSlateBlue);
        foreach (var enabled in new[] { false, true })
        {
            var cellCalls = 0;
            var columnCalls = 0;
            var rowCalls = 0;
            GridColors CellColors(GridCellColorContext context)
            {
                cellCalls++;
                return !context.IsSelected && (((TicketRow)context.Item).Status == 2) ? warning : default;
            }

            GridColors ColumnHeaderColors(GridColumnHeaderColorContext context)
            {
                columnCalls++;
                return context.SortPriority == 0 ? header : default;
            }

            GridColors RowHeaderColors(GridRowHeaderColorContext context)
            {
                rowCalls++;
                return !context.IsSelected && (((TicketRow)context.Item).Status == 2) ? warning : default;
            }
            await NextFrameAsync(() =>
            {
                grid.CellColors = enabled ? CellColors : null;
                grid.ColumnHeaderColors = enabled ? ColumnHeaderColors : null;
                grid.RowHeaderColors = enabled ? RowHeaderColors : null;
                grid.ScrollTo(0, 0);
            }).ConfigureAwait(true);
            var frames = new List<double>();
            var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var previousCells = cellCalls;
            var previousColumns = columnCalls;
            var previousRows = rowCalls;
            var visibleOnly = true;
            var maxCalls = 0;
            var count = 0;
            void OnFrame(object? sender, GridFrameEventArgs frame)
            {
                var currentCells = cellCalls - previousCells;
                var currentColumns = columnCalls - previousColumns;
                var currentRows = rowCalls - previousRows;
                visibleOnly &= enabled ? currentCells == frame.RenderedCells && currentColumns == frame.Columns.Count && currentRows == frame.Rows.Count : currentCells + currentColumns + currentRows == 0;
                previousCells = cellCalls;
                previousColumns = columnCalls;
                previousRows = rowCalls;
                maxCalls = Math.Max(maxCalls, currentCells + currentColumns + currentRows);
                count++;
                if (count > 30)
                {
                    frames.Add(frame.Milliseconds);
                }

                if (count >= 150)
                {
                    completion.TrySetResult();
                }
            }

            grid.FrameRendered += OnFrame;
            using var callback = new ScrollFrameCallback(() => grid.ScrollTo(grid.ScrollX + 2, grid.ScrollY + 12));
            try
            {
                callback.Start();
                await completion.Task.WaitAsync(TimeSpan.FromSeconds(20), cancellation.Token).ConfigureAwait(true);
            }
            finally
            {
                callback.Stop();
                grid.FrameRendered -= OnFrame;
            }

            Check($"colors: enabled={enabled} viewport callback count", visibleOnly && maxCalls < 500 && frames.Count >= 120);
            Log(FormattableString.Invariant($"COLOR_METRIC enabled={enabled};rows={grid.RowCount};columns={grid.VisibleColumns().Count};frames={frames.Count};paint_p95_ms={P95(frames):F3};max_callbacks_per_frame={maxCalls}"));
        }

        grid.CellColors = null;
        grid.ColumnHeaderColors = null;
        grid.RowHeaderColors = null;
    }
}
