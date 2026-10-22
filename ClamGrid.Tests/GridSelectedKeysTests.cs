namespace ClamGrid.Tests;

public sealed class GridSelectedKeysTests
{
    [Fact]
    public void UnknownKeysAreDroppedAndTheRestIsSelected()
    {
        // Arrange
        var rows = Rows(4);
        using var view = new GridDataView<Row>(rows, static row => row.Id);

        // Act
        var applied = view.SetSelectedKeys([1, 99]);

        // Assert
        Assert.True(applied);
        Assert.Equal(1, view.SelectedCount);
        Assert.True(view.IsSelected(1));
        Assert.Equal([1], view.SelectedKeys);
    }

    [Fact]
    public void SelectedKeysFollowTheDisplayOrder()
    {
        // Arrange
        var rows = Rows(4);
        using var view = new GridDataView<Row>(rows, static row => row.Id);
        view.RegisterSort("name", static row => row.Name, StringComparer.Ordinal);
        view.SetSelectedKeys([3, 1]);

        // Assert
        Assert.Equal([1, 3], view.SelectedKeys);

        // Act
        view.SortBy("name", GridSortCycle.DescendingAscending);

        // Assert
        Assert.Equal([3, 1], view.SelectedKeys);
    }

    [Fact]
    public void SingleToggleKeepsTheFirstKeyInTheDisplayOrder()
    {
        // Arrange
        var rows = Rows(4);
        using var view = new GridDataView<Row>(rows, static row => row.Id);
        view.SelectionMode = GridSelectionMode.SingleToggle;

        // Act
        view.SetSelectedKeys([3, 1]);

        // Assert
        Assert.Equal(1, view.SelectedCount);
        Assert.True(view.IsSelected(1));
    }

    [Fact]
    public void NothingIsSelectedWhileSelectionIsDisabled()
    {
        // Arrange
        var rows = Rows(4);
        using var view = new GridDataView<Row>(rows, static row => row.Id);
        view.SelectionMode = GridSelectionMode.None;

        // Act
        view.SetSelectedKeys([1, 2]);

        // Assert
        Assert.Empty(view.SelectedKeys);

        // Act
        view.SetSource(Rows(4));

        // Assert
        Assert.Empty(view.SelectedKeys);
    }

    [Fact]
    public void TheSelectionSurvivesAReloadWithNewInstancesOfTheSameKeys()
    {
        // Arrange
        var rows = Rows(4);
        using var view = new GridDataView<Row>(rows, static row => row.Id);
        view.SetSelectedKeys([1, 3]);

        // Act
        view.SetSource(Rows(4));

        // Assert
        Assert.Equal([1, 3], view.SelectedKeys);
    }

    [Fact]
    public void KeysWithoutARowAreDroppedAndDoNotComeBack()
    {
        // Arrange
        var rows = Rows(2);
        using var view = new GridDataView<Row>(rows, static row => row.Id);

        // Act
        view.SetSelectedKeys([5]);

        // Assert
        Assert.Empty(view.SelectedKeys);

        // Act
        view.SetSource(Rows(6));

        // Assert
        Assert.Empty(view.SelectedKeys);
    }

    [Fact]
    public void AResetKeepsTheSelectionOfTheRowsThatRemain()
    {
        // Arrange
        var rows = Rows(4);
        using var view = new GridDataView<Row>(rows, static row => row.Id);
        view.SetSelected(1, true);
        view.SetSelected(3, true);

        // Act
        view.SetSource(Rows(3));

        // Assert
        Assert.Equal([1], view.SelectedKeys);
    }

    [Fact]
    public void AClearedSelectionStaysEmptyAfterAReload()
    {
        // Arrange
        var rows = Rows(4);
        using var view = new GridDataView<Row>(rows, static row => row.Id);
        view.SetSelectedKeys([1]);
        view.ClearSelection();

        // Act
        view.SetSource(Rows(4));

        // Assert
        Assert.Empty(view.SelectedKeys);
    }

    [Fact]
    public void TheKeysAreTheRowsWithoutAKeySelector()
    {
        // Arrange
        var rows = Rows(3);
        using var view = new GridDataView<Row>(rows);

        // Act
        view.SetSelectedKeys([rows[1]]);

        // Assert
        Assert.True(view.IsSelected(1));
        Assert.Same(rows[1], Assert.Single(view.SelectedKeys));
    }

    [Fact]
    public void ANullKeyIsRejected()
    {
        // Arrange
        var rows = Rows(2);
        using var view = new GridDataView<Row>(rows, static row => row.Id);

        // Act
        // Assert
        Assert.Throws<ArgumentNullException>(() => view.SetSelectedKeys([null!]));
    }

    [Fact]
    public void KeysAreNotAppliedWhileANotificationIsRunning()
    {
        // Arrange
        var rows = Rows(4);
        using var view = new GridDataView<Row>(rows, static row => row.Id);
        var applied = true;
        view.Changed += (sender, _) => applied = ((GridDataView<Row>)sender!).SetSelectedKeys([1]);

        // Act
        rows.Add(new Row(9, "row9"));

        // Assert
        Assert.False(applied);
        Assert.Empty(view.SelectedKeys);
    }

    private static ObservableCollection<Row> Rows(int count) => [with(Enumerable.Range(0, count).Select(static id => new Row(id, $"row{id}")))];

    private sealed record Row(int Id, string Name);
}
