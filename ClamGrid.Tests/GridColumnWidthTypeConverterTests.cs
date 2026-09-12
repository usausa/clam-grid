namespace ClamGrid.Tests;

public sealed class GridColumnWidthTypeConverterTests
{
    [Theory]
    [InlineData("Auto", GridColumnWidthUnit.Auto, 0)]
    [InlineData("auto", GridColumnWidthUnit.Auto, 0)]
    [InlineData("*", GridColumnWidthUnit.Star, 1)]
    [InlineData("2*", GridColumnWidthUnit.Star, 2)]
    [InlineData("1.5 *", GridColumnWidthUnit.Star, 1.5)]
    [InlineData("85", GridColumnWidthUnit.Absolute, 85)]
    [InlineData(" 120.5 ", GridColumnWidthUnit.Absolute, 120.5)]
    public void XamlTextIsConvertedToWidth(string text, GridColumnWidthUnit unit, double value)
    {
        // Arrange
        var converter = new GridColumnWidthTypeConverter();

        // Act
        var width = (GridColumnWidth)converter.ConvertFromInvariantString(text)!;

        // Assert
        Assert.Equal(unit, width.Unit);
        Assert.Equal(value, width.Value);
        Assert.Equal(width, converter.ConvertFromInvariantString(converter.ConvertToInvariantString(width)!));
    }

    [Theory]
    [InlineData("")]
    [InlineData("abc")]
    [InlineData("-1")]
    [InlineData("0*")]
    public void InvalidTextIsRejected(string text)
    {
        // Arrange
        var converter = new GridColumnWidthTypeConverter();

        // Act & Assert
        Assert.ThrowsAny<Exception>(() => converter.ConvertFromInvariantString(text));
    }
}
