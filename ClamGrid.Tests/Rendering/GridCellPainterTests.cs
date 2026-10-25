namespace ClamGrid.Tests.Rendering;

using SkiaSharp;
using SkiaSharp.Views.Maui;

public sealed class GridCellPainterTests
{
    private const int ViewportWidth = 300;

    private const int ViewportHeight = 200;

    private const double RowHeight = 40;

    [Fact]
    public void PainterIsCalledForTheVisibleCellsOfItsColumn()
    {
        // Arrange
        var calls = new List<GridCellPaintContext>();
        var items = CreateItems(100);
        var columns = CreateColumns((_, context) => calls.Add(context));
        var layout = CreateLayout([100, 80, 120], items.Length);
        layout.ScrollTo(0, 60);
        using var renderer = CreateRenderer();
        using var bitmap = new SKBitmap(ViewportWidth, ViewportHeight);
        using var canvas = new SKCanvas(bitmap);

        // Act
        var rendered = renderer.Render(canvas, layout, columns, items, null, false);

        // Assert
        Assert.Equal([1, 2, 3, 4, 5, 6], calls.Select(static x => x.RowIndex));
        Assert.All(calls, static x => Assert.Equal(1, x.ColumnIndex));
        Assert.All(calls, x => Assert.Same(columns[1], x.Column));
        Assert.All(calls, x => Assert.Same(items[x.RowIndex], x.Item));
        Assert.All(calls, x => Assert.Same(items[x.RowIndex].Values, x.Value));
        Assert.Equal(new SKRect(100, -20, 180, 20), calls[0].Bounds);
        Assert.Equal(new SKRect(100, 180, 180, 220), calls[^1].Bounds);
        Assert.Equal(18, rendered);
    }

    [Fact]
    public void FrozenAndScrolledPainterCellsAreClippedToTheirArea()
    {
        // Arrange
        var calls = new List<(int Column, SKRect Bounds, SKRectI Clip)>();
        void Record(SKCanvas canvas, GridCellPaintContext context) => calls.Add((context.ColumnIndex, context.Bounds, canvas.DeviceClipBounds));
        var items = CreateItems(3);
        GridColumn[] columns = [Column("frozen", Record), Column("plain", null), Column("scrolled", Record)];
        var layout = CreateLayout([100, 150, 150], items.Length, 1);
        layout.ScrollTo(50, 0);
        using var renderer = CreateRenderer();
        using var bitmap = new SKBitmap(ViewportWidth, ViewportHeight);
        using var canvas = new SKCanvas(bitmap);

        // Act
        renderer.Render(canvas, layout, columns, items, null, false);

        // Assert
        Assert.Equal([2, 2, 2, 0, 0, 0], calls.Select(static x => x.Column));
        Assert.Equal(new SKRect(200, 0, 350, 40), calls[0].Bounds);
        Assert.Equal(new SKRectI(200, 0, 300, 40), calls[0].Clip);
        Assert.Equal(new SKRect(0, 80, 100, 120), calls[^1].Bounds);
        Assert.Equal(new SKRectI(0, 80, 100, 120), calls[^1].Clip);
    }

    [Fact]
    public void PainterCannotChangeTheCanvasForTheNextCells()
    {
        // Arrange
        var states = new List<(SKMatrix Matrix, int SaveCount, SKRectI Clip)>();
        var items = CreateItems(3);
        var columns = CreateColumns((canvas, _) =>
        {
            states.Add((canvas.TotalMatrix, canvas.SaveCount, canvas.DeviceClipBounds));
            using var paint = new SKPaint();
            paint.Color = SKColors.Red;
            canvas.DrawRect(new SKRect(-1000, -1000, 1000, 1000), paint);
            canvas.Save();
            canvas.Translate(10, 10);
            canvas.ClipRect(new SKRect(0, 0, 1, 1));
        });
        var layout = CreateLayout([100, 80, 120], items.Length);
        using var renderer = CreateRenderer();
        using var bitmap = new SKBitmap(ViewportWidth, ViewportHeight);
        using var canvas = new SKCanvas(bitmap);
        var saveCount = canvas.SaveCount;

        // Act
        renderer.Render(canvas, layout, columns, items, null, false);

        // Assert
        Assert.Equal(3, states.Count);
        Assert.All(states, static x => Assert.Equal(SKMatrix.Identity, x.Matrix));
        Assert.All(states, x => Assert.Equal(states[0].SaveCount, x.SaveCount));
        Assert.Equal([new SKRectI(100, 0, 180, 40), new SKRectI(100, 40, 180, 80), new SKRectI(100, 80, 180, 120)], states.Select(static x => x.Clip));
        Assert.Equal(saveCount, canvas.SaveCount);
        Assert.Equal(SKColors.Red, bitmap.GetPixel(140, 20));
        Assert.Equal(SKColors.White, bitmap.GetPixel(50, 20));
        Assert.Equal(SKColors.White, bitmap.GetPixel(140, 160));
    }

    [Fact]
    public void PainterReplacesTheTextAndTheCheckBox()
    {
        // Arrange
        var items = CreateItems(3);
        GridColumn[] plain = [Column("text", null) with { ValueAccessor = new GridValueAccessor<Item, string>(static _ => "WWWWWW") }, Column("check", null) with { ValueAccessor = new GridValueAccessor<Item, bool>(static _ => true), IsBoolean = true }];
        GridColumn[] painted = [plain[0] with { CellPainter = static (_, _) => { } }, plain[1] with { CellPainter = static (_, _) => { } }];
        var layout = CreateLayout([150, 150], items.Length);
        using var renderer = CreateRenderer();

        // Act
        var (plainText, plainCheck) = CountInkedPixels(renderer, layout, plain, items);
        var (paintedText, paintedCheck) = CountInkedPixels(renderer, layout, painted, items);

        // Assert
        Assert.True(plainText > 0);
        Assert.True(plainCheck > 0);
        Assert.Equal(0, paintedText);
        Assert.Equal(0, paintedCheck);
    }

    [Fact]
    public void PainterIsDrawnOverTheBackgroundAndUnderTheGridLines()
    {
        // Arrange
        var items = CreateItems(3);
        var columns = CreateColumns(static (canvas, context) =>
        {
            using var paint = new SKPaint();
            paint.Color = SKColors.Red;
            canvas.DrawRect(context.Bounds, paint);
        });
        var style = new GridStyle { ScrollBarVisible = false, AlternatingRowBackground = Colors.Yellow };
        var layout = CreateLayout([100, 80, 120], items.Length);
        using var renderer = new GridRenderer(style);
        using var bitmap = new SKBitmap(ViewportWidth * 4, ViewportHeight * 4);
        using var canvas = new SKCanvas(bitmap);
        canvas.Scale(4);

        // Act
        renderer.Render(canvas, layout, columns, items, null, false);

        // Assert
        Assert.Equal(SKColors.Red, bitmap.GetPixel(560, 80));
        Assert.Equal(SKColors.Red, bitmap.GetPixel(560, 240));
        Assert.Equal(style.GridLineColor.ToSKColor(), bitmap.GetPixel(560, 159));
        Assert.Equal(style.GridLineColor.ToSKColor(), bitmap.GetPixel(719, 80));
    }

    [Fact]
    public void PainterReceivesTheSelectionAndTheFinalColors()
    {
        // Arrange
        var calls = new List<GridCellPaintContext>();
        using var view = new GridDataView<Item>(CreateItems(3), static x => x.Id);
        view.SetSelected(1, true);
        var columns = CreateColumns((_, context) => calls.Add(context));
        var style = new GridStyle { CellColors = static context => context.RowIndex == 2 ? new GridColors(Background: Colors.Yellow) : default };
        var layout = CreateLayout([100, 80, 120], view.Items.Count);
        using var renderer = new GridRenderer(style);
        using var bitmap = new SKBitmap(ViewportWidth, ViewportHeight);
        using var canvas = new SKCanvas(bitmap);

        // Act
        renderer.Render(canvas, layout, columns, view.Items, view, false);

        // Assert
        Assert.Equal([false, true, false], calls.Select(static x => x.IsSelected));
        Assert.Equal(new GridColors(style.TextColor, style.Background), calls[0].Colors);
        Assert.Equal(new GridColors(style.SelectedTextColor, style.SelectedBackground), calls[1].Colors);
        Assert.Equal(new GridColors(style.TextColor, Colors.Yellow), calls[2].Colors);
    }

    [Fact]
    public void AutoWidthOfAPainterColumnComesFromTheHeader()
    {
        // Arrange
        var items = CreateItems(3);
        var column = new GridColumn("trend", "A", new GridValueAccessor<Item, string>(static _ => "WWWWWWWWWWWWWWWWWWWW")) { MinWidth = 0 };
        using var renderer = CreateRenderer();

        // Act
        var widths = renderer.MeasureColumns([column, column with { CellPainter = static (_, _) => { } }], items, 1000, 10, null);
        var header = renderer.MeasureColumns([column], [], 1000, 10, null);

        // Assert
        Assert.True(widths[0] > widths[1]);
        Assert.Equal(header[0], widths[1]);
    }

    [Fact]
    public void PainterAddsNoAllocationPerFrame()
    {
        // Arrange
        var items = CreateItems(100);
        GridColumn[] columns = [Column("a", static (_, _) => { }), Column("b", static (_, _) => { })];
        var layout = CreateLayout([150, 150], items.Length);
        using var renderer = CreateRenderer();
        using var bitmap = new SKBitmap(ViewportWidth, ViewportHeight);
        using var canvas = new SKCanvas(bitmap);
        layout.ScrollTo(0, 20);
        renderer.Render(canvas, layout, columns, items, null, false);
        layout.ScrollTo(0, 100);

        // Act
        var before = GC.GetAllocatedBytesForCurrentThread();
        renderer.Render(canvas, layout, columns, items, null, false);
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        // Assert
        Assert.Equal(0L, allocated);
    }

    private static Item[] CreateItems(int count) => Enumerable.Range(0, count).Select(static x => new Item(x, String.Empty, [x, x + 1, x + 2])).ToArray();

    private static GridColumn[] CreateColumns(Action<SKCanvas, GridCellPaintContext> painter) => [Column("name", null), Column("trend", painter), Column("tail", null)];

    private static GridColumn Column(string key, Action<SKCanvas, GridCellPaintContext>? painter) =>
        key == "trend"
            ? new GridColumn(key, key, new GridValueAccessor<Item, double[]>(static x => x.Values)) { CellPainter = painter }
            : new GridColumn(key, key, new GridValueAccessor<Item, string>(static x => x.Name)) { CellPainter = painter };

    private static GridLayout CreateLayout(double[] widths, int rows, int frozen = 0) => new(widths, rows, RowHeight, 0, 0, ViewportWidth, ViewportHeight, frozen);

    private static GridRenderer CreateRenderer() => new(new GridStyle { ScrollBarVisible = false });

    private static (int Text, int Check) CountInkedPixels(GridRenderer renderer, GridLayout layout, GridColumn[] columns, Item[] items)
    {
        using var bitmap = new SKBitmap(ViewportWidth, ViewportHeight);
        using var canvas = new SKCanvas(bitmap);
        renderer.Render(canvas, layout, columns, items, null, false);
        return (Count(new SKRectI(0, 0, 149, 39)), Count(new SKRectI(150, 0, 299, 39)));

        int Count(SKRectI area)
        {
            var count = 0;
            for (var y = area.Top; y < area.Bottom; y++)
            {
                for (var x = area.Left; x < area.Right; x++)
                {
                    if (bitmap.GetPixel(x, y) != SKColors.White)
                    {
                        count++;
                    }
                }
            }

            return count;
        }
    }

    private sealed record Item(int Id, string Name, double[] Values);
}
