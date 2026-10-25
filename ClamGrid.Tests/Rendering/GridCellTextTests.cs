namespace ClamGrid.Tests.Rendering;

using SkiaSharp;

public sealed class GridCellTextTests
{
    private const int ViewportWidth = 200;

    private const int ViewportHeight = 120;

    private const double RowHeight = 60;

    [Fact]
    public void LineBreaksStackOnlyWhenMoreLinesAreAllowed()
    {
        // Act
        var collapsed = InkBounds(Column(1), "AAAAAAAAAA\nBBBBBBBBBB");
        var stacked = InkBounds(Column(2), "AAAAAAAAAA\nBBBBBBBBBB");

        // Assert
        Assert.True(stacked.Height > collapsed.Height);
        Assert.True(stacked.Width < collapsed.Width);
    }

    [Fact]
    public void LinesBeyondTheAllowedCountAreNotDrawn()
    {
        // Act
        var two = InkBounds(Column(2), "A\nB\nC");
        var three = InkBounds(Column(3), "A\nB\nC");

        // Assert
        Assert.True(three.Height > two.Height);
    }

    [Fact]
    public void VerticalAlignmentMovesTheLines()
    {
        // Act
        var start = InkBounds(Column(1) with { VerticalAlignment = TextAlignment.Start }, "A");
        var center = InkBounds(Column(1), "A");
        var end = InkBounds(Column(1) with { VerticalAlignment = TextAlignment.End }, "A");

        // Assert
        Assert.True(start.Top < center.Top);
        Assert.True(center.Bottom < end.Bottom);
        Assert.True(end.Bottom <= RowHeight);
    }

    [Fact]
    public void WithoutALimitTheLinesFillTheRowHeight()
    {
        // Act
        var tall = InkBounds(Column(0), "A\nB\nC", 120);
        var compact = InkBounds(Column(0), "A\nB\nC", 40);
        var single = InkBounds(Column(1), "A\nB\nC", 120);

        // Assert
        Assert.True(tall.Height > compact.Height);
        Assert.True(tall.Height > single.Height);
    }

    [Fact]
    public void AutomaticWidthUsesTheWidestLine()
    {
        // Arrange
        using var renderer = new GridRenderer(new GridStyle());
        Item[] items = [new("AAAAAAAAAA\nBBBBBBBBBB")];

        // Act
        var collapsed = renderer.MeasureColumns([Column(1)], items, 500, 1, null);
        var stacked = renderer.MeasureColumns([Column(2)], items, 500, 1, null);
        var unlimited = renderer.MeasureColumns([Column(0)], items, 500, 1, null);

        // Assert
        Assert.True(stacked[0] < collapsed[0]);
        Assert.True(stacked[0] > 40);
        Assert.Equal(stacked[0], unlimited[0]);
    }

    private static GridColumn Column(int lineCount) =>
        new("text", "text", new GridValueAccessor<Item, string>(static x => x.Text)) { LineCount = lineCount };

    private static SKRectI InkBounds(GridColumn column, string text, double rowHeight = RowHeight)
    {
        using var renderer = new GridRenderer(new GridStyle { ScrollBarVisible = false, GridLineColor = Colors.White });
        var layout = new GridLayout([ViewportWidth], 1, rowHeight, 0, 0, ViewportWidth, ViewportHeight);
        using var bitmap = new SKBitmap(ViewportWidth, ViewportHeight);
        using var canvas = new SKCanvas(bitmap);
        renderer.Render(canvas, layout, [column], [new Item(text)], null, false);
        var left = ViewportWidth;
        var top = ViewportHeight;
        var right = 0;
        var bottom = 0;
        for (var y = 0; y < ViewportHeight; y++)
        {
            for (var x = 0; x < ViewportWidth; x++)
            {
                if (bitmap.GetPixel(x, y) != SKColors.White)
                {
                    left = Math.Min(left, x);
                    top = Math.Min(top, y);
                    right = Math.Max(right, x + 1);
                    bottom = Math.Max(bottom, y + 1);
                }
            }
        }

        return new SKRectI(left, top, right, bottom);
    }

    private sealed record Item(string Text);
}
