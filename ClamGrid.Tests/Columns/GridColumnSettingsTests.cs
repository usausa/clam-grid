namespace ClamGrid.Tests.Columns;

public sealed class GridColumnSettingsTests
{
    [Fact]
    public void NullEmptyAndAllHiddenRestoreDefaultOrder()
    {
        // Arrange
        GridColumn[] columns = [Column("a"), Column("b"), Column("c")];
        GridColumnState[] expected = [new("a", true), new("b", true), new("c", true)];

        // Act & Assert
        Assert.Equal(expected, GridColumnSettings.Normalize(columns, null));
        Assert.Equal(expected, GridColumnSettings.Normalize(columns, []));
        Assert.Equal(expected, GridColumnSettings.Normalize(columns, [new("c", false), new("a", false)]));
    }

    [Fact]
    public void UnknownKeysAndDuplicatesAreRemovedAndNewKeysAreHidden()
    {
        // Arrange
        GridColumn[] columns = [Column("a"), Column("b"), Column("c")];
        GridColumnState[] orders = [new("unknown", true), new("b", false), new("b", true), new("a", true)];

        // Act
        var result = GridColumnSettings.Normalize(columns, orders);

        // Assert
        Assert.Equal([new("b", false), new("a", true), new GridColumnState("c", false)], result);
        Assert.Equal(result, GridColumnSettings.Normalize(columns, result));
    }

    [Fact]
    public void UnknownVisibleKeyDoesNotLeaveAnEmptyGrid()
    {
        // Act & Assert
        Assert.Equal([new GridColumnState("a", true)], GridColumnSettings.Normalize([Column("a")], [new("unknown", true), new("a", false)]));
        Assert.Empty(GridColumnSettings.Normalize([], [new("unknown", true)]));
    }

    [Fact]
    public void InvalidCatalogKeysAreDiagnosedBeforeApplying()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => GridColumnSettings.Normalize([Column("a"), Column("a")], null));
        Assert.Throws<ArgumentException>(static () => GridColumnSettings.ValidateKeys(["a", "a"]));
        Assert.Throws<ArgumentException>(static () => GridColumnSettings.ValidateKeys([" "]));
    }

    [Fact]
    public void SavedWidthsAreKeptForHiddenColumnsAndInvalidWidthsAreDropped()
    {
        // Arrange
        GridColumn[] columns = [Column("a"), Column("b"), Column("c")];
        GridColumnState[] orders = [new("a", true) { Width = 120 }, new("b", false) { Width = 80 }, new("c", true) { Width = Double.NaN }];

        // Act
        var result = GridColumnSettings.Normalize(columns, orders);
        var fallback = GridColumnSettings.Normalize(columns, [new("b", false) { Width = 80 }, new("c", false) { Width = -1 }]);

        // Assert
        Assert.Equal([new("a", true) { Width = 120 }, new("b", false) { Width = 80 }, new GridColumnState("c", true)], result);
        Assert.Equal([new("a", true), new("b", true) { Width = 80 }, new GridColumnState("c", true)], fallback);
    }

    [Fact]
    public void ArrangeAppliesSavedWidthsAndKeepsTheDeclaredOnes()
    {
        // Arrange
        GridColumn[] catalog = [Column("a") with { Width = GridColumnWidth.Star() }, Column("b") with { MinWidth = 60 }, Column("c")];
        GridColumnState[] states = [new("b", true) { Width = 60 }, new("a", true) { Width = 150 }, new("c", false) { Width = 90 }];

        // Act
        var visible = GridColumnSettings.Arrange(catalog, states);
        var declared = GridColumnSettings.Arrange(catalog, [new GridColumnState("c", true)]);

        // Assert
        Assert.Equal(["b", "a"], visible.Select(static column => column.Key));
        Assert.Equal(GridColumnWidth.Absolute(60), visible[0].Width);
        Assert.Equal(GridColumnWidth.Absolute(150), visible[1].Width);
        Assert.Equal(GridColumnWidth.Star(), catalog[0].Width);
        Assert.Same(catalog[2], Assert.Single(declared));
    }

    [Fact]
    public void SavedWidthsAreClampedToTheMinimumWidth()
    {
        // Arrange
        GridColumn[] columns = [Column("a") with { MinWidth = 60 }, Column("b")];
        GridColumnState[] states = [new("a", true) { Width = 20 }, new("b", true) { Width = 100 }];

        // Act
        var result = GridColumnSettings.Normalize(columns, states);

        // Assert
        Assert.Equal([new GridColumnState("a", true) { Width = 60 }, new("b", true) { Width = 100 }], result);
        Assert.Equal(result, GridColumnSettings.Normalize(columns, result));
    }

    private static GridColumn Column(string key) => new(key, key.ToUpperInvariant(), new GridValueAccessor<object, string>(static _ => String.Empty));
}
