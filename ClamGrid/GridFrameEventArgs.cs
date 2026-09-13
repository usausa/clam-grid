namespace ClamGrid;

public sealed class GridFrameEventArgs(double milliseconds, int renderedCells, int textMeasurements, GridIndexRange rows, GridIndexRange columns, int frozenColumns, double scrollX, double scrollY) : EventArgs
{
    public double Milliseconds { get; } = milliseconds;

    public int RenderedCells { get; } = renderedCells;

    public int TextMeasurements { get; } = textMeasurements;

    public GridIndexRange Rows { get; } = rows;

    public GridIndexRange Columns { get; } = columns;

    public int FrozenColumns { get; } = frozenColumns;

    public double ScrollX { get; } = scrollX;

    public double ScrollY { get; } = scrollY;
}
