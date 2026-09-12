namespace ClamGrid.Tests.Internal.Layout;

public sealed class GridColumnSizerTests
{
    [Fact]
    public void FixedAndAutoWidthsLeaveTheRemainderForStars()
    {
        var widths = GridColumnSizer.Resolve([new(GridColumnWidth.Absolute(80), 32, 0), new(GridColumnWidth.Auto, 32, 120), new(GridColumnWidth.Star(), 32, 0), new(GridColumnWidth.Star(2), 32, 0)], 500);
        Assert.Equal([80d, 120d, 100d, 200d], widths);
    }

    [Fact]
    public void MinimumWidthsRedistributeStarSpaceWithoutDependingOnColumnOrder()
    {
        var widths = GridColumnSizer.Resolve([new(GridColumnWidth.Star(), 80, 0), new(GridColumnWidth.Star(), 0, 0), new(GridColumnWidth.Star(), 120, 0)], 300);
        Assert.Equal([90d, 90d, 120d], widths);
    }

    [Fact]
    public void NarrowViewportKeepsMinimumWidthsAndAllowsHorizontalScrolling()
    {
        var widths = GridColumnSizer.Resolve([new(GridColumnWidth.Absolute(100), 32, 0), new(GridColumnWidth.Star(), 60, 0), new(GridColumnWidth.Star(), 60, 0)], 150);
        Assert.Equal([100d, 60d, 60d], widths);
    }

    [Fact]
    public void LargeFiniteWeightsDoNotOverflowTheDistribution()
    {
        var widths = GridColumnSizer.Resolve([new(GridColumnWidth.Star(Double.MaxValue), 0, 0), new(GridColumnWidth.Star(Double.MaxValue), 0, 0)], 100);
        Assert.Equal([50d, 50d], widths);
    }
}
