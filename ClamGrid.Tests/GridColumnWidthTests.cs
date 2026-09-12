namespace ClamGrid.Tests;

public sealed class GridColumnWidthTests
{
    [Theory]
    [InlineData(-1)]
    [InlineData(Double.NaN)]
    [InlineData(Double.PositiveInfinity)]
    [InlineData(Double.NegativeInfinity)]
    public void AbsoluteRejectsInvalidLayoutLengths(double value)
    {
        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => GridColumnWidth.Absolute(value));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(Double.NaN)]
    [InlineData(Double.PositiveInfinity)]
    [InlineData(Double.NegativeInfinity)]
    public void StarRejectsWeightsThatCannotBeDistributed(double value)
    {
        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => GridColumnWidth.Star(value));
    }

    [Fact]
    public void ZeroWidthIsDistinctFromAutomaticMeasurement()
    {
        // Act & Assert
        Assert.NotEqual(GridColumnWidth.Auto, GridColumnWidth.Absolute(0));
        Assert.Equal(GridColumnWidth.Auto, default);
    }
}
