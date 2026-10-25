namespace ClamGrid.Tests.Rendering;

using SkiaSharp;
using SkiaSharp.Views.Maui;

public sealed class GridLineTests
{
    private const int ViewportWidth = 300;

    private const int ViewportHeight = 200;

    private const double RowHeight = 40;

    private const int Scale = 4;

    [Fact]
    public void DefaultsKeepTheDrawnAppearance()
    {
        // Arrange
        var style = new GridStyle();

        // Assert
        Assert.True(style.HorizontalLinesVisible);
        Assert.True(style.VerticalLinesVisible);
        Assert.Equal(0.5, style.GridLineWidth);
    }

    [Fact]
    public void LineVisibilityChoosesTheLinesOfTheBody()
    {
        // Arrange
        var modes = new[]
        {
            (Horizontal: true, Vertical: true),
            (Horizontal: true, Vertical: false),
            (Horizontal: false, Vertical: true),
            (Horizontal: false, Vertical: false)
        };

        // Act & Assert
        foreach (var (horizontal, vertical) in modes)
        {
            var style = new GridStyle { ScrollBarVisible = false, HorizontalLinesVisible = horizontal, VerticalLinesVisible = vertical };
            using var bitmap = Render(style);
            var line = style.GridLineColor.ToSKColor();
            Assert.Equal(horizontal, CountOnRow(bitmap, 159, line) > 500);
            Assert.Equal(vertical, CountOnColumn(bitmap, 719, line) > 200);
        }
    }

    [Fact]
    public void LineWidthFollowsTheStyle()
    {
        // Arrange
        var thin = new GridStyle { ScrollBarVisible = false };
        var thick = new GridStyle { ScrollBarVisible = false, GridLineWidth = 2 };

        // Act
        using var thinBitmap = Render(thin);
        using var thickBitmap = Render(thick);

        // Assert
        var line = thin.GridLineColor.ToSKColor();
        Assert.True(CountOnColumn(thinBitmap, 560, line, 150, 170) < CountOnColumn(thickBitmap, 560, line, 150, 170));
    }

    private static SKBitmap Render(GridStyle style)
    {
        Item[] items = [new("a"), new("b"), new("c")];
        GridColumn[] columns = [Column("first"), Column("second"), Column("third")];
        var layout = new GridLayout([100, 80, 120], items.Length, RowHeight, 0, 0, ViewportWidth, ViewportHeight);
        using var renderer = new GridRenderer(style);
        var bitmap = new SKBitmap(ViewportWidth * Scale, ViewportHeight * Scale);
        using var canvas = new SKCanvas(bitmap);
        canvas.Scale(Scale);
        renderer.Render(canvas, layout, columns, items, null, false);
        return bitmap;
    }

    private static GridColumn Column(string key) => new(key, key, new GridValueAccessor<Item, string>(static x => x.Name));

    private static int CountOnRow(SKBitmap bitmap, int y, SKColor color)
    {
        var count = 0;
        for (var x = 0; x < bitmap.Width; x++)
        {
            if (bitmap.GetPixel(x, y) == color)
            {
                count++;
            }
        }

        return count;
    }

    private static int CountOnColumn(SKBitmap bitmap, int x, SKColor color, int start = 0, int end = Int32.MaxValue)
    {
        var count = 0;
        for (var y = Math.Max(0, start); y < Math.Min(bitmap.Height, end); y++)
        {
            if (bitmap.GetPixel(x, y) == color)
            {
                count++;
            }
        }

        return count;
    }

    private sealed record Item(string Name);
}
