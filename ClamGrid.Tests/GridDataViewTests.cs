namespace ClamGrid.Tests;

public sealed class GridDataViewTests
{
    [Fact]
    public void NonAdjacentMoveRemovalAndReplacementPreserveOnlyTheCorrectRows()
    {
        // Arrange
        var rows = Rows(5);
        using var view = new GridDataView<Row>(rows, static row => row.Id);
        view.SetSelected(1, true);
        view.SetSelected(3, true);
        var notifications = 0;
        view.CollectionChanged += (_, _) =>
        {
            notifications++;
            Assert.Equal(rows, view);
            Assert.Equal(view.SelectedCount, Enumerable.Range(0, view.Count).Count(view.IsSelected));
            Assert.Equal(view.SelectedCount, view.SelectedItems.Count);
        };

        // Act
        rows.Move(1, 4);

        // Assert
        Assert.True(view.IsSelected(4));
        Assert.True(view.IsSelected(2));
        Assert.False(view.IsSelected(1));

        // Act
        rows.RemoveAt(4);

        // Assert
        Assert.Equal(1, view.SelectedCount);

        // Act
        rows[2] = new Row(3, "replacement");

        // Assert
        Assert.Equal(0, view.SelectedCount);

        // Act
        rows.Insert(0, new Row(9, "added"));

        // Assert
        Assert.Equal(4, notifications);
    }

    [Fact]
    public void ResetWithTheSameInstancesClearsSelectionButRefreshPreservesIt()
    {
        // Arrange
        var rows = Rows(4);
        using var view = new GridDataView<Row>(rows);
        view.SetSelected(2, true);

        // Act
        view.Refresh();

        // Assert
        Assert.Same(rows[2], Assert.Single(view.SelectedItems));

        // Act
        view.SetSource(rows);

        // Assert
        Assert.Empty(view.SelectedItems);

        // Act
        view.SelectAll();
        rows.Clear();

        // Assert
        Assert.Empty(view);
        Assert.Equal(0, view.SelectedCount);
    }

    [Fact]
    public void SingleAndMultipleSelectionAreTogglesAndBulkSelectionNotifiesOnce()
    {
        // Arrange
        using var view = new GridDataView<Row>(Rows(5));
        var changes = 0;
        view.SelectionChanged += (_, _) => changes++;

        // Act
        view.UpdateSelection(static row => (row.Id % 2) == 0);

        // Assert
        Assert.Equal(3, view.SelectedCount);
        Assert.Equal(1, changes);

        // Act
        view.SelectAll();

        // Assert
        Assert.Equal(5, view.SelectedCount);

        // Act
        view.SelectionMode = GridSelectionMode.SingleToggle;

        // Assert
        Assert.Equal(1, view.SelectedCount);
        Assert.True(view.IsSelected(0));

        // Act & Assert
        Assert.True(view.TryToggleSelection(4, out var selected));
        Assert.True(selected);
        Assert.False(view.IsSelected(0));
        Assert.True(view.TryToggleSelection(4, out selected));
        Assert.False(selected);
        Assert.False(view.TryToggleSelection(-1, out _));
        Assert.False(view.TryToggleSelection(5, out _));

        // Arrange
        view.SelectionMode = GridSelectionMode.None;

        // Act & Assert
        Assert.False(view.TryToggleSelection(1, out _));

        // Act
        view.SelectAll();

        // Assert
        Assert.Empty(view.SelectedItems);
    }

    [Fact]
    public void ExternalViewSelectionAndItemsUseTheSameSortedIndices()
    {
        // Arrange
        var rows = Rows(5);
        using var view = new GridDataView<Row>(rows, static row => row.Id);
        view.RegisterSort("id", static row => row.Id);
        IGridDataView external = view;
        external.SetSelected(external.IndexOfKey(1), true);

        // Act
        view.RestoreSortOrders([new GridSortOrder("id", true)]);

        // Assert
        Assert.Same(rows[1], view[3]);
        Assert.Same(rows[1], external.Items[3]);
        Assert.True(external.IsSelected(3));
        Assert.Same(rows[1], Assert.Single(external.SelectedItems));
        Assert.Equal(3, external.IndexOfKey(1));
        Assert.Equal(rows, rows.OrderBy(static row => row.Id));
    }

    [Fact]
    public void PropertyChangesRefreshAndResortWithoutTransferringSelection()
    {
        // Arrange
        var rows = Rows(3);
        using var view = new GridDataView<Row>(rows, static row => row.Id);
        view.RegisterSort("name", static row => row.Name, StringComparer.Ordinal);
        view.SortBy("name");
        view.SetSelected(0, true);
        var changes = 0;
        view.Changed += (_, e) =>
        {
            if (e.Kind == GridDataChangeKind.Item)
            {
                changes++;
                Assert.Same(rows[0], view[^1]);
                Assert.True(view.IsSelected(2));
            }
        };

        // Act
        rows[0].Name = "zz";

        // Assert
        Assert.Equal(1, changes);

        // Act
        rows.Add(new Row(10, "aa"));

        // Assert
        Assert.Same(rows[0], view[^1]);
        Assert.True(view.IsSelected(3));
    }

    [Fact]
    public void SourcesAndRemovedItemsAreUnsubscribedOnReplacementSuspendAndDispose()
    {
        // Arrange
        var rows = Rows(3);
        var removed = rows[1];
        var replacement = Rows(2);
        using var view = new GridDataView<Row>(rows);

        // Assert
        Assert.Equal(1, removed.SubscriberCount);

        // Act
        rows.RemoveAt(1);

        // Assert
        Assert.Equal(0, removed.SubscriberCount);

        // Act
        view.SetSource(replacement);

        // Assert
        Assert.All(rows, static row => Assert.Equal(0, row.SubscriberCount));

        // Act
        rows.Add(new Row(40, "old source"));

        // Assert
        Assert.Equal(2, view.Count);

        // Act
        view.SetSelected(0, true);
        view.Suspend();

        // Assert
        Assert.All(replacement, static row => Assert.Equal(0, row.SubscriberCount));

        // Act
        replacement.Add(new Row(4, "missed"));

        // Assert
        Assert.Equal(2, view.Count);

        // Act
        view.Resume();

        // Assert
        Assert.Equal(3, view.Count);
        Assert.Empty(view.SelectedItems);

        // Act
        view.Dispose();

        // Assert
        Assert.All(replacement, static row => Assert.Equal(0, row.SubscriberCount));

        // Act
        replacement.Clear();

        // Assert
        Assert.Equal(3, view.Count);
    }

    [Fact]
    public void ReferenceIdentityDoesNotUseOverriddenValueEquality()
    {
        // Arrange
        var first = new EqualRow(1);
        var second = new EqualRow(1);
        using var view = new GridDataView<EqualRow>(new[] { first, second });

        // Act
        view.SetSelected(0, true);

        // Assert
        Assert.True(view.IsSelected(0));
        Assert.False(view.IsSelected(1));
        Assert.Equal(1, view.IndexOfKey(second));
    }

    [Fact]
    public void InvalidSourcesAreRejectedWithoutChangingTheCommittedView()
    {
        // Arrange
        var rows = Rows(2);
        using var view = new GridDataView<Row>(rows, static row => row.Id);
        view.SetSelected(0, true);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => view.SetSource(new[] { new Row(1, "a"), new Row(1, "b") }));
        Assert.Throws<ArgumentException>(() => view.SetSource(new object?[] { null }));
        Assert.Throws<ArgumentException>(() => view.SetSource((string[])["wrong type"]));
        Assert.Equal(rows, view);
        Assert.True(view.IsSelected(0));
    }

    [Fact]
    public void NotificationsSeeCommittedStateAndSourceReentrancyIsDeferred()
    {
        // Arrange
        var rows = Rows(3);
        using var view = new GridDataView<Row>(rows);
        view.Changed += (_, e) =>
        {
            if (e.Kind == GridDataChangeKind.Selection)
            {
                Assert.Equal(1, view.SelectedCount);
                rows.RemoveAt(0);
                Assert.Equal(3, view.Count);
            }
        };
        view.SelectionChanged += (_, _) => Assert.Equal(view.SelectedCount, view.SelectedItems.Count);

        // Act
        view.SetSelected(0, true);

        // Assert
        Assert.Equal(2, view.Count);
        Assert.Empty(view.SelectedItems);
    }

    [Fact]
    public void MutatingSelectionPredicateCannotOverwriteTheUpdatedSource()
    {
        // Arrange
        var rows = Rows(2);
        using var view = new GridDataView<Row>(rows);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => view.UpdateSelection(row =>
        {
            if (row.Id == 0)
            {
                rows.Clear();
            }

            return true;
        }));
        Assert.Empty(view);
        Assert.Empty(view.SelectedItems);
    }

    [Fact]
    public void SourceReplacementEnumeratesSinglePassInputOnlyOnce()
    {
        // Arrange
        using var view = new GridDataView<Row>(Array.Empty<Row>());
        var queue = new Queue<Row>(Rows(3));

        // Act
        view.SetSource(Drain(queue));

        // Assert
        Assert.Equal(3, view.Count);
        Assert.Empty(queue);

        static IEnumerable<Row> Drain(Queue<Row> input)
        {
            while (input.TryDequeue(out var row))
            {
                yield return row;
            }
        }
    }

    private static ObservableCollection<Row> Rows(int count) => [with(Enumerable.Range(0, count).Select(static id => new Row(id, $"row{id}")))];

    private sealed record EqualRow(int Value);

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
