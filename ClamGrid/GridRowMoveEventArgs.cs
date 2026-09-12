namespace ClamGrid;

public sealed class GridRowMoveEventArgs(object rowKey, int oldIndex, int newIndex) : EventArgs
{
    public object RowKey { get; } = rowKey;

    public int OldIndex { get; } = oldIndex;

    public int NewIndex { get; } = newIndex;

    public bool Cancel { get; set; }
}
