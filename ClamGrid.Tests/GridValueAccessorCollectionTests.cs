namespace ClamGrid.Tests;

public sealed class GridValueAccessorCollectionTests
{
    [Fact]
    public void InitializerRegistersTypedAccessorsByKey()
    {
        // Arrange
        var item = new Item { Name = "abc" };

        // Act
        var accessors = new GridValueAccessorCollection<Item>
        {
            { "name", static x => x.Name },
            { "done", static x => x.IsDone, static (x, value) => x.IsDone = value }
        };
        accessors.GetValueAccessor("done")!.SetValue(item, true);

        // Assert
        Assert.Equal(2, accessors.Count);
        Assert.Equal("abc", accessors.GetValueAccessor("name")!.GetValue(item));
        Assert.False(accessors.GetValueAccessor("name")!.CanWrite);
        Assert.True(item.IsDone);
        Assert.Null(accessors.GetValueAccessor("missing"));
        Assert.Equal(["name", "done"], accessors.Select(static pair => pair.Key));
    }

    [Fact]
    public void DuplicateOrEmptyKeysAreRejected()
    {
        // Arrange
        var accessors = new GridValueAccessorCollection<Item> { { "name", static x => x.Name } };

        // Act & Assert
        Assert.Throws<ArgumentException>(() => accessors.Add("name", static x => x.Name));
        Assert.Throws<ArgumentException>(() => accessors.Add(" ", static x => x.Name));
    }

    [Fact]
    public void XamlColumnWithoutAccessorReadsAsEmptyAndRejectsWrites()
    {
        // Arrange
        var column = new GridColumn { Key = "name", Header = "Name", Width = GridColumnWidth.Star() };

        // Act & Assert
        Assert.Null(column.ValueAccessor.GetValue(new Item()));
        Assert.False(column.ValueAccessor.CanWrite);
        Assert.Throws<InvalidOperationException>(() => column.ValueAccessor.SetValue(new Item(), "x"));
        Assert.Equal(GridColumnWidth.Star(), column.Width);
        Assert.True(column.AllowSorting);
    }

    private sealed class Item
    {
        public string Name { get; set; } = String.Empty;

        public bool IsDone { get; set; }
    }
}
