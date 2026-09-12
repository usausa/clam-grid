namespace Example.Messaging;

public sealed class GridScrollRequestEventArgs : EventArgs
{
    public int RowIndex { get; }

    public int ColumnIndex { get; }

    public double DeltaX { get; }

    public double DeltaY { get; }

    public GridScrollRequestEventArgs(int rowIndex, int columnIndex)
    {
        RowIndex = rowIndex;
        ColumnIndex = columnIndex;
    }

    public GridScrollRequestEventArgs(double deltaX, double deltaY)
    {
        RowIndex = -1;
        ColumnIndex = -1;
        DeltaX = deltaX;
        DeltaY = deltaY;
    }
}

public sealed class GridController : NotificationObject
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    public event EventHandler<GridScrollRequestEventArgs>? ScrollRequest;

    [EditorBrowsable(EditorBrowsableState.Never)]
    public event EventHandler<EventArgs>? InvalidateRequest;

    public event EventHandler<GridCellEventArgs>? CellTapped;

    public event EventHandler<GridCellEventArgs>? CellLongPressed;

    public event EventHandler<GridCellValueEventArgs>? CellValueChanged;

    public event EventHandler<GridColumnWidthEventArgs>? ColumnWidthChanged;

    public event EventHandler<GridRowMoveEventArgs>? RowMoved;

    public event EventHandler<GridFrameEventArgs>? FrameRendered;

    // Property

    public IReadOnlyList<GridColumn> Columns { get; }

    public IReadOnlyList<GridColumnOrder>? ColumnOrders
    {
        get;
        set => SetProperty(ref field, value);
    }

    public int VisibleColumnCount => ColumnOrders?.Count(static x => x.IsVisible) ?? Columns.Count;

    // Constructor

    public GridController(IEnumerable<GridColumn> columns, IEnumerable<GridColumnOrder>? orders = null)
    {
        Columns = columns.ToArray();
        ColumnOrders = orders?.ToArray();
    }

    // Column

    public GridColumnEditSession CreateColumnEditSession()
    {
        var headers = Columns.ToDictionary(static x => x.Key, static x => x.Header, StringComparer.Ordinal);
        var orders = ColumnOrders ?? Columns.Select(static x => new GridColumnOrder(x.Key, true)).ToArray();
        return new GridColumnEditSession(orders.Where(x => headers.ContainsKey(x.Key)).Select(x => new GridColumnOption(x.Key, headers[x.Key], x.IsVisible)));
    }

    // Request

    public void ScrollIntoView(int rowIndex, int columnIndex)
    {
        ScrollRequest?.Invoke(this, new GridScrollRequestEventArgs(rowIndex, columnIndex));
    }

    public void ScrollBy(double deltaX, double deltaY)
    {
        ScrollRequest?.Invoke(this, new GridScrollRequestEventArgs(deltaX, deltaY));
    }

    public void Invalidate()
    {
        InvalidateRequest?.Invoke(this, EventArgs.Empty);
    }

    // Handle

    [EditorBrowsable(EditorBrowsableState.Never)]
    public void HandleCellTapped(GridCellEventArgs e) => CellTapped?.Invoke(this, e);

    [EditorBrowsable(EditorBrowsableState.Never)]
    public void HandleCellLongPressed(GridCellEventArgs e) => CellLongPressed?.Invoke(this, e);

    [EditorBrowsable(EditorBrowsableState.Never)]
    public void HandleCellValueChanged(GridCellValueEventArgs e) => CellValueChanged?.Invoke(this, e);

    [EditorBrowsable(EditorBrowsableState.Never)]
    public void HandleColumnWidthChanged(GridColumnWidthEventArgs e) => ColumnWidthChanged?.Invoke(this, e);

    [EditorBrowsable(EditorBrowsableState.Never)]
    public void HandleRowMoved(GridRowMoveEventArgs e) => RowMoved?.Invoke(this, e);

    [EditorBrowsable(EditorBrowsableState.Never)]
    public void HandleFrameRendered(GridFrameEventArgs e) => FrameRendered?.Invoke(this, e);
}
