namespace ClamGrid.Tests;

public sealed class GridColumnSizerTests
{
    [Fact]
    public void FixedAndAutoWidthsLeaveTheRemainderForStars()
    {
        // Arrange
        GridColumnWidthSpec[] columns = [new(GridColumnWidth.Absolute(80), 32, 0), new(GridColumnWidth.Auto, 32, 120), new(GridColumnWidth.Star(), 32, 0), new(GridColumnWidth.Star(2), 32, 0)];

        // Act
        var widths = GridColumnSizer.Resolve(columns, 500);

        // Assert
        Assert.Equal([80d, 120d, 100d, 200d], widths);
    }

    [Fact]
    public void MinimumWidthsRedistributeStarSpaceWithoutDependingOnColumnOrder()
    {
        // Arrange
        GridColumnWidthSpec[] columns = [new(GridColumnWidth.Star(), 80, 0), new(GridColumnWidth.Star(), 0, 0), new(GridColumnWidth.Star(), 120, 0)];

        // Act
        var widths = GridColumnSizer.Resolve(columns, 300);

        // Assert
        Assert.Equal([90d, 90d, 120d], widths);
    }

    [Fact]
    public void NarrowViewportKeepsMinimumWidthsAndAllowsHorizontalScrolling()
    {
        // Arrange
        GridColumnWidthSpec[] columns = [new(GridColumnWidth.Absolute(100), 32, 0), new(GridColumnWidth.Star(), 60, 0), new(GridColumnWidth.Star(), 60, 0)];

        // Act
        var widths = GridColumnSizer.Resolve(columns, 150);

        // Assert
        Assert.Equal([100d, 60d, 60d], widths);
    }

    [Fact]
    public void LargeFiniteWeightsDoNotOverflowTheDistribution()
    {
        // Arrange
        GridColumnWidthSpec[] columns = [new(GridColumnWidth.Star(Double.MaxValue), 0, 0), new(GridColumnWidth.Star(Double.MaxValue), 0, 0)];

        // Act
        var widths = GridColumnSizer.Resolve(columns, 100);

        // Assert
        Assert.Equal([50d, 50d], widths);
    }
}
