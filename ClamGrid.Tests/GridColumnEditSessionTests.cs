namespace ClamGrid.Tests;

public sealed class GridColumnEditSessionTests
{
    [Fact]
    public void EditingIsIsolatedAndExportUsesMovedSourceOrder()
    {
        // Arrange
        GridColumnOption[] original = [new("a", "A", true), new("b", "B", true), new("c", "C", true)];
        var edit = new GridColumnEditSession(original);
        var notifications = 0;
        edit.Columns[0].PropertyChanged += (_, _) => notifications++;

        // Act
        edit.Columns[0].IsVisible = false;
        edit.Columns[0].IsVisible = false;
        edit.Columns.Move(0, 2);
        var saved = edit.Export();

        // Assert
        Assert.Equal([new("b", true), new("c", true), new GridColumnOrder("a", false)], saved);
        Assert.True(original[0].IsVisible);
        Assert.Equal("a", original[0].Key);
        Assert.Equal(1, notifications);

        // Act
        edit.Columns[2].IsVisible = true;

        // Assert
        Assert.False(saved[2].IsVisible);
    }

    [Fact]
    public void SeparateEditSessionsDoNotShareVisibility()
    {
        // Arrange
        var first = new GridColumnEditSession([new GridColumnOption("a", "A", true)]);
        var second = new GridColumnEditSession(first.Columns);

        // Act
        second.Columns[0].IsVisible = false;

        // Assert
        Assert.True(first.Columns[0].IsVisible);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new GridColumnEditSession([new("a", "A", true), new("a", "A", false)]));
    }

    [Fact]
    public void CreateNormalizesSavedOrdersAgainstTheDefinitions()
    {
        // Arrange
        var accessor = new GridValueAccessor<object, string>(static _ => String.Empty);
        GridColumn[] columns = [new("a", "A", accessor), new("b", "B", accessor), new("c", "C", accessor)];
        GridColumnOrder[] orders = [new("c", true), new("unknown", true), new("a", false)];

        // Act
        var session = GridColumnEditSession.Create(columns, orders);
        var fromArgs = new GridColumnConfigurationEventArgs(columns, null, orders).CreateEditSession();

        // Assert
        Assert.Equal(["c", "a", "b"], session.Columns.Select(static column => column.Key));
        Assert.Equal([true, false, false], session.Columns.Select(static column => column.IsVisible));
        Assert.Equal("B", session.Columns[2].Header);
        Assert.Equal(session.Export(), fromArgs.Export());
        Assert.Equal([new("a", true), new("b", true), new GridColumnOrder("c", true)], GridColumnEditSession.Create(columns).Export());
    }
}
