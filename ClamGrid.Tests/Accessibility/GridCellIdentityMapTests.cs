namespace ClamGrid.Tests.Accessibility;

public sealed class GridCellIdentityMapTests
{
    [Fact]
    public void CellIdsFollowKeysAcrossRowAndColumnReordering()
    {
        // Arrange
        var map = new GridCellIdentityMap(EqualityComparer<object>.Default);

        // Act
        var first = map.GetId(42, "name");
        map.GetId(10, "number");

        // Assert
        Assert.Equal(first, map.GetId(42, "name"));
        Assert.Equal(new GridCellIdentity(42, "name"), map.Find(first));
        Assert.NotEqual(first, map.GetId(42, "number"));
    }

    [Fact]
    public void RemovedRowsAndColumnsDoNotReusePreviouslyExposedIds()
    {
        // Arrange
        var map = new GridCellIdentityMap(EqualityComparer<object>.Default);
        var removedRow = map.GetId(1, "name");
        var removedColumn = map.GetId(2, "old");
        var retained = map.GetId(2, "name");

        // Act
        map.Prune(key => (int)key == 2, new HashSet<string> { "name" });

        // Assert
        Assert.Null(map.Find(removedRow));
        Assert.Null(map.Find(removedColumn));
        Assert.Equal(retained, map.GetId(2, "name"));
        Assert.NotEqual(removedRow, map.GetId(1, "name"));
    }
}
