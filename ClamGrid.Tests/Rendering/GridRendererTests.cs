namespace ClamGrid.Tests.Rendering;

public sealed class GridRendererTests
{
    [Fact]
    public void FormatStringAppliesToFormattableValuesOnly()
    {
        // Arrange
        var date = new DateTime(2026, 9, 13, 8, 5, 0);

        // Act
        var number = GridRenderer.FormatValue(1234, "D6");
        var formattedDate = GridRenderer.FormatValue(date, "yyyyMMdd");
        var text = GridRenderer.FormatValue("text", "D6");
        var nothing = GridRenderer.FormatValue(null, "D6");
        var unformatted = GridRenderer.FormatValue(1234, null);

        // Assert
        Assert.Equal("001234", number);
        Assert.Equal("20260913", formattedDate);
        Assert.Equal("text", text);
        Assert.Equal(String.Empty, nothing);
        Assert.Equal("1234", unformatted);
    }

    [Fact]
    public void DefaultHeaderMarksOnlyThePrimarySortKey()
    {
        // Arrange
        using var view = CreateView([new("a", true), new("b")]);
        var style = new GridStyle();

        // Act
        var primary = GridRenderer.GetHeaderText(style, Column("a"), view);
        var secondary = GridRenderer.GetHeaderText(style, Column("b"), view);
        var unsorted = GridRenderer.GetHeaderText(style, Column("c"), view);
        var disabled = GridRenderer.GetHeaderText(style, Column("a") with { AllowSorting = false }, view);
        var unbound = GridRenderer.GetHeaderText(style, Column("a"), null);

        // Assert
        Assert.Equal("↓ A", primary);
        Assert.Equal("B", secondary);
        Assert.Equal("C", unsorted);
        Assert.Equal("A", disabled);
        Assert.Equal("A", unbound);
    }

    [Fact]
    public void CustomMarksCanFollowTheHeaderAndShowThePriority()
    {
        // Arrange
        using var view = CreateView([new("a", true), new("b")]);
        var style = new GridStyle { AscendingSortMark = "▲", DescendingSortMark = "▼", SortMarkPosition = GridSortMarkPosition.End, ShowSortPriority = true };

        // Act
        var primary = GridRenderer.GetHeaderText(style, Column("a"), view);
        var secondary = GridRenderer.GetHeaderText(style, Column("b"), view);
        var aliased = GridRenderer.GetHeaderText(style, Column("d") with { SortKey = "a" }, view);

        // Assert
        Assert.Equal("A ▼1", primary);
        Assert.Equal("B ▲2", secondary);
        Assert.Equal("D ▼1", aliased);
    }

    [Fact]
    public void PriorityIsOmittedForASingleSortKey()
    {
        // Arrange
        using var view = CreateView([new("b")]);
        var style = new GridStyle { AscendingSortMark = "▲", ShowSortPriority = true };

        // Act
        var header = GridRenderer.GetHeaderText(style, Column("b"), view);

        // Assert
        Assert.Equal("▲ B", header);
    }

    [Fact]
    public void EmptyMarkLeavesTheHeaderUnchanged()
    {
        // Arrange
        using var view = CreateView([new("a")]);
        var style = new GridStyle { AscendingSortMark = String.Empty };

        // Act
        var header = GridRenderer.GetHeaderText(style, Column("a"), view);

        // Assert
        Assert.Equal("A", header);
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
        var style = new GridStyle { RowHeaderText = static context => context.IsSelected ? "S" : null };
        var item = new Item(1);

        // Act
        var selected = GridRenderer.GetRowHeaderText(style, item, 4, true);
        var unselected = GridRenderer.GetRowHeaderText(style, item, 4, false);
        var plain = GridRenderer.GetRowHeaderText(new GridStyle(), item, 4, true);

        // Assert
        Assert.Equal("S", selected);
        Assert.Equal("5", unselected);
        Assert.Equal("5", plain);
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
}
