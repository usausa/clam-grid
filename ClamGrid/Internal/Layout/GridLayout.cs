namespace ClamGrid.Internal.Layout;

internal sealed class GridLayout
{
    private readonly double[] edges;

    public int RowCount { get; }

    public int ColumnCount => edges.Length - 1;

    public double RowHeight { get; }

    public double HeaderHeight { get; }

    public double RowHeaderWidth { get; }

    public double ViewportWidth { get; }

    public double ViewportHeight { get; }

    public double ContentWidth => edges[^1];

    public double ContentHeight => RowCount * RowHeight;

    public double ScrollX { get; private set; }

    public double ScrollY { get; private set; }

    public double MaximumScrollX => Math.Max(0, ContentWidth - BodyBounds.Width);

    public double MaximumScrollY => Math.Max(0, ContentHeight - BodyBounds.Height);

    public GridRect BodyBounds => new(RowHeaderWidth, HeaderHeight, Math.Max(0, ViewportWidth - RowHeaderWidth), Math.Max(0, ViewportHeight - HeaderHeight));

    public GridRect ColumnHeaderArea => new(RowHeaderWidth, 0, BodyBounds.Width, Math.Min(HeaderHeight, ViewportHeight));

    public GridRect RowHeaderArea => new(0, HeaderHeight, Math.Min(RowHeaderWidth, ViewportWidth), BodyBounds.Height);

    public GridIndexRange VisibleRows
    {
        get
        {
            if (BodyBounds.Height <= 0)
            {
                return default;
            }

            return new GridIndexRange((int)Math.Min(RowCount, Math.Floor(ScrollY / RowHeight)), (int)Math.Min(RowCount, Math.Ceiling((ScrollY + BodyBounds.Height) / RowHeight)));
        }
    }

    public GridIndexRange VisibleColumns
    {
        get
        {
            if (BodyBounds.Width <= 0)
            {
                return default;
            }

            var start = Math.Clamp(UpperBound(ScrollX) - 1, 0, ColumnCount);
            var end = Math.Clamp(LowerBound(ScrollX + BodyBounds.Width), start, ColumnCount);
            return new GridIndexRange(start, end);
        }
    }

    public GridLayout(IReadOnlyList<double> columnWidths, int rowCount, double rowHeight, double headerHeight, double rowHeaderWidth, double viewportWidth, double viewportHeight)
    {
        ArgumentNullException.ThrowIfNull(columnWidths);
        ArgumentOutOfRangeException.ThrowIfNegative(rowCount);
        RequireDimension(rowHeight, nameof(rowHeight), false);
        RequireDimension(headerHeight, nameof(headerHeight));
        RequireDimension(rowHeaderWidth, nameof(rowHeaderWidth));
        RequireDimension(viewportWidth, nameof(viewportWidth));
        RequireDimension(viewportHeight, nameof(viewportHeight));
        if (!Double.IsFinite(rowCount * rowHeight))
        {
            throw new ArgumentOutOfRangeException(nameof(rowCount));
        }

        RowCount = rowCount;
        RowHeight = rowHeight;
        HeaderHeight = headerHeight;
        RowHeaderWidth = rowHeaderWidth;
        ViewportWidth = viewportWidth;
        ViewportHeight = viewportHeight;
        edges = new double[columnWidths.Count + 1];
        for (var i = 0; i < columnWidths.Count; i++)
        {
            RequireDimension(columnWidths[i], nameof(columnWidths));
            edges[i + 1] = edges[i] + columnWidths[i];
            RequireDimension(edges[i + 1], nameof(columnWidths));
        }
    }

    public bool ScrollTo(double x, double y)
    {
        if (!Double.IsFinite(x) || !Double.IsFinite(y))
        {
            return false;
        }

        var nextX = Math.Clamp(x, 0, MaximumScrollX);
        var nextY = Math.Clamp(y, 0, MaximumScrollY);
        if (nextX.Equals(ScrollX) && nextY.Equals(ScrollY))
        {
            return false;
        }

        ScrollX = nextX;
        ScrollY = nextY;
        return true;
    }

    public bool ScrollIntoView(int row, int column)
    {
        if ((row < 0) || (row >= RowCount) || (column < 0) || (column >= ColumnCount) || BodyBounds.IsEmpty)
        {
            return false;
        }

        var x = Reveal(edges[column], edges[column + 1], ScrollX, BodyBounds.Width);
        var y = Reveal(row * RowHeight, (row + 1d) * RowHeight, ScrollY, BodyBounds.Height);
        ScrollTo(x, y);
        return true;
    }

    public GridRect GetCellBounds(int row, int column) => new(RowHeaderWidth + edges[column] - ScrollX, HeaderHeight + (row * RowHeight) - ScrollY, edges[column + 1] - edges[column], RowHeight);

    public GridRect GetColumnHeaderBounds(int column) => new(RowHeaderWidth + edges[column] - ScrollX, 0, edges[column + 1] - edges[column], HeaderHeight);

    public GridRect GetRowHeaderBounds(int row) => new(0, HeaderHeight + (row * RowHeight) - ScrollY, RowHeaderWidth, RowHeight);

    public int HitTestColumnBoundary(double x, double y, double tolerance = 8)
    {
        if (!Double.IsFinite(tolerance) || (tolerance < 0) || !ColumnHeaderArea.Contains(x, y))
        {
            return -1;
        }

        var candidate = -1;
        var distance = tolerance;
        var visible = VisibleColumns;
        for (var column = visible.Start; column < visible.End; column++)
        {
            var rect = GetColumnHeaderBounds(column);
            var difference = Math.Abs(rect.Right - x);
            if ((rect.Width > 0) && (rect.Right > RowHeaderWidth) && (rect.Right <= ViewportWidth) && (difference <= distance))
            {
                candidate = column;
                distance = difference;
            }
        }

        return candidate;
    }

    public GridHit HitTest(double x, double y)
    {
        if (!new GridRect(0, 0, ViewportWidth, ViewportHeight).Contains(x, y))
        {
            return GridHit.None;
        }

        if ((x < RowHeaderWidth) && (y < HeaderHeight))
        {
            return new GridHit(GridCellType.CornerHeader, -1, -1);
        }

        var row = y < HeaderHeight ? -1 : Math.Floor((y - HeaderHeight + ScrollY) / RowHeight);
        var column = x < RowHeaderWidth ? -1 : UpperBound(x - RowHeaderWidth + ScrollX) - 1;
        if ((row >= RowCount) || (column >= ColumnCount))
        {
            return GridHit.None;
        }

        if (y < HeaderHeight)
        {
            return new GridHit(GridCellType.ColumnHeader, -1, column);
        }

        return new GridHit(x < RowHeaderWidth ? GridCellType.RowHeader : GridCellType.Cell, (int)row, column);
    }

    private static double Reveal(double start, double end, double scroll, double size)
    {
        if ((start < scroll) || ((end - start) > size))
        {
            return start;
        }

        return end > scroll + size ? end - size : scroll;
    }

    private static void RequireDimension(double value, string name, bool allowZero = true)
    {
        if (!Double.IsFinite(value) || (value < 0) || (!allowZero && (value == 0)))
        {
            throw new ArgumentOutOfRangeException(name);
        }
    }

    private int LowerBound(double value)
    {
        var low = 0;
        var high = edges.Length;
        while (low < high)
        {
            var middle = low + ((high - low) / 2);
            if (edges[middle] < value)
            {
                low = middle + 1;
            }
            else
            {
                high = middle;
            }
        }

        return low;
    }

    private int UpperBound(double value)
    {
        var low = 0;
        var high = edges.Length;
        while (low < high)
        {
            var middle = low + ((high - low) / 2);
            if (edges[middle] <= value)
            {
                low = middle + 1;
            }
            else
            {
                high = middle;
            }
        }

        return low;
    }
}
