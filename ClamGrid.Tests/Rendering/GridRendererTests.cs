namespace ClamGrid.Tests.Rendering;

public sealed class GridRendererTests
{
    [Fact]
    public void FormatStringAppliesToFormattableValuesOnly()
    {
        // Arrange
        var column = Column("a");
        var date = new DateTime(2026, 9, 13, 8, 5, 0);

        // Act
        var number = GridRenderer.GetCellText(column with { Format = "D6" }, 1234);
        var formattedDate = GridRenderer.GetCellText(column with { Format = "yyyyMMdd" }, date);
        var text = GridRenderer.GetCellText(column with { Format = "D6" }, "text");
        var nothing = GridRenderer.GetCellText(column with { Format = "D6" }, null);
        var unformatted = GridRenderer.GetCellText(column, 1234);

        // Assert
        Assert.Equal("001234", number);
        Assert.Equal("20260913", formattedDate);
        Assert.Equal("text", text);
        Assert.Equal(String.Empty, nothing);
        Assert.Equal("1234", unformatted);
    }

    [Fact]
    public void ColumnFormatIsParsedWhenItIsSet()
    {
        // Arrange
        var column = Column("a") with { Format = "{0:N0} pcs" };

        // Act
        var renamed = column with { Header = "B" };
        var reformatted = column with { Format = "N0" };
        var cleared = column with { Format = null };

        // Assert
        Assert.Same(column.TextFormat, renamed.TextFormat);
        Assert.Equal("1,234 pcs", column.TextFormat.Format(1234, CultureInfo.InvariantCulture));
        Assert.Equal("1,234", reformatted.TextFormat.Format(1234, CultureInfo.InvariantCulture));
        Assert.Same(GridTextFormat.Default, cleared.TextFormat);
        Assert.Same(GridTextFormat.Default, Column("a").TextFormat);
        Assert.Equal(reformatted, column with { Format = "N0" });
        Assert.NotEqual(column, reformatted);
        Assert.Throws<FormatException>(static () => Column("a") with { Format = "{0:N0" });
    }

    [Fact]
    public void ConverterRunsBeforeTheFormatString()
    {
        // Arrange
        var plain = Column("a") with { Format = "D6" };
        var scaled = plain with { Converter = new ScaleConverter() };
        var marked = plain with { Converter = new MarkConverter() };

        // Act
        var formatted = GridRenderer.GetCellText(plain, 12);
        var converted = GridRenderer.GetCellText(scaled, 12);
        var text = GridRenderer.GetCellText(marked, 12);
        var nothing = GridRenderer.GetCellText(marked, null);

        // Assert
        Assert.Equal("000012", formatted);
        Assert.Equal("000120", converted);
        Assert.Equal("★12", text);
        Assert.Equal(String.Empty, nothing);
    }

    [Fact]
    public void DefaultHeaderMarksOnlyThePrimarySortKey()
    {
        // Arrange
        using var view = CreateView([new("a", true), new("b")]);
        using var renderer = new GridRenderer(new GridStyle());

        // Act
        var primary = renderer.GetHeaderMark(Column("a"), view);
        var secondary = renderer.GetHeaderMark(Column("b"), view);
        var unsorted = renderer.GetHeaderMark(Column("c"), view);
        var disabled = renderer.GetHeaderMark(Column("a") with { AllowSorting = false }, view);
        var unbound = renderer.GetHeaderMark(Column("a"), null);

        // Assert
        Assert.Equal("↓ ", primary);
        Assert.Equal(String.Empty, secondary);
        Assert.Equal(String.Empty, unsorted);
        Assert.Equal(String.Empty, disabled);
        Assert.Equal(String.Empty, unbound);
    }

    [Fact]
    public void CustomMarksCanFollowTheHeaderAndShowThePriority()
    {
        // Arrange
        using var view = CreateView([new("a", true), new("b")]);
        using var renderer = new GridRenderer(new GridStyle { AscendingSortMark = "▲", DescendingSortMark = "▼", SortMarkPosition = GridSortMarkPosition.End, SortPriorityVisible = true });

        // Act
        var primary = renderer.GetHeaderMark(Column("a"), view);
        var secondary = renderer.GetHeaderMark(Column("b"), view);
        var aliased = renderer.GetHeaderMark(Column("d") with { SortKey = "a" }, view);

        // Assert
        Assert.Equal(" ▼1", primary);
        Assert.Equal(" ▲2", secondary);
        Assert.Same(primary, aliased);
    }

    [Fact]
    public void PriorityIsOmittedForASingleSortKey()
    {
        // Arrange
        using var view = CreateView([new("b")]);
        using var renderer = new GridRenderer(new GridStyle { AscendingSortMark = "▲", SortPriorityVisible = true });

        // Act
        var mark = renderer.GetHeaderMark(Column("b"), view);

        // Assert
        Assert.Equal("▲ ", mark);
    }

    [Fact]
    public void EmptyMarkAddsNoSpace()
    {
        // Arrange
        using var view = CreateView([new("a")]);
        using var renderer = new GridRenderer(new GridStyle { AscendingSortMark = String.Empty });

        // Act
        var mark = renderer.GetHeaderMark(Column("a"), view);

        // Assert
        Assert.Equal(String.Empty, mark);
    }

    [Fact]
    public void HeaderLinesBreakAtEveryLineBreak()
    {
        // Act & Assert
        Assert.Equal(["対応", "開始日"], GridRenderer.GetTextLines("対応\n開始日"));
        Assert.Equal(["A", "B", "C"], GridRenderer.GetTextLines("A\r\nB\rC"));
        Assert.Equal(["A", String.Empty], GridRenderer.GetTextLines("A\n"));
        Assert.Equal([String.Empty], GridRenderer.GetTextLines(String.Empty));
    }

    [Fact]
    public void MultiLineHeaderIsMeasuredByItsWidestLine()
    {
        // Arrange
        using var renderer = new GridRenderer(new GridStyle());
        var single = Column("a") with { Header = "WWWWWWWW" };

        // Act
        var widths = renderer.MeasureColumns([single, single with { Header = "W\nWWWWWWWW" }, single with { Header = "WWWWWWWW\nW" }], [], 1000, 0, null);

        // Assert
        Assert.True(widths[0] > single.MinWidth);
        Assert.Equal(widths[0], widths[1]);
        Assert.Equal(widths[0], widths[2]);
    }

    [Fact]
    public void AutomaticHeightGrowsWithTheLines()
    {
        // Arrange
        using var renderer = new GridRenderer(new GridStyle());

        // Act
        var one = renderer.GetAutoHeight(1);
        var two = renderer.GetAutoHeight(2);

        // Assert
        Assert.Equal(renderer.AutoRowHeight, one);
        Assert.Equal(one, renderer.GetAutoHeight(0));
        Assert.True(two > one);
        Assert.True(two < one * 2);
    }

    [Fact]
    public void MeasurementUsesFallbackFontsForCharactersThePrimaryFontLacks()
    {
        // Arrange
        using var renderer = new GridRenderer(new GridStyle());
        var column = new GridColumn("id", "見出し 🍎 ▲", new GridValueAccessor<Item, int>(static x => x.Id));

        // Act
        var widths = renderer.MeasureColumns([column], [new Item(1)], 500, 1, null);

        // Assert
        Assert.True(widths[0] > 0);
        Assert.True(renderer.AutoRowHeight > 0);
        Assert.True(renderer.Measurements > 0);
    }

    [Fact]
    public void RowHeaderTextCallbackOverridesTheRowNumber()
    {
        // Arrange
        using var renderer = new GridRenderer(new GridStyle { RowHeaderText = static context => context.IsSelected ? "S" : null });
        using var plainRenderer = new GridRenderer(new GridStyle());
        var item = new Item(1);

        // Act
        var selected = renderer.GetRowHeaderText(item, 4, true);
        var unselected = renderer.GetRowHeaderText(item, 4, false);
        var plain = plainRenderer.GetRowHeaderText(item, 4, true);
        var large = plainRenderer.GetRowHeaderText(item, 999, false);

        // Assert
        Assert.Equal("S", selected);
        Assert.Equal("5", unselected);
        Assert.Equal("5", plain);
        Assert.Equal("1000", large);
        Assert.Same(large, plainRenderer.GetRowHeaderText(item, 999, true));
    }

    [Fact]
    public void AlternatingRowsYieldToRowAndColumnColors()
    {
        // Arrange
        var style = new GridStyle { Background = Colors.White, AlternatingRowBackground = Colors.LightGray };
        var plain = Column("a");
        var colored = plain with { Background = Colors.Yellow };

        // Act
        var even = GridRenderer.GetCellBackground(style, plain, 2, null);
        var odd = GridRenderer.GetCellBackground(style, plain, 3, null);
        var column = GridRenderer.GetCellBackground(style, colored, 3, null);
        var row = GridRenderer.GetCellBackground(style, colored, 3, Colors.Red);
        var unset = GridRenderer.GetCellBackground(new GridStyle { Background = Colors.White }, plain, 3, null);

        // Assert
        Assert.Equal(Colors.White, even);
        Assert.Equal(Colors.LightGray, odd);
        Assert.Equal(Colors.Yellow, column);
        Assert.Equal(Colors.Red, row);
        Assert.Equal(Colors.White, unset);
    }

    [Fact]
    public void AutoRowHeightIncludesFontsResolvedWhileMeasuring()
    {
        // Arrange
        using var renderer = new GridRenderer(new GridStyle());
        var before = renderer.AutoRowHeight;
        var column = new GridColumn("id", "日本語の見出し", new GridValueAccessor<Item, int>(static x => x.Id));

        // Act
        renderer.MeasureColumns([column], [new Item(1)], 500, 1, null);
        var after = renderer.AutoRowHeight;

        // Assert
        Assert.True(after >= before);
        Assert.Equal(after, renderer.AutoRowHeight);
    }

    private static GridDataView<Item> CreateView(GridSortOrder[] orders)
    {
        Item[] items = [new(1), new(2)];
        var view = new GridDataView<Item>(items, static x => x.Id);
        view.RegisterSort("a", static x => x.Id);
        view.RegisterSort("b", static x => -x.Id);
        view.RestoreSortOrders(orders);
        return view;
    }

    private static GridColumn Column(string key) => new(key, key.ToUpperInvariant(), new GridValueAccessor<Item, int>(static x => x.Id));

    private sealed record Item(int Id);

    // Keeps the value formattable
    private sealed class ScaleConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => value is int number ? number * 10 : value;

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotSupportedException();
    }

    // Produces text, so the format string no longer applies
    private sealed class MarkConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => value is null ? null : $"★{value}";

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotSupportedException();
    }
}
