namespace ClamGrid;

public sealed class GridCellEventArgs(GridHit hit, Point position, object? item, string? columnKey) : EventArgs
{
    public GridHit Hit { get; } = hit;

    public Point Position { get; } = position;

    public object? Item { get; } = item;

    public string? ColumnKey { get; } = columnKey;

    public bool Handled { get; set; }
}
