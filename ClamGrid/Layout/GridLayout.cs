namespace ClamGrid.Layout;

internal sealed class GridLayout
{
    private readonly double[] edges;

    public int RowCount { get; }

    public int ColumnCount => edges.Length - 1;

    public int FrozenColumnCount { get; }

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

    // Width of the frozen columns, limited to the body so the scroll area never becomes negative
    public double FrozenWidth => Math.Min(edges[FrozenColumnCount], BodyBounds.Width);

    public GridRect FrozenArea => new(RowHeaderWidth, HeaderHeight, FrozenWidth, BodyBounds.Height);

    // Body area right of the frozen columns where the remaining columns scroll horizontally
    public GridRect ScrollArea => new(RowHeaderWidth + FrozenWidth, HeaderHeight, BodyBounds.Width - FrozenWidth, BodyBounds.Height);

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

    public GridIndexRange FrozenColumns => new(0, FrozenColumnCount);

    // Scrolling columns that intersect the scroll area; frozen columns are reported separately
    public GridIndexRange VisibleColumns
    {
        get
        {
            var area = ScrollArea;
            if (area.Width <= 0)
            {
                return new GridIndexRange(FrozenColumnCount, FrozenColumnCount);
            }

            var start = Math.Clamp(UpperBound(FrozenWidth + ScrollX) - 1, FrozenColumnCount, ColumnCount);
            var end = Math.Clamp(LowerBound(FrozenWidth + ScrollX + area.Width), start, ColumnCount);
            return new GridIndexRange(start, end);
        }
    }

    public GridLayout(IReadOnlyList<double> columnWidths, int rowCount, double rowHeight, double headerHeight, double rowHeaderWidth, double viewportWidth, double viewportHeight, int frozenColumnCount = 0)
    {
        ArgumentNullException.ThrowIfNull(columnWidths);
        ArgumentOutOfRangeException.ThrowIfNegative(rowCount);
        ArgumentOutOfRangeException.ThrowIfNegative(frozenColumnCount);
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
        FrozenColumnCount = Math.Min(frozenColumnCount, columnWidths.Count);
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

    // Frozen columns never need horizontal scrolling; other columns are revealed inside the scroll area
    public bool ScrollIntoView(int row, int column)
    {
        if ((row < 0) || (row >= RowCount) || (column < 0) || (column >= ColumnCount) || BodyBounds.IsEmpty)
        {
            return false;
        }

        var x = column < FrozenColumnCount ? ScrollX : Reveal(edges[column] - FrozenWidth, edges[column + 1] - FrozenWidth, ScrollX, ScrollArea.Width);
        var y = Reveal(row * RowHeight, (row + 1d) * RowHeight, ScrollY, BodyBounds.Height);
        ScrollTo(x, y);
        return true;
    }

    public GridRect GetCellBounds(int row, int column) => new(GetColumnLeft(column), HeaderHeight + (row * RowHeight) - ScrollY, edges[column + 1] - edges[column], RowHeight);

    public GridRect GetColumnHeaderBounds(int column) => new(GetColumnLeft(column), 0, edges[column + 1] - edges[column], HeaderHeight);

    public GridRect GetRowHeaderBounds(int row) => new(0, HeaderHeight + (row * RowHeight) - ScrollY, RowHeaderWidth, RowHeight);

    // Boundaries hidden under the frozen columns are not grabbable
    public int HitTestColumnBoundary(double x, double y, double tolerance = 8)
    {
        if (!Double.IsFinite(tolerance) || (tolerance < 0) || !ColumnHeaderArea.Contains(x, y))
        {
            return -1;
        }

        var candidate = -1;
        var distance = tolerance;
        Consider(FrozenColumns, RowHeaderWidth);
        Consider(VisibleColumns, RowHeaderWidth + FrozenWidth);
        return candidate;

        void Consider(GridIndexRange range, double left)
        {
            for (var column = range.Start; column < range.End; column++)
            {
                var rect = GetColumnHeaderBounds(column);
                var difference = Math.Abs(rect.Right - x);
                if ((rect.Width > 0) && (rect.Right > left) && (rect.Right <= ViewportWidth) && (difference <= distance))
                {
                    candidate = column;
                    distance = difference;
                }
            }
        }
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
        var column = x < RowHeaderWidth ? -1 : HitTestColumn(x - RowHeaderWidth);
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

    private double GetColumnLeft(int column) => RowHeaderWidth + edges[column] - (column < FrozenColumnCount ? 0 : ScrollX);

    // Offsets inside the frozen width map to frozen columns, the rest is shifted by the scroll offset
    private int HitTestColumn(double offset) => UpperBound(offset < FrozenWidth ? offset : offset + ScrollX) - 1;

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
