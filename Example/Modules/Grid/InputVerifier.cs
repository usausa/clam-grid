namespace Example.Modules.Grid;

using Android.OS;
using Android.Views;

public sealed partial class InputVerifier : IDisposable
{
    public const string Tag = "ClamGrid.InputTest";

    private readonly ClamGridView grid;

    private readonly ScrollView parent;

    private readonly ObservableCollection<SampleRow> source = SampleData.CreateSource(100);

    private readonly GridDataView<SampleRow> data;

    private Action<string> report = static _ => { };

    private int taps;

    private int holds;

    private int resizes;

    private int configurations;

    private bool cancelTap;

    private bool cancelResize;

    private bool disposed;

    private long downTime;

    public int Passed { get; private set; }

    public InputVerifier(ClamGridView grid, ScrollView parent)
    {
        this.grid = grid;
        this.parent = parent;
        data = new GridDataView<SampleRow>(source, static row => row.Id);
        data.RegisterSort("id", static row => row.Id);
        grid.GridStyle = new GridStyle { RowHeaderWidth = 40, RowHeight = 40, HeaderHeight = 40 };
        grid.Columns.ReplaceAll([
            new GridColumn("id", "番号", new GridValueAccessor<SampleRow, int>(static row => row.Id + 1)) { Width = GridColumnWidth.Absolute(100) },
            new GridColumn("name", "名前", new GridValueAccessor<SampleRow, string>(static row => row.Name)) { Width = GridColumnWidth.Absolute(130) },
            new GridColumn("description", "説明", new GridValueAccessor<SampleRow, string>(static row => row.Description)) { Width = GridColumnWidth.Absolute(450) }
        ]);
        grid.ItemsSource = data;
        grid.CellTapped += OnCellTapped;
        grid.CellLongPressed += OnCellLongPressed;
        grid.ColumnWidthChanging += OnColumnWidthChanging;
        grid.ColumnWidthChanged += OnColumnWidthChanged;
        grid.ColumnConfigurationRequested += OnColumnConfigurationRequested;
    }

    public async Task RunAsync(Action<string> status)
    {
        report = status;
        await Task.Delay(800).ConfigureAwait(true);
        try
        {
            await VerifyAsync().ConfigureAwait(true);
        }
        finally
        {
            if (!disposed)
            {
                grid.CancelInteraction();
                Send(MotionEventActions.Cancel, 90, 80);
            }
        }
    }

    public void Dispose()
    {
        if (!disposed)
        {
            disposed = true;
            grid.CellTapped -= OnCellTapped;
            grid.CellLongPressed -= OnCellLongPressed;
            grid.ColumnWidthChanging -= OnColumnWidthChanging;
            grid.ColumnWidthChanged -= OnColumnWidthChanged;
            grid.ColumnConfigurationRequested -= OnColumnConfigurationRequested;
            grid.ItemsSource = null;
            grid.RowMover = null;
            data.Dispose();
        }
    }

    private void OnCellTapped(object? sender, GridCellEventArgs e)
    {
        taps++;
        e.Handled = cancelTap;
    }

    private void OnCellLongPressed(object? sender, GridCellEventArgs e) => holds++;

    private void OnColumnWidthChanging(object? sender, GridColumnWidthEventArgs e) => e.Cancel = cancelResize;

    private void OnColumnWidthChanged(object? sender, GridColumnWidthEventArgs e) => resizes++;

    private void OnColumnConfigurationRequested(object? sender, GridColumnConfigurationEventArgs e) => configurations++;

    private async Task VerifyAsync()
    {
        await TapAsync(90, 80).ConfigureAwait(true);
        Check("tap selects once", taps == 1 && data.SelectedCount == 1);
        await TapAsync(90, 80).ConfigureAwait(true);
        Check("retap clears", taps == 2 && data.SelectedCount == 0);
        await HoldAsync(90, 80).ConfigureAwait(true);
        Check("long press selects all without tap", holds == 1 && taps == 2 && data.SelectedCount == 100);
        await HoldAsync(90, 80).ConfigureAwait(true);
        Check("long press clears all", holds == 2 && taps == 2 && data.SelectedCount == 0);
        data.SelectionMode = GridSelectionMode.SingleToggle;
        await HoldAsync(90, 80).ConfigureAwait(true);
        Check("single mode long press does not select", data.SelectedCount == 0 && taps == 2);
        data.SelectionMode = GridSelectionMode.MultipleToggle;

        var commands = 0;
        grid.CellTappedCommand = new Command<GridCellEventArgs>(_ => commands++);
        await TapAsync(90, 80).ConfigureAwait(true);
        Check("command consumes default selection", commands == 1 && data.SelectedCount == 0);
        cancelTap = true;
        await TapAsync(90, 80).ConfigureAwait(true);
        Check("handled event suppresses command", commands == 1 && data.SelectedCount == 0);
        cancelTap = false;
        grid.CellTappedCommand = new Command<GridCellEventArgs>(_ => commands++, _ => false);
        await TapAsync(90, 80).ConfigureAwait(true);
        Check("disabled cell command permits default", commands == 1 && data.SelectedCount == 1);
        grid.CellTappedCommand = null;
        data.ClearSelection();
        grid.CellTappedCommand = new Command<GridCellEventArgs>(_ => commands++, _ =>
        {
            source.Move(0, 99);
            return false;
        });
        await TapAsync(90, 80).ConfigureAwait(true);
        Check("CanExecute source change cancels default", commands == 1 && data.SelectedCount == 0);
        grid.CellTappedCommand = null;
        source.Move(99, 0);
        grid.CellLongPressedCommand = new Command<GridCellEventArgs>(_ => commands++);
        await HoldAsync(90, 80).ConfigureAwait(true);
        Check("long command consumes bulk selection", commands == 2 && data.SelectedCount == 0);
        grid.CellLongPressedCommand = null;
        grid.SelectAllCommand = new Command<bool>(select => data.UpdateSelection(row => select && !row.IsDiscontinued));
        await HoldAsync(90, 120).ConfigureAwait(true);
        Check("bulk command receives true and applies predicate", data.SelectedCount == 75);
        await HoldAsync(90, 120).ConfigureAwait(true);
        Check("bulk command receives false", data.SelectedCount == 0);
        grid.SelectAllCommand = new Command<bool>(_ => commands++, _ => false);
        await HoldAsync(90, 80).ConfigureAwait(true);
        Check("disabled bulk command performs no fallback", data.SelectedCount == 0 && commands == 2);
        grid.SelectAllCommand = null;

        await TapAsync(90, 20).ConfigureAwait(true);
        Check("header tap sorts", data.SortOrders.Count == 1 && !data.SortOrders[0].Descending);
        var priorTaps = taps;
        await HoldAsync(90, 20).ConfigureAwait(true);
        Check("header long press requests configuration without sort", configurations == 1 && taps == priorTaps && !data.SortOrders[0].Descending);
        await DragAsync(140, 20, 200, 20).ConfigureAwait(true);
        Check("resize commits width without tap or sort", Math.Abs(grid.Columns[0].Width.Value - 160) < 1 && resizes == 1 && taps == priorTaps && !data.SortOrders[0].Descending);
        cancelResize = true;
        await DragAsync(200, 20, 240, 20).ConfigureAwait(true);
        Check("resize event can cancel commit", Math.Abs(grid.Columns[0].Width.Value - 160) < 1 && resizes == 1);
        cancelResize = false;
        await DragAsync(200, 20, 60, 20).ConfigureAwait(true);
        Check("resize clamps to minimum width", Math.Abs(grid.Columns[0].Width.Value - 40) < 1 && resizes == 2 && taps == priorTaps);
        grid.Columns[0] = grid.Columns[0] with { Width = GridColumnWidth.Absolute(160) };
        resizes = 1;
        Send(MotionEventActions.Down, 200, 20);
        Send(MotionEventActions.Move, 240, 20);
        Send(MotionEventActions.Cancel, 240, 20);
        Check("OS cancel restores resize preview", Math.Abs(grid.Columns[0].Width.Value - 160) < 1 && resizes == 1 && grid.InputState == GridGestureState.Idle);
        await DragAsync(200, 20, 210, -20).ConfigureAwait(true);
        Check("outside release cancels resize", Math.Abs(grid.Columns[0].Width.Value - 160) < 1 && resizes == 1 && taps == priorTaps);
        grid.AllowColumnResizing = false;
        await DragAsync(200, 20, 140, 20).ConfigureAwait(true);
        Check("resize disabled pans without changing width", Math.Abs(grid.Columns[0].Width.Value - 160) < 1 && resizes == 1 && grid.ScrollX > 0);
        grid.AllowColumnResizing = true;
        grid.ScrollTo(0, 0);
        grid.Columns[0] = grid.Columns[0] with { AllowResizing = false };
        await DragAsync(200, 20, 140, 20).ConfigureAwait(true);
        Check("per-column resize permission is respected", Math.Abs(grid.Columns[0].Width.Value - 160) < 1 && resizes == 1 && grid.ScrollX > 0);
        grid.Columns[0] = grid.Columns[0] with { AllowResizing = true };
        grid.ScrollTo(0, 0);

        priorTaps = taps;
        Send(MotionEventActions.Down, 90, 80);
        SendMultiple(MotionEventActions.PointerDown | (MotionEventActions)(1 << 8), 2);
        SendMultiple(MotionEventActions.PointerUp | (MotionEventActions)(1 << 8), 2);
        Send(MotionEventActions.Up, 90, 80);
        await Task.Delay(550).ConfigureAwait(true);
        Check("multiple pointers cancel entire gesture", taps == priorTaps && data.SelectedCount == 0 && grid.InputState == GridGestureState.Idle);
        Send(MotionEventActions.Down, 90, 80);
        source.Move(0, 99);
        Send(MotionEventActions.Up, 90, 80);
        Check("source change cancels pending tap", taps == priorTaps && data.SelectedCount == 0);
        Send(MotionEventActions.Down, 90, 80);
        grid.CancelInteraction();
        Send(MotionEventActions.Up, 90, 80);
        Check("explicit cancellation suppresses release", taps == priorTaps);

        await DragAsync(230, 210, 230, 80).ConfigureAwait(true);
        var releasedY = grid.ScrollY;
        await Task.Delay(100).ConfigureAwait(true);
        Check("pan coasts after release without selection", grid.ScrollY > releasedY && grid.IsInertiaRunning && data.SelectedCount == 0 && taps == priorTaps);
        Send(MotionEventActions.Down, 90, 80);
        var stoppedY = grid.ScrollY;
        await Task.Delay(80).ConfigureAwait(true);
        Check("new pointer stops inertia", !grid.IsInertiaRunning && grid.ScrollY.Equals(stoppedY));
        Send(MotionEventActions.Cancel, 90, 80);
        grid.ScrollTo(0, 0);
        await DragAsync(230, 210, 230, 80).ConfigureAwait(true);
        source[0].Name = "更新";
        var changedY = grid.ScrollY;
        await Task.Delay(80).ConfigureAwait(true);
        Check("data update stops inertia", !grid.IsInertiaRunning && grid.ScrollY.Equals(changedY));
        grid.ScrollTo(0, Double.MaxValue);
        var endY = grid.ScrollY;
        await DragAsync(230, 210, 230, 80).ConfigureAwait(true);
        Check("inertia stops at content end", !grid.IsInertiaRunning && grid.ScrollY.Equals(endY));
        grid.ScrollTo(0, 0);
        Send(MotionEventActions.Down, 90, 80);
        grid.IsEnabled = false;
        grid.IsEnabled = true;
        Send(MotionEventActions.Up, 90, 80);
        Check("disable cancels active press", taps == priorTaps);

        await parent.ScrollToAsync(0, 0, false).ConfigureAwait(true);
        grid.ScrollTo(0, 0);
        await Task.Delay(100).ConfigureAwait(true);
        var parentY = parent.ScrollY;
        await DragAsync(230, 210, 230, 80, true).ConfigureAwait(true);
        Check("nested grid owns gesture and parent stays still", grid.ScrollY > 0 && parent.ScrollY.Equals(parentY) && data.SelectedCount == 0);
        grid.CancelInteraction();
        Check("row refresh validates range", grid.RefreshRows(0, 1) && !grid.RefreshRows(-1, 1) && !grid.RefreshRows(99, 2));
        source.Clear();
        await TapAsync(90, 80).ConfigureAwait(true);
        Check("empty body has no phantom tap", taps == priorTaps && data.SelectedCount == 0);
        await VerifyEditingAsync().ConfigureAwait(true);
    }

    private void Check(string name, bool success)
    {
        if (!success)
        {
            throw new InvalidOperationException(name);
        }

        Passed++;
        report($"{Passed}項目通過: {name}");
        global::Android.Util.Log.Info(Tag, $"PASS {Passed}: {name}");
    }

    private async Task TapAsync(float x, float y)
    {
        Send(MotionEventActions.Down, x, y);
        await Task.Delay(30).ConfigureAwait(true);
        Send(MotionEventActions.Up, x, y);
    }

    private async Task HoldAsync(float x, float y)
    {
        Send(MotionEventActions.Down, x, y);
        await Task.Delay(650).ConfigureAwait(true);
        Send(MotionEventActions.Up, x, y);
    }

    private async Task DragAsync(float fromX, float fromY, float toX, float toY, bool throughParent = false)
    {
        Send(MotionEventActions.Down, fromX, fromY, throughParent);
        for (var step = 1; step <= 8; step++)
        {
            await Task.Delay(16).ConfigureAwait(true);
            Send(MotionEventActions.Move, fromX + ((toX - fromX) * step / 8), fromY + ((toY - fromY) * step / 8), throughParent);
        }

        Send(MotionEventActions.Up, toX, toY, throughParent);
    }

    private void Send(MotionEventActions action, float x, float y, bool throughParent = false)
    {
        var view = (View)grid.Handler!.PlatformView!;
        var target = throughParent ? (View)parent.Handler!.PlatformView! : view;
        var scale = (float)(view.Width / grid.Width);
        var offsetX = 0;
        var offsetY = 0;
        if (throughParent)
        {
            var childPosition = new int[2];
            var parentPosition = new int[2];
            view.GetLocationOnScreen(childPosition);
            target.GetLocationOnScreen(parentPosition);
            offsetX = childPosition[0] - parentPosition[0];
            offsetY = childPosition[1] - parentPosition[1];
        }

        if (action == MotionEventActions.Down)
        {
            downTime = SystemClock.UptimeMillis();
        }

        using var motion = MotionEvent.Obtain(downTime, SystemClock.UptimeMillis(), action, (x * scale) + offsetX, (y * scale) + offsetY, MetaKeyStates.None)!;
        motion.SetSource(InputSourceType.Touchscreen);
        target.DispatchTouchEvent(motion);
    }

    private void SendMultiple(MotionEventActions action, int count)
    {
        var view = (View)grid.Handler!.PlatformView!;
        var scale = (float)(view.Width / grid.Width);
        using var first = new MotionEvent.PointerProperties { Id = 0, ToolType = MotionEventToolType.Finger };
        using var second = new MotionEvent.PointerProperties { Id = 1, ToolType = MotionEventToolType.Finger };
        using var firstPoint = new MotionEvent.PointerCoords { X = 90 * scale, Y = 80 * scale, Pressure = 1, Size = 1 };
        using var secondPoint = new MotionEvent.PointerCoords { X = 110 * scale, Y = 90 * scale, Pressure = 1, Size = 1 };
        using var motion = MotionEvent.Obtain(downTime, SystemClock.UptimeMillis(), action, count, [first, second], [firstPoint, secondPoint], MetaKeyStates.None, 0, 1, 1, 0, 0, InputSourceType.Touchscreen, MotionEventFlags.None)!;
        view.DispatchTouchEvent(motion);
    }
}
