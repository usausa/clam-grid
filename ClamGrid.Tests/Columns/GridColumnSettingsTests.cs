namespace ClamGrid.Tests.Columns;

public sealed class GridColumnSettingsTests
{
    [Fact]
    public void NullEmptyAndAllHiddenRestoreDefaultOrder()
    {
        // Arrange
        string[] keys = ["a", "b", "c"];
        GridColumnOrder[] expected = [new("a", true), new("b", true), new("c", true)];

        // Act & Assert
        Assert.Equal(expected, GridColumnSettings.Normalize(keys, null));
        Assert.Equal(expected, GridColumnSettings.Normalize(keys, []));
        Assert.Equal(expected, GridColumnSettings.Normalize(keys, [new("c", false), new("a", false)]));
    }

    [Fact]
    public void UnknownKeysAndDuplicatesAreRemovedAndNewKeysAreHidden()
    {
        // Arrange
        string[] keys = ["a", "b", "c"];
        GridColumnOrder[] orders = [new("unknown", true), new("b", false), new("b", true), new("a", true)];

        // Act
        var result = GridColumnSettings.Normalize(keys, orders);

        // Assert
        Assert.Equal([new("b", false), new("a", true), new GridColumnOrder("c", false)], result);
        Assert.Equal(result, GridColumnSettings.Normalize(keys, result));
    }

    [Fact]
    public void UnknownVisibleKeyDoesNotLeaveAnEmptyGrid()
    {
        // Act & Assert
        Assert.Equal([new GridColumnOrder("a", true)], GridColumnSettings.Normalize(["a"], [new("unknown", true), new("a", false)]));
        Assert.Empty(GridColumnSettings.Normalize([], [new("unknown", true)]));
    }

    [Fact]
    public void InvalidCatalogKeysAreDiagnosedBeforeApplying()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => GridColumnSettings.Normalize(["a", "a"], null));
        Assert.Throws<ArgumentException>(() => GridColumnSettings.Normalize([" "], null));
    }

    [Fact]
    public void SavedWidthsAreKeptForHiddenColumnsAndInvalidWidthsAreDropped()
    {
        // Arrange
        string[] keys = ["a", "b", "c"];
        GridColumnOrder[] orders = [new("a", true) { Width = 120 }, new("b", false) { Width = 80 }, new("c", true) { Width = Double.NaN }];

        // Act
        var result = GridColumnSettings.Normalize(keys, orders);
        var fallback = GridColumnSettings.Normalize(keys, [new("b", false) { Width = 80 }, new("c", false) { Width = -1 }]);

        // Assert
        Assert.Equal([new("a", true) { Width = 120 }, new("b", false) { Width = 80 }, new GridColumnOrder("c", true)], result);
        Assert.Equal([new("a", true), new("b", true) { Width = 80 }, new GridColumnOrder("c", true)], fallback);
    }

    [Fact]
    public void ArrangeAppliesSavedWidthsAboveTheMinimum()
    {
        // Arrange
        GridColumn[] catalog = [Column("a") with { Width = GridColumnWidth.Star() }, Column("b") with { MinWidth = 60 }, Column("c")];
        GridColumnOrder[] orders = [new("b", true) { Width = 30 }, new("a", true) { Width = 150 }, new("c", false) { Width = 90 }];

        // Act
        var visible = GridColumnSettings.Arrange(catalog, orders);
        var declared = GridColumnSettings.Arrange(catalog, [new GridColumnOrder("c", true)]);

        // Assert
        Assert.Equal(["b", "a"], visible.Select(static column => column.Key));
        Assert.Equal(GridColumnWidth.Absolute(60), visible[0].Width);
        Assert.Equal(GridColumnWidth.Absolute(150), visible[1].Width);
        Assert.Equal(GridColumnWidth.Star(), catalog[0].Width);
        Assert.Same(catalog[2], Assert.Single(declared));
    }

    [Fact]
    public void DirectEditsKeepOrDropTheSavedWidth()
    {
        // Arrange
        GridColumn[] catalog = [Column("a") with { Width = GridColumnWidth.Absolute(100) }, Column("b"), Column("c")];
        GridColumnOrder[] orders = [new("a", true) { Width = 150 }, new("b", true) { Width = 200 }, new("c", true)];
        var visible = GridColumnSettings.Arrange(catalog, orders);

        // Act
        var (unchanged, unchangedOrders) = GridColumnSettings.Merge(catalog, visible, orders);
        visible[0] = visible[0] with { Header = "A2" };
        visible[1] = visible[1] with { Width = GridColumnWidth.Absolute(80) };
        visible[2] = visible[2] with { Header = "C2" };
        var (edited, editedOrders) = GridColumnSettings.Merge(catalog, visible, orders);

        // Assert
        Assert.True(unchanged.Zip(catalog).All(static pair => ReferenceEquals(pair.First, pair.Second)));
        Assert.Equal(orders, unchangedOrders);
        Assert.Equal("A2", edited[0].Header);
        Assert.Equal(GridColumnWidth.Absolute(100), edited[0].Width);
        Assert.Same(visible[1], edited[1]);
        Assert.Same(visible[2], edited[2]);
        Assert.Equal([new("a", true) { Width = 150 }, new("b", true), new GridColumnOrder("c", true)], editedOrders);
    }

    private static GridColumn Column(string key) => new(key, key.ToUpperInvariant(), new GridValueAccessor<object, string>(static _ => String.Empty));
}
