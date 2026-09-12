namespace ClamGrid;

public sealed class GridCellValueEventArgs(GridHit hit, object item, string columnKey, bool oldValue, bool newValue) : EventArgs
{
    public GridHit Hit { get; } = hit;

    public object Item { get; } = item;

    public string ColumnKey { get; } = columnKey;

    public bool OldValue { get; } = oldValue;

    public bool NewValue { get; } = newValue;

    public bool Cancel { get; set; }
}
