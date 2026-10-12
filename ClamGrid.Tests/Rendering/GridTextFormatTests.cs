namespace ClamGrid.Tests.Rendering;

public sealed class GridTextFormatTests
{
    [Fact]
    public void FormatsWithABraceAreCompositeAndOthersAreFormatStrings()
    {
        // Arrange
        var culture = CultureInfo.InvariantCulture;
        var date = new DateTime(2026, 9, 13);

        // Act & Assert
        Assert.Equal("1,234 pcs", GridTextFormat.Parse("{0:N0} pcs").Format(1234, culture));
        Assert.Equal("1,234", GridTextFormat.Parse("N0").Format(1234, culture));
        Assert.Equal("2026/09", GridTextFormat.Parse("yyyy/MM").Format(date, culture));
        Assert.Equal("Yamada san", GridTextFormat.Parse("{0} san").Format((object?)"Yamada", culture));
        Assert.Equal("text", GridTextFormat.Parse("D6").Format((object?)"text", culture));
        Assert.Equal(String.Empty, GridTextFormat.Parse("{0} san").Format(null, culture));
    }

    [Fact]
    public void EmptyFormatsShareTheDefaultText()
    {
        // Arrange
        var culture = CultureInfo.InvariantCulture;

        // Act
        var none = GridTextFormat.Parse(null);
        var empty = GridTextFormat.Parse(String.Empty);

        // Assert
        Assert.Same(GridTextFormat.Default, none);
        Assert.Same(GridTextFormat.Default, empty);
        Assert.True(none.IsDefault);
        Assert.False(GridTextFormat.Parse("N0").IsDefault);
        Assert.Equal("1234.5", none.Format(1234.5, culture));
        Assert.Equal("1234.5", none.Format((object?)1234.5, culture));
    }

    [Fact]
    public void InvalidCompositeFormatsAreRejectedWhenParsed()
    {
        // Act & Assert
        Assert.Throws<FormatException>(static () => GridTextFormat.Parse("{0:N0"));
    }

    [Fact]
    public void FormatsParsedFromTheSameTextAreEqual()
    {
        // Act & Assert
        Assert.Equal(GridTextFormat.Parse("{0:N0} pcs"), GridTextFormat.Parse("{0:N0} pcs"));
        Assert.Equal(GridTextFormat.Parse("{0:N0} pcs").GetHashCode(), GridTextFormat.Parse("{0:N0} pcs").GetHashCode());
        Assert.Equal(GridTextFormat.Parse("N0"), GridTextFormat.Parse("N0"));
        Assert.NotEqual(GridTextFormat.Parse("N0"), GridTextFormat.Parse("N1"));
        Assert.NotEqual(GridTextFormat.Parse("{0:N0}"), GridTextFormat.Parse("N0"));
        Assert.NotEqual(GridTextFormat.Default, GridTextFormat.Parse("N0"));
    }
}
