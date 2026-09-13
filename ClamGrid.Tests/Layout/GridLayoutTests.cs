namespace ClamGrid.Tests.Layout;

public sealed class GridLayoutTests
{
    [Fact]
    public void VisibleRangesIncludePartialCellsAndExcludeExactEndBoundaries()
    {
        // Arrange
        var layout = new GridLayout([100, 100, 100], 10000, 40, 40, 40, 240, 440);

        // Act & Assert
        Assert.Equal(new GridIndexRange(0, 10), layout.VisibleRows);
        Assert.Equal(new GridIndexRange(0, 2), layout.VisibleColumns);

        // Act
        layout.ScrollTo(50, 20);

        // Assert
        Assert.Equal(new GridIndexRange(0, 11), layout.VisibleRows);
        Assert.Equal(new GridIndexRange(0, 3), layout.VisibleColumns);
    }

    [Fact]
    public void HitTestUsesTheSameScrolledCoordinatesAsDrawing()
    {
        // Arrange
        var layout = new GridLayout([100, 160, 90], 100, 40, 32, 48, 280, 320);
        layout.ScrollTo(75, 65);
        var bounds = layout.GetCellBounds(2, 1);

        // Act & Assert
        Assert.Equal(new GridHit(GridCellType.Cell, 2, 1), layout.HitTest(bounds.X + 1, bounds.Y + 1));
        Assert.Equal(new GridHit(GridCellType.ColumnHeader, -1, 1), layout.HitTest(bounds.X + 1, 10));
        Assert.Equal(new GridHit(GridCellType.RowHeader, 2, -1), layout.HitTest(10, bounds.Y + 1));
        Assert.Equal(new GridHit(GridCellType.CornerHeader, -1, -1), layout.HitTest(10, 10));
    }

    [Fact]
    public void SharedEdgesBelongToTheNextCell()
    {
        // Arrange
        var layout = new GridLayout([100, 100], 3, 40, 32, 48, 300, 200);

        // Act & Assert
        Assert.Equal(new GridHit(GridCellType.Cell, 1, 1), layout.HitTest(148, 72));
        Assert.Equal(GridHit.None, layout.HitTest(248, 72));
        Assert.Equal(GridHit.None, layout.HitTest(60, 152));
        Assert.Equal(GridHit.None, layout.HitTest(-1, 20));
        Assert.Equal(GridHit.None, layout.HitTest(Double.NaN, 20));
    }

    [Fact]
    public void ZeroWidthColumnsAreNotHitAtTheirSharedBoundary()
    {
        // Arrange
        var layout = new GridLayout([0, 0, 100, 0, 100], 2, 40, 0, 0, 100, 80);

        // Act & Assert
        Assert.Equal(new GridHit(GridCellType.Cell, 0, 2), layout.HitTest(0, 0));
        Assert.Equal(new GridIndexRange(2, 3), layout.VisibleColumns);

        // Act
        layout.ScrollTo(100, 0);

        // Assert
        Assert.Equal(new GridHit(GridCellType.Cell, 0, 4), layout.HitTest(0, 0));
    }

    [Fact]
    public void FrozenColumnsStayInPlaceWhileTheOthersScroll()
    {
        // Arrange
        var layout = new GridLayout([100, 100, 100, 100], 10, 40, 40, 40, 300, 440, 1);

        // Act
        layout.ScrollTo(50, 0);

        // Assert
        Assert.Equal(100d, layout.FrozenWidth);
        Assert.Equal(40d, layout.GetCellBounds(0, 0).X);
        Assert.Equal(90d, layout.GetCellBounds(0, 1).X);
        Assert.Equal(new GridRect(140, 40, 160, 400), layout.ScrollArea);
        Assert.Equal(new GridIndexRange(0, 1), layout.FrozenColumns);
        Assert.Equal(new GridIndexRange(1, 4), layout.VisibleColumns);
        Assert.Equal(new GridHit(GridCellType.Cell, 0, 0), layout.HitTest(100, 60));
        Assert.Equal(new GridHit(GridCellType.Cell, 0, 1), layout.HitTest(150, 60));
        Assert.Equal(new GridHit(GridCellType.ColumnHeader, -1, 2), layout.HitTest(250, 10));
    }

    [Fact]
    public void ScrollIntoViewRevealsScrollingColumnsInsideTheScrollArea()
    {
        // Arrange
        var layout = new GridLayout([100, 100, 100, 100], 10, 40, 40, 40, 300, 440, 1);
        layout.ScrollTo(50, 0);

        // Act
        var frozen = layout.ScrollIntoView(0, 0);
        var frozenScroll = layout.ScrollX;
        var revealed = layout.ScrollIntoView(0, 3);

        // Assert
        Assert.True(frozen);
        Assert.Equal(50d, frozenScroll);
        Assert.True(revealed);
        Assert.Equal(140d, layout.ScrollX);
        Assert.Equal(300d, layout.GetCellBounds(0, 3).Right);
    }

    [Fact]
    public void BoundariesUnderTheFrozenColumnsAreNotGrabbable()
    {
        // Arrange
        var layout = new GridLayout([100, 100, 100, 100], 10, 40, 40, 40, 300, 440, 1);
        layout.ScrollTo(150, 0);

        // Act & Assert
        Assert.Equal(0, layout.HitTestColumnBoundary(140, 10));
        Assert.Equal(-1, layout.HitTestColumnBoundary(90, 10));
        Assert.Equal(2, layout.HitTestColumnBoundary(190, 10));
    }

    [Fact]
    public void FrozenColumnCountIsLimitedToTheColumnCount()
    {
        // Arrange
        var layout = new GridLayout([100], 10, 40, 40, 40, 300, 440, 5);

        // Act & Assert
        Assert.Equal(1, layout.FrozenColumnCount);
        Assert.Equal(0d, layout.MaximumScrollX);
        Assert.Equal(new GridIndexRange(1, 1), layout.VisibleColumns);
    }

    [Fact]
    public void ScrollIntoViewMovesMinimallyAndClampsAtTheEnd()
    {
        // Arrange
        var layout = new GridLayout([100, 100, 100], 10000, 40, 40, 40, 240, 440);

        // Act & Assert
        Assert.True(layout.ScrollIntoView(9999, 2));
        Assert.Equal(399600, layout.ScrollY);
        Assert.Equal(100, layout.ScrollX);
        Assert.Equal(new GridIndexRange(9990, 10000), layout.VisibleRows);

        // Act & Assert
        Assert.True(layout.ScrollIntoView(9999, 2));
        Assert.Equal(399600, layout.ScrollY);

        // Act & Assert
        Assert.False(layout.ScrollIntoView(10000, 2));
        Assert.Equal(399600, layout.ScrollY);
    }

    [Fact]
    public void OversizedCellAlignsItsLeadingEdge()
    {
        // Arrange
        var layout = new GridLayout([500, 500], 10, 200, 0, 0, 100, 100);

        // Act
        var scrolled = layout.ScrollIntoView(2, 1);

        // Assert
        Assert.True(scrolled);
        Assert.Equal(500, layout.ScrollX);
        Assert.Equal(400, layout.ScrollY);
    }

    [Fact]
    public void EmptyViewportAndDataDoNotCreatePhantomCells()
    {
        // Arrange
        var layout = new GridLayout([], 0, 40, 40, 40, 0, 0);

        // Act & Assert
        Assert.Equal(0, layout.VisibleRows.Count);
        Assert.Equal(0, layout.VisibleColumns.Count);
        Assert.Equal(GridHit.None, layout.HitTest(0, 0));
        Assert.False(layout.ScrollIntoView(0, 0));
        Assert.False(layout.ScrollTo(Double.NaN, 0));
    }

    [Fact]
    public void VisibleWorkIsIndependentOfTotalRowCount()
    {
        // Arrange
        var small = new GridLayout([100, 100, 100], 1000, 40, 40, 40, 240, 440);
        var large = new GridLayout([100, 100, 100], 50000, 40, 40, 40, 240, 440);

        // Act
        small.ScrollTo(20, 400);
        large.ScrollTo(20, 400);

        // Assert
        Assert.Equal(small.VisibleRows, large.VisibleRows);
        Assert.Equal(small.VisibleColumns, large.VisibleColumns);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2.625)]
    [InlineData(3.5)]
    public void PixelCoordinatesResolveToTheDrawnCell(double density)
    {
        // Arrange
        var transform = new GridCoordinateTransform(density, density);
        var layout = new GridLayout([100, 100], 20, 40, 32, 48, 240, 320);
        layout.ScrollTo(30, 60);
        var pixels = transform.ToPixels(layout.GetCellBounds(3, 1));

        // Act
        var point = transform.ToDip(pixels.X + 1, pixels.Y + 1);

        // Assert
        Assert.Equal(new GridHit(GridCellType.Cell, 3, 1), layout.HitTest(point.X, point.Y));
    }
}
