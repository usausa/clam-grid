namespace ClamGrid.Tests;

public sealed class GridDataViewFilterTests
{
    [Fact]
    public void FilterHidesRowsAndKeepsTheSourceCount()
    {
        // Arrange
        var rows = Rows(6);
        using var view = new GridDataView<Row>(rows, static row => row.Id);
        view.RegisterSort("id", static row => row.Id);
        view.RestoreSortOrders([new GridSortOrder("id", true)]);
        var kinds = new List<GridDataChangeKind>();
        var properties = new List<string?>();
        view.Changed += (_, e) => kinds.Add(e.Kind);
        view.PropertyChanged += (_, e) => properties.Add(e.PropertyName);

        // Act
        view.Filter = static row => (row.Id % 2) == 0;

        // Assert
        Assert.Equal([4, 2, 0], Ids(view));
        Assert.Equal(6, view.SourceCount);
        Assert.Equal(-1, view.IndexOfKey(1));
        Assert.Equal(2, view.IndexOfKey(0));
        Assert.Equal([GridDataChangeKind.Filter], kinds);
        Assert.Contains(nameof(view.Count), properties);
        Assert.Contains(nameof(view.Filter), properties);

        // Act
        view.Filter = null;

        // Assert
        Assert.Equal([5, 4, 3, 2, 1, 0], Ids(view));
        Assert.Equal(6, view.SourceCount);
    }

    [Fact]
    public void HiddenRowsLoseTheirSelectionAndBulkSelectionSkipsThem()
    {
        // Arrange
        using var view = new GridDataView<Row>(Rows(4), static row => row.Id);
        view.SelectAll();

        // Act
        view.Filter = static row => row.Id != 2;

        // Assert
        Assert.Equal([0, 1, 3], view.SelectedItems.Cast<Row>().Select(static row => row.Id));

        // Act
        view.Filter = null;

        // Assert
        Assert.Equal(3, view.SelectedCount);
        Assert.False(view.IsSelected(view.IndexOfKey(2)));

        // Act
        view.Filter = static row => row.Id < 2;
        view.ClearSelection();
        view.SelectAll();
        view.Filter = null;

        // Assert
        Assert.Equal([0, 1], view.SelectedItems.Cast<Row>().Select(static row => row.Id));
    }

    [Fact]
    public void RowChangesRunTheFilterAgainAndHiddenRowsStayObserved()
    {
        // Arrange
        var rows = Rows(3);
        var view = new GridDataView<Row>(rows, static row => row.Id) { Filter = static row => !row.Name.StartsWith('x') };

        // Act
        rows[1].Name = "x1";

        // Assert
        Assert.Equal([0, 2], Ids(view));
        Assert.Equal(1, rows[1].SubscriberCount);

        // Act
        rows[1].Name = "row1";

        // Assert
        Assert.Equal([0, 1, 2], Ids(view));

        // Act
        rows[1].Name = "x1";
        view.Dispose();

        // Assert
        Assert.All(rows, static row => Assert.Equal(0, row.SubscriberCount));
    }

    [Fact]
    public void SourceChangesAreFilteredAndReplacementKeepsTheFilter()
    {
        // Arrange
        var rows = Rows(3);
        using var view = new GridDataView<Row>(rows, static row => row.Id);
        view.Filter = static row => row.Id != 10;

        // Act
        rows.Add(new Row(10, "hidden"));
        rows.Add(new Row(11, "shown"));

        // Assert
        Assert.Equal([0, 1, 2, 11], Ids(view));
        Assert.Equal(5, view.SourceCount);

        // Act
        view.SetSource(Rows(12));

        // Assert
        Assert.Equal(11, view.Count);
        Assert.Equal(12, view.SourceCount);
    }

    [Fact]
    public void SortsKeepTheOrderOfHiddenRows()
    {
        // Arrange
        ObservableCollection<Row> rows = [new(0, "b"), new(1, "a"), new(2, "b"), new(3, "a")];
        using var view = new GridDataView<Row>(rows, static row => row.Id);
        view.RegisterSort("id", static row => row.Id);
        view.RegisterSort("name", static row => row.Name, StringComparer.Ordinal);
        view.RestoreSortOrders([new GridSortOrder("id", true)]);
        view.RestoreSortOrders([new GridSortOrder("name")]);

        // Act
        view.Filter = static row => row.Id != 2;

        // Assert
        Assert.Equal([3, 1, 0], Ids(view));

        // Act
        view.Filter = null;

        // Assert
        Assert.Equal([3, 1, 2, 0], Ids(view));

        // Act
        view.Filter = static row => row.Id != 0;
        view.RestoreSortOrders([]);

        // Assert
        Assert.Equal([1, 2, 3], Ids(view));
    }

    [Fact]
    public void FailingFilterKeepsThePreviousFilterAndRows()
    {
        // Arrange
        using var view = new GridDataView<Row>(Rows(3), static row => row.Id);
        Func<Row, bool> odd = static row => (row.Id % 2) == 1;
        view.Filter = odd;

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() =>
        {
            view.Filter = static _ => throw new InvalidOperationException();
        });
        Assert.Same(odd, view.Filter);
        Assert.Equal([1], Ids(view));
    }

    [Fact]
    public void HiddenRowsAreStillValidated()
    {
        // Arrange
        using var view = new GridDataView<Row>(Rows(2), static row => row.Id);
        view.Filter = static row => row.Id == 0;

        // Act & Assert
        Assert.Throws<ArgumentException>(() => view.SetSource(new[] { new Row(0, "a"), new Row(1, "b"), new Row(1, "c") }));
        Assert.Equal([0], Ids(view));
        Assert.Equal(2, view.SourceCount);
    }

    [Fact]
    public void RowMovesAreDisabledWhileRowsAreHidden()
    {
        // Arrange
        var rows = Rows(3);
        using var view = new GridDataView<Row>(rows, static row => row.Id);
        var mover = new GridRowMover<Row>(rows, view);

        // Act
        view.Filter = static row => row.Id != 1;

        // Assert
        Assert.False(mover.CanMove);

        // Act
        view.Filter = static _ => true;

        // Assert
        Assert.True(mover.CanMove);
    }

    private static int[] Ids(GridDataView<Row> view) => view.Select(static row => row.Id).ToArray();

    private static ObservableCollection<Row> Rows(int count) => [with(Enumerable.Range(0, count).Select(static id => new Row(id, $"row{id}")))];

    private sealed class Row : INotifyPropertyChanged
    {
        private PropertyChangedEventHandler? propertyChanged;

        public event PropertyChangedEventHandler? PropertyChanged
        {
            add
            {
                propertyChanged += value;
                SubscriberCount++;
            }
            remove
            {
                propertyChanged -= value;
                SubscriberCount--;
            }
        }

        public int SubscriberCount { get; private set; }

        public int Id { get; }

        public string Name
        {
            get;
            set
            {
                field = value;
                propertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Name)));
            }
        }

        public Row(int id, string name)
        {
            Id = id;
            Name = name;
        }
    }
}
