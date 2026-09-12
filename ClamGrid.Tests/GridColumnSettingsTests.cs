namespace ClamGrid.Tests;

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
}
