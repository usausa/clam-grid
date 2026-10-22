namespace ClamGrid.Tests;

public sealed class GridStyleTests
{
    [Fact]
    public void ValidStylesAreAccepted()
    {
        // Act & Assert
        Assert.True(GridStyle.ValidateValue(new GridStyle()));
        Assert.True(GridStyle.ValidateValue(new GridStyle { HorizontalPadding = 0, VerticalPadding = 0, RowHeight = 40, HeaderHeight = 32, RowHeaderWidth = 0 }));
        Assert.True(GridStyle.ValidateValue(new GridStyle { ScrollBarThickness = 1, ScrollBarVisible = false }));
    }

    [Fact]
    public void ScrollBarDefaultsKeepTheDrawnAppearance()
    {
        // Arrange
        var style = new GridStyle();

        // Assert
        Assert.True(style.ScrollBarVisible);
        Assert.Equal(4, style.ScrollBarThickness);
        Assert.Equal(unchecked((int)0x8C64748B), style.ScrollBarColor.ToInt());
    }

    [Fact]
    public void InvalidStylesAreRejected()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(static () => GridStyle.ValidateValue(null));
        Assert.Throws<ArgumentException>(static () => GridStyle.ValidateValue(new GridStyle { FontSize = 0 }));
        Assert.Throws<ArgumentException>(static () => GridStyle.ValidateValue(new GridStyle { VerticalPadding = Double.NaN }));
        Assert.Throws<ArgumentException>(static () => GridStyle.ValidateValue(new GridStyle { RowHeight = 0 }));
        Assert.Throws<ArgumentException>(static () => GridStyle.ValidateValue(new GridStyle { HeaderHeight = Double.PositiveInfinity }));
        Assert.Throws<ArgumentException>(static () => GridStyle.ValidateValue(new GridStyle { RowHeaderWidth = -1 }));
        Assert.Throws<ArgumentException>(static () => GridStyle.ValidateValue(new GridStyle { ScrollBarThickness = 0 }));
        Assert.Throws<ArgumentException>(static () => GridStyle.ValidateValue(new GridStyle { ScrollBarThickness = Double.NaN }));
    }

    [Fact]
    public void InvalidSizesAreRejectedWhenTheRendererIsCreated()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(static () => new GridRenderer(new GridStyle { FontSize = Double.NaN }));
        Assert.Throws<ArgumentException>(static () => new GridRenderer(new GridStyle { RowHeight = -1 }));
    }
}
