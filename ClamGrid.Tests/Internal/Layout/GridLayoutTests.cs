namespace ClamGrid.Tests.Internal.Layout;

public sealed class GridLayoutTests
{
    [Fact]
    public void VisibleRangesIncludePartialCellsAndExcludeExactEndBoundaries()
    {
        var layout = new GridLayout([100, 100, 100], 10000, 40, 40, 40, 240, 440);
        Assert.Equal(new GridIndexRange(0, 10), layout.VisibleRows);
        Assert.Equal(new GridIndexRange(0, 2), layout.VisibleColumns);
        layout.ScrollTo(50, 20);
        Assert.Equal(new GridIndexRange(0, 11), layout.VisibleRows);
        Assert.Equal(new GridIndexRange(0, 3), layout.VisibleColumns);
    }

    [Fact]
    public void HitTestUsesTheSameScrolledCoordinatesAsDrawing()
    {
        var layout = new GridLayout([100, 160, 90], 100, 40, 32, 48, 280, 320);
        layout.ScrollTo(75, 65);
        var bounds = layout.GetCellBounds(2, 1);
        Assert.Equal(new GridHit(GridCellType.Cell, 2, 1), layout.HitTest(bounds.X + 1, bounds.Y + 1));
        Assert.Equal(new GridHit(GridCellType.ColumnHeader, -1, 1), layout.HitTest(bounds.X + 1, 10));
        Assert.Equal(new GridHit(GridCellType.RowHeader, 2, -1), layout.HitTest(10, bounds.Y + 1));
        Assert.Equal(new GridHit(GridCellType.CornerHeader, -1, -1), layout.HitTest(10, 10));
    }

    [Fact]
    public void SharedEdgesBelongToTheNextCell()
    {
        var layout = new GridLayout([100, 100], 3, 40, 32, 48, 300, 200);
        Assert.Equal(new GridHit(GridCellType.Cell, 1, 1), layout.HitTest(148, 72));
        Assert.Equal(GridHit.None, layout.HitTest(248, 72));
        Assert.Equal(GridHit.None, layout.HitTest(60, 152));
        Assert.Equal(GridHit.None, layout.HitTest(-1, 20));
        Assert.Equal(GridHit.None, layout.HitTest(Double.NaN, 20));
    }

    [Fact]
    public void ZeroWidthColumnsAreNotHitAtTheirSharedBoundary()
    {
        var layout = new GridLayout([0, 0, 100, 0, 100], 2, 40, 0, 0, 100, 80);
        Assert.Equal(new GridHit(GridCellType.Cell, 0, 2), layout.HitTest(0, 0));
        Assert.Equal(new GridIndexRange(2, 3), layout.VisibleColumns);
        layout.ScrollTo(100, 0);
        Assert.Equal(new GridHit(GridCellType.Cell, 0, 4), layout.HitTest(0, 0));
    }

    [Fact]
    public void ScrollIntoViewMovesMinimallyAndClampsAtTheEnd()
    {
        var layout = new GridLayout([100, 100, 100], 10000, 40, 40, 40, 240, 440);
        Assert.True(layout.ScrollIntoView(9999, 2));
        Assert.Equal(399600, layout.ScrollY);
        Assert.Equal(100, layout.ScrollX);
        Assert.Equal(new GridIndexRange(9990, 10000), layout.VisibleRows);
        Assert.True(layout.ScrollIntoView(9999, 2));
        Assert.Equal(399600, layout.ScrollY);
        Assert.False(layout.ScrollIntoView(10000, 2));
        Assert.Equal(399600, layout.ScrollY);
    }

    [Fact]
    public void OversizedCellAlignsItsLeadingEdge()
    {
        var layout = new GridLayout([500, 500], 10, 200, 0, 0, 100, 100);
        Assert.True(layout.ScrollIntoView(2, 1));
        Assert.Equal(500, layout.ScrollX);
        Assert.Equal(400, layout.ScrollY);
    }

    [Fact]
    public void EmptyViewportAndDataDoNotCreatePhantomCells()
    {
        var layout = new GridLayout([], 0, 40, 40, 40, 0, 0);
        Assert.Equal(0, layout.VisibleRows.Count);
        Assert.Equal(0, layout.VisibleColumns.Count);
        Assert.Equal(GridHit.None, layout.HitTest(0, 0));
        Assert.False(layout.ScrollIntoView(0, 0));
        Assert.False(layout.ScrollTo(Double.NaN, 0));
    }

    [Fact]
    public void VisibleWorkIsIndependentOfTotalRowCount()
    {
        var small = new GridLayout([100, 100, 100], 1000, 40, 40, 40, 240, 440);
        var large = new GridLayout([100, 100, 100], 50000, 40, 40, 40, 240, 440);
        small.ScrollTo(20, 400);
        large.ScrollTo(20, 400);
        Assert.Equal(small.VisibleRows, large.VisibleRows);
        Assert.Equal(small.VisibleColumns, large.VisibleColumns);
    }

    [Fact]
    public void BoundaryHitUsesVisibleHeaderEdgesAfterScrolling()
    {
        var layout = new GridLayout([100, 80, 120], 10, 40, 32, 48, 260, 200);
        Assert.Equal(0, layout.HitTestColumnBoundary(146, 20));
        Assert.Equal(-1, layout.HitTestColumnBoundary(146, 33));
        Assert.Equal(-1, layout.HitTestColumnBoundary(49, 20));
        layout.ScrollTo(60, 0);
        Assert.Equal(0, layout.HitTestColumnBoundary(88, 20));
        Assert.Equal(1, layout.HitTestColumnBoundary(168, 20));
        Assert.Equal(-1, layout.HitTestColumnBoundary(255, 20));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2.625)]
    [InlineData(3.5)]
    public void PixelCoordinatesResolveToTheDrawnCell(double density)
    {
        var transform = new GridCoordinateTransform(density, density);
        var layout = new GridLayout([100, 100], 20, 40, 32, 48, 240, 320);
        layout.ScrollTo(30, 60);
        var pixels = transform.ToPixels(layout.GetCellBounds(3, 1));
        var point = transform.ToDip(pixels.X + 1, pixels.Y + 1);
        Assert.Equal(new GridHit(GridCellType.Cell, 3, 1), layout.HitTest(point.X, point.Y));
    }
}
