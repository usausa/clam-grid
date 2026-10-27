namespace ClamGrid.Tests;

public sealed class ClamGridViewStyleTests
{
    [Fact]
    public void SnapshotMatchesTheDefaultsWhenNothingIsSet()
    {
        // Arrange
        using var grid = new ClamGridView();

        // Act & Assert
        Assert.Equal(new GridStyle(), grid.EffectiveStyle);
    }

    [Fact]
    public void SnapshotFollowsEveryChange()
    {
        // Arrange
        using var grid = new ClamGridView();

        // Act
        grid.FontSize = 20;
        grid.HeaderBackground = Colors.Red;
        grid.RowHeight = 40;
        grid.CornerText = "No";

        // Assert
        Assert.Equal(20d, grid.EffectiveStyle.FontSize);
        Assert.Equal(Colors.Red, grid.EffectiveStyle.HeaderBackground);
        Assert.Equal(40d, grid.EffectiveStyle.RowHeight);
        Assert.Equal("No", grid.EffectiveStyle.CornerText);
    }

    [Fact]
    public void BackgroundColorFillsTheGrid()
    {
        // Arrange
        using var grid = new ClamGridView();

        // Act & Assert
        Assert.Equal(Colors.White, grid.EffectiveStyle.Background);

        // Act
        grid.BackgroundColor = Colors.Black;

        // Assert
        Assert.Equal(Colors.Black, grid.EffectiveStyle.Background);
    }

    [Fact]
    public void ScrollBarDefaultsKeepTheDrawnAppearance()
    {
        // Arrange
        using var grid = new ClamGridView();

        // Assert
        Assert.True(grid.ScrollBarVisible);
        Assert.Equal(4, grid.ScrollBarThickness);
        Assert.Equal(unchecked((int)0x8C64748B), grid.ScrollBarColor.ToInt());
    }

    [Fact]
    public void InvalidSizesAreRejected()
    {
        // Arrange
        using var grid = new ClamGridView();

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => grid.FontSize = 0);
        Assert.Throws<ArgumentOutOfRangeException>(() => grid.VerticalPadding = Double.NaN);
        Assert.Throws<ArgumentOutOfRangeException>(() => grid.RowHeight = 0);
        Assert.Throws<ArgumentOutOfRangeException>(() => grid.HeaderHeight = Double.PositiveInfinity);
        Assert.Throws<ArgumentOutOfRangeException>(() => grid.RowHeaderWidth = -1);
        Assert.Throws<ArgumentOutOfRangeException>(() => grid.ScrollBarThickness = 0);
        Assert.Throws<ArgumentOutOfRangeException>(() => grid.GridLineWidth = Double.NaN);
    }

    [Fact]
    public void ZeroIsAcceptedWhereItHasAMeaning()
    {
        // Arrange
        using var grid = new ClamGridView();

        // Act
        grid.HorizontalPadding = 0;
        grid.VerticalPadding = 0;
        grid.RowHeaderWidth = 0;

        // Assert
        Assert.Equal(0d, grid.EffectiveStyle.HorizontalPadding);
        Assert.Equal(0d, grid.EffectiveStyle.VerticalPadding);
        Assert.Equal(0d, grid.EffectiveStyle.RowHeaderWidth);
    }

    [Fact]
    public void ThemeIsAppliedThroughAStyle()
    {
        // Arrange
        var style = new Style(typeof(ClamGridView));
        style.Setters.Add(new Setter { Property = ClamGridView.HeaderBackgroundProperty, Value = Colors.Red });
        style.Setters.Add(new Setter { Property = ClamGridView.FontFamilyProperty, Value = "sans-serif" });
        using var grid = new ClamGridView();

        // Act
        grid.Style = style;

        // Assert
        Assert.Equal(Colors.Red, grid.EffectiveStyle.HeaderBackground);
        Assert.Equal("sans-serif", grid.EffectiveStyle.FontFamily);
    }

    [Fact]
    public void BackgroundColorIsAppliedThroughAStyle()
    {
        // Arrange
        var style = new Style(typeof(ClamGridView));
        style.Setters.Add(new Setter { Property = VisualElement.BackgroundColorProperty, Value = Color.FromArgb("#121212") });
        using var grid = new ClamGridView();

        // Act
        grid.Style = style;

        // Assert
        Assert.Equal(Color.FromArgb("#121212"), grid.EffectiveStyle.Background);
    }

    [Fact]
    public void ColorsFollowADynamicResource()
    {
        // Arrange
        using var grid = new ClamGridView();
        grid.Resources = new ResourceDictionary { { "HeaderBackground", Colors.Red } };

        // Act
        grid.SetDynamicResource(ClamGridView.HeaderBackgroundProperty, "HeaderBackground");

        // Assert
        Assert.Equal(Colors.Red, grid.EffectiveStyle.HeaderBackground);

        // Act
        grid.Resources["HeaderBackground"] = Colors.Blue;

        // Assert
        Assert.Equal(Colors.Blue, grid.EffectiveStyle.HeaderBackground);
    }

    [Fact]
    public void ColorCallbacksAreBindableProperties()
    {
        // Arrange
        var background = static (object item) => (Color?)Colors.Red;
        using var grid = new ClamGridView();

        // Act
        grid.RowBackground = background;

        // Assert
        Assert.Same(background, grid.EffectiveStyle.RowBackground);
        Assert.Same(background, grid.GetValue(ClamGridView.RowBackgroundProperty));
    }
}
