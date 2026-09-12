namespace ClamGrid;

public sealed class GridColumnWidthEventArgs(string columnKey, int columnIndex, double oldWidth, double newWidth) : EventArgs
{
    public string ColumnKey { get; } = columnKey;

    public int ColumnIndex { get; } = columnIndex;

    public double OldWidth { get; } = oldWidth;

    public double NewWidth { get; } = newWidth;

    public bool Cancel { get; set; }
}
