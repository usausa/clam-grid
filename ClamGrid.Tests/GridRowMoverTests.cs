namespace ClamGrid.Tests;

public sealed class GridRowMoverTests
{
    [Fact]
    public void RowMoverChangesCollectionOnceAndPreservesSelectionIdentity()
    {
        // Arrange
        ObservableCollection<GridColumnOption> source = [new("a", "A", true), new("b", "B", true), new("c", "C", true)];
        using var view = new GridDataView<GridColumnOption>(source, static item => item.Key);
        var mover = new GridRowMover<GridColumnOption>(source, view);
        var changes = new List<NotifyCollectionChangedAction>();
        source.CollectionChanged += (_, e) => changes.Add(e.Action);
        view.SetSelected(0, true);

        // Act
        var moved = mover.Move("a", 2);

        // Assert
        Assert.True(moved);
        Assert.Equal(["b", "c", "a"], source.Select(static item => item.Key));
        Assert.Equal([NotifyCollectionChangedAction.Move], changes);
        Assert.True(view.IsSelected(2));
        Assert.Equal(1, view.SelectedCount);

        // Act & Assert
        Assert.False(mover.Move("missing", 0));
        Assert.False(mover.Move("a", 3));
        Assert.False(mover.Move("a", 2));
    }

    [Fact]
    public void RowMoverRejectsSortedOrUnrelatedSource()
    {
        // Arrange
        ObservableCollection<GridColumnOption> source = [new("a", "A", true), new("b", "B", true)];
        using var view = new GridDataView<GridColumnOption>(source, static item => item.Key);
        var mover = new GridRowMover<GridColumnOption>(source, view);
        view.RegisterSort("key", static item => item.Key);

        // Act
        view.SortBy("key");

        // Assert
        Assert.False(mover.CanMove);
        Assert.False(mover.Move("a", 1));

        // Act
        view.RestoreSortOrders([]);

        // Assert
        Assert.True(mover.CanMove);

        // Act
        view.SetSource(new GridColumnOption[] { new("a", "A", true), new("b", "B", true) });

        // Assert
        Assert.False(mover.CanMove);
    }
}
