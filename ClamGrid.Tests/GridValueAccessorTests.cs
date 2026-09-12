namespace ClamGrid.Tests;

public sealed class GridValueAccessorTests
{
    [Fact]
    public void ReadOnlyColumnRejectsEditsWithoutChangingTheItem()
    {
        // Arrange
        var item = new Item { IsVisible = true };
        IGridValueAccessor accessor = new GridValueAccessor<Item, bool>(static value => value.IsVisible);

        // Act & Assert
        Assert.False(accessor.CanWrite);
        Assert.Throws<InvalidOperationException>(() => accessor.SetValue(item, false));
        Assert.True(item.IsVisible);
    }

    [Fact]
    public void BooleanColumnWritesBackToTheOriginalItem()
    {
        // Arrange
        var item = new Item();
        IGridValueAccessor accessor = new GridValueAccessor<Item, bool>(static value => value.IsVisible, static (value, visible) => value.IsVisible = visible);

        // Act
        accessor.SetValue(item, true);

        // Assert
        Assert.True(item.IsVisible);
        Assert.Equal(true, accessor.GetValue(item));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("true")]
    [InlineData(1)]
    public void InvalidBooleanValueDoesNotChangeTheItem(object? value)
    {
        // Arrange
        var item = new Item();
        IGridValueAccessor accessor = new GridValueAccessor<Item, bool>(static target => target.IsVisible, static (target, visible) => target.IsVisible = visible);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => accessor.SetValue(item, value));
        Assert.False(item.IsVisible);
    }

    [Fact]
    public void NullableColumnAcceptsNullWithoutConversion()
    {
        // Arrange
        var item = new Item { Text = "000123" };
        IGridValueAccessor accessor = new GridValueAccessor<Item, string?>(static value => value.Text, static (value, text) => value.Text = text);

        // Act & Assert
        Assert.Equal("000123", accessor.GetValue(item));

        // Act
        accessor.SetValue(item, null);

        // Assert
        Assert.Null(item.Text);
        Assert.Null(accessor.GetValue(item));
    }

    [Fact]
    public void WrongItemTypeIsRejectedBeforeCallingTheSetter()
    {
        // Arrange
        var called = false;
        IGridValueAccessor accessor = new GridValueAccessor<Item, bool>(static value => value.IsVisible, (_, _) => called = true);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => accessor.GetValue(new object()));
        Assert.Throws<ArgumentException>(() => accessor.SetValue(new object(), true));
        Assert.False(called);
    }

    private sealed class Item
    {
        public bool IsVisible { get; set; }

        public string? Text { get; set; }
    }
}
