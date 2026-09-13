namespace ClamGrid.Tests;

public sealed class GridFontsTests
{
    [Fact]
    public void DefaultsPreferJapaneseGlyphsWithoutExtraTypefaces()
    {
        // Act
        var languages = GridFonts.Languages;
        var fallbacks = GridFonts.Fallbacks;

        // Assert
        Assert.Equal(["ja"], languages);
        Assert.Empty(fallbacks);
    }
}
