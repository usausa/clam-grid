namespace Example.Modules.Grid;

using Android.Views;

using Example.Components;

public sealed partial class InputVerifier
{
    private int edits;

    private int moves;

    private async Task VerifyEditingAsync()
    {
        data.RestoreSortOrders([]);
        for (var index = 0; index < 20; index++)
        {
            source.Add(new SampleRow(index));
        }

        grid.ScrollTo(0, 0);
        var boolean = new GridColumn("visible", "発注", new GridValueAccessor<SampleRow, bool>(static row => row.IsChecked, static (row, value) => row.IsChecked = value))
        {
            Width = GridColumnWidth.Absolute(100), IsBoolean = true, IsReadOnly = false, AllowSorting = false
        };
        var name = new GridColumn("name", "名前", new GridValueAccessor<SampleRow, string>(static row => row.Name)) { Width = GridColumnWidth.Absolute(130), IsReadOnly = true, AllowSorting = false };
        var address = new GridColumn("description", "説明", new GridValueAccessor<SampleRow, string>(static row => row.Description)) { Width = GridColumnWidth.Absolute(450), AllowSorting = false };
        grid.ConfigureColumns([boolean, name, address]);
        var beforeTaps = taps;
        var beforeHolds = holds;
        edits = 0;
        var cancel = false;
        grid.CellValueChanged += (_, _) => edits++;
        grid.CellValueChanging += (_, e) => e.Cancel = cancel;
        await TapAsync(90, 60).ConfigureAwait(true);
        Check("bool tap writes property once without row selection", source[0].IsChecked && edits == 1 && taps == beforeTaps && data.SelectedCount == 0);
        await TapAsync(90, 60).ConfigureAwait(true);
        Check("bool retap reverses value", !source[0].IsChecked && edits == 2);
        await HoldAsync(90, 60).ConfigureAwait(true);
        Check("bool long hold does not write or select", !source[0].IsChecked && edits == 2 && holds == beforeHolds && data.SelectedCount == 0);
        cancel = true;
        await TapAsync(90, 60).ConfigureAwait(true);
        Check("bool edit event can cancel", !source[0].IsChecked && edits == 2);
        cancel = false;
        grid.Columns[0] = boolean with { IsReadOnly = true };
        await TapAsync(90, 60).ConfigureAwait(true);
        Check("column read-only prevents bool edit", !source[0].IsChecked && edits == 2);
        grid.Columns[0] = boolean with { IsReadOnly = null };
        await TapAsync(90, 60).ConfigureAwait(true);
        Check("bool inherits grid read-only", !source[0].IsChecked && edits == 2);
        grid.IsReadOnly = false;
        await TapAsync(90, 60).ConfigureAwait(true);
        Check("bool inherits editable grid", source[0].IsChecked && edits == 3);
        grid.Columns[0] = new GridColumn("visible", "発注", new GridValueAccessor<SampleRow, bool>(static row => row.IsChecked)) { Width = GridColumnWidth.Absolute(100), IsBoolean = true, IsReadOnly = false };
        await TapAsync(90, 60).ConfigureAwait(true);
        Check("missing setter prevents editing", source[0].IsChecked && edits == 3);
        grid.Columns[0] = boolean;
        source[0].IsChecked = false;
        await DragAsync(90, 200, 90, 80).ConfigureAwait(true);
        Check("pan from bool cell never writes", edits == 3 && data.SelectedCount == 0);
        grid.ScrollTo(0, 0);
        grid.IsReadOnly = true;
        grid.AllowColumnResizing = false;
        grid.AllowColumnConfiguration = false;
        grid.AllowRowDragging = true;
        grid.RowMover = new GridRowMover<SampleRow>(source, data);
        data.SetSelected(0, true);
        moves = 0;
        var cancelMove = false;
        grid.RowMoved += (_, _) => moves++;
        grid.RowMoveRequested += (_, e) => e.Cancel = cancelMove;
        await HoldAsync(20, 60).ConfigureAwait(true);
        Check("row handle hold has no tap or long event", taps == beforeTaps && holds == beforeHolds && moves == 0 && data.SelectedCount == 1);
        await DragAsync(20, 60, 20, 190).ConfigureAwait(true);
        Check("row drag commits one source Move and preserves selected key", source[3].Id == 0 && data.IsSelected(3) && moves == 1 && taps == beforeTaps);
        await DragAsync(20, 180, 20, 45).ConfigureAwait(true);
        Check("row drag can move upward", source[0].Id == 0 && data.IsSelected(0) && moves == 2);
        cancelMove = true;
        await DragAsync(20, 60, 20, 190).ConfigureAwait(true);
        Check("row move request can cancel", source[0].Id == 0 && moves == 2);
        cancelMove = false;
        Send(MotionEventActions.Down, 20, 60);
        Send(MotionEventActions.Move, 20, 190);
        Send(MotionEventActions.Cancel, 20, 190);
        Check("OS cancel discards row preview", source[0].Id == 0 && moves == 2 && grid.InputState == GridGestureState.Idle);
        await DragAsync(20, 60, -20, 190).ConfigureAwait(true);
        Check("outside drop leaves collection unchanged", source[0].Id == 0 && moves == 2);
        Send(MotionEventActions.Down, 20, 60);
        Send(MotionEventActions.Move, 20, 190);
        SendMultiple(MotionEventActions.PointerDown | (MotionEventActions)(1 << 8), 2);
        SendMultiple(MotionEventActions.PointerUp | (MotionEventActions)(1 << 8), 2);
        Send(MotionEventActions.Up, 20, 190);
        Check("second pointer cancels row drag", source[0].Id == 0 && moves == 2);
        Send(MotionEventActions.Down, 20, 60);
        Send(MotionEventActions.Move, 20, 190);
        source[0].Name = "操作中に更新";
        Send(MotionEventActions.Up, 20, 190);
        Check("data change cancels row drag", source[0].Id == 0 && moves == 2);
        Send(MotionEventActions.Down, 20, 60);
        Send(MotionEventActions.Move, 20, 252);
        await Task.Delay(400).ConfigureAwait(true);
        var edgeScroll = grid.ScrollY;
        Check("stationary drag at lower edge auto scrolls without early Move", edgeScroll > 80 && source[0].Id == 0 && moves == 2 && grid.InputState == GridGestureState.RowDragging);
        Send(MotionEventActions.Move, 20, 42);
        await Task.Delay(200).ConfigureAwait(true);
        Check("stationary drag at upper edge scrolls upward", grid.ScrollY < edgeScroll);
        Send(MotionEventActions.Up, 20, 42);
        Check("edge drag commits matching source and view order", source.Select(static row => row.Id).SequenceEqual(data.Select(static row => row.Id)) && data.SelectedCount == 1 && !grid.IsInertiaRunning);
        grid.ScrollTo(0, 0);
        data.SortBy("id");
        Check("sorted view disables row mover", !grid.RowMover.CanMove);
        data.RestoreSortOrders([]);
        grid.AllowRowDragging = false;

        grid.ApplyColumnOrders([new("description", true), new("visible", false), new("name", true)]);
        Check("column apply updates visible order and preserves row selection", grid.Columns.Select(static column => column.Key).SequenceEqual(["description", "name"]) && data.SelectedCount == 1);
        var session = grid.CreateColumnEditSession();
        session.Columns[0].IsVisible = false;
        session.Columns.Move(2, 0);
        Check("edit copy contains hidden columns without changing grid", session.Columns.Count == 3 && grid.Columns[0].Key == "description" && grid.ColumnOrders[0].IsVisible);
        grid.ScrollTo(Double.MaxValue, 0);
        grid.ApplyColumnOrders([new("name", true), new("description", false), new("visible", false)]);
        Check("column update clamps scroll and hit test uses new order", grid.ScrollX.Equals(0d) && grid.HitTest(90, 60).ColumnIndex == 0 && grid.Columns[0].Key == "name" && data.SelectedCount == 1);
        grid.ApplyColumnOrders([new("visible", false), new("name", false), new("description", false)]);
        Check("all hidden settings restore all default columns", grid.Columns.Select(static column => column.Key).SequenceEqual(["visible", "name", "description"]));
        var decoded = ColumnSettingsCodec.Read("[{\"ColumnName\":\"name\",\"IsVisible\":true},{\"ColumnName\":\"visible\",\"IsVisible\":false}]");
        grid.ApplyColumnOrders(decoded);
        var encoded = ColumnSettingsCodec.Write(grid.ColumnOrders);
        Check("legacy ColumnOrder JSON round trips without reflection", grid.ColumnOrders.SequenceEqual(ColumnSettingsCodec.Read(encoded)!) && grid.Columns.Count == 1 && encoded.Contains("ColumnName", StringComparison.Ordinal));
        grid.ConfigureColumns([boolean, name, address, new GridColumn("new", "追加", name.ValueAccessor)], grid.ColumnOrders);
        Check("new catalog column is appended hidden", grid.ColumnOrders[^1] == new GridColumnOrder("new", false) && grid.Columns.Count == 1);
    }
}
