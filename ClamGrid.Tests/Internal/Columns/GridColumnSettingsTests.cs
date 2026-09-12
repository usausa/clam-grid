namespace ClamGrid.Tests.Internal.Columns;

public sealed class GridColumnSettingsTests
{
    [Fact]
    public void NullEmptyAndAllHiddenRestoreDefaultOrder()
    {
        string[] keys = ["a", "b", "c"];
        GridColumnOrder[] expected = [new("a", true), new("b", true), new("c", true)];
        Assert.Equal(expected, GridColumnSettings.Normalize(keys, null));
        Assert.Equal(expected, GridColumnSettings.Normalize(keys, []));
        Assert.Equal(expected, GridColumnSettings.Normalize(keys, [new("c", false), new("a", false)]));
    }

    [Fact]
    public void UnknownKeysAndDuplicatesAreRemovedAndNewKeysAreHidden()
    {
        var result = GridColumnSettings.Normalize(["a", "b", "c"], [new("unknown", true), new("b", false), new("b", true), new("a", true)]);
        Assert.Equal([new("b", false), new("a", true), new GridColumnOrder("c", false)], result);
        Assert.Equal(result, GridColumnSettings.Normalize(["a", "b", "c"], result));
    }

    [Fact]
    public void UnknownVisibleKeyDoesNotLeaveAnEmptyGrid()
    {
        Assert.Equal([new GridColumnOrder("a", true)], GridColumnSettings.Normalize(["a"], [new("unknown", true), new("a", false)]));
        Assert.Empty(GridColumnSettings.Normalize([], [new("unknown", true)]));
    }

    [Fact]
    public void InvalidCatalogKeysAreDiagnosedBeforeApplying()
    {
        Assert.Throws<ArgumentException>(() => GridColumnSettings.Normalize(["a", "a"], null));
        Assert.Throws<ArgumentException>(() => GridColumnSettings.Normalize([" "], null));
    }
}
