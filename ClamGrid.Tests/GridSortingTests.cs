namespace ClamGrid.Tests;

public sealed class GridSortingTests
{
    [Fact]
    public void HeaderRequestsTogglePromoteAndKeepSecondaryHistory()
    {
        // Arrange
        using var view = CreateView();

        // Act & Assert
        Assert.Equal(GridSortStatus.Applied, view.SortBy("number").Status);
        Assert.False(view.SortOrders[0].Descending);

        // Act
        view.SortBy("number");

        // Assert
        Assert.True(view.SortOrders[0].Descending);

        // Act
        view.SortBy("name");

        // Assert
        Assert.Equal(new[] { new GridSortOrder("name"), new GridSortOrder("number", true) }, view.SortOrders);

        // Act
        view.SortBy("number");

        // Assert
        Assert.Equal(new[] { new GridSortOrder("number"), new GridSortOrder("name") }, view.SortOrders);
    }

    [Fact]
    public void ClearingCycleRemovesTheKeyOnTheThirdRequest()
    {
        // Arrange
        using var view = CreateView();
        view.SortBy("name");

        // Act
        view.SortBy("number", GridSortCycle.AscendingDescendingNone);

        // Assert
        Assert.Equal(new[] { new GridSortOrder("number"), new GridSortOrder("name") }, view.SortOrders);

        // Act
        view.SortBy("number", GridSortCycle.AscendingDescendingNone);

        // Assert
        Assert.Equal(new[] { new GridSortOrder("number", true), new GridSortOrder("name") }, view.SortOrders);

        // Act
        view.SortBy("number", GridSortCycle.AscendingDescendingNone);

        // Assert
        Assert.Equal(new[] { new GridSortOrder("name") }, view.SortOrders);
    }

    [Fact]
    public void DescendingFirstCycleStartsDescendingAndToggles()
    {
        // Arrange
        using var view = CreateView();

        // Act
        view.SortBy("number", GridSortCycle.DescendingAscending);

        // Assert
        Assert.Equal(new GridSortOrder("number", true), view.SortOrders[0]);

        // Act
        view.SortBy("number", GridSortCycle.DescendingAscending);

        // Assert
        Assert.Equal(new GridSortOrder("number"), view.SortOrders[0]);

        // Act
        view.SortBy("number", GridSortCycle.DescendingAscending);

        // Assert
        Assert.Equal(new GridSortOrder("number", true), view.SortOrders[0]);
    }

    [Fact]
    public void SecondaryKeysAndTiesAreStableInBothDirections()
    {
        // Arrange
        Row[] rows = [new(0, 2, "x"), new(1, 1, "x"), new(2, 1, "x"), new(3, 1, "a")];
        using var view = new GridDataView<Row>(rows);
        view.RegisterSort("number", static row => row.Number);
        view.RegisterSort("name", static row => row.Name, StringComparer.Ordinal);

        // Act
        view.RestoreSortOrders([new GridSortOrder("number"), new GridSortOrder("name")]);

        // Assert
        Assert.Equal<int>([3, 1, 2, 0], view.Select(static row => row.Id));

        // Act
        view.SortBy("number");

        // Assert
        Assert.Equal<int>([0, 3, 1, 2], view.Select(static row => row.Id));
    }

    [Fact]
    public void HiddenKeysNullsAndUnknownSavedKeysAreHandledExplicitly()
    {
        // Arrange
        using var view = CreateView();

        // Act
        var result = view.RestoreSortOrders([new("removed"), new("name"), new("name", true), new("number")]);

        // Assert
        Assert.Equal(GridSortStatus.Applied, result.Status);
        Assert.Equal("removed", Assert.Single(result.IgnoredKeys));
        Assert.Null(view[0].Name);
        Assert.Equal(2, view.SortOrders.Count);

        // Arrange
        var before = view.ToArray();

        // Act & Assert
        Assert.Equal(GridSortStatus.Rejected, view.SortBy("unregistered").Status);
        Assert.Equal(before, view);

        // Act
        var saved = view.SaveSortOrders();
        saved[0] = new GridSortOrder("unregistered");

        // Assert
        Assert.Equal("name", view.SortOrders[0].Key);
    }

    [Fact]
    public void DirectionAwareComparerCanKeepAGroupFixedWhenDescending()
    {
        // Arrange
        Row[] rows = [new(0, 3, "priority"), new(1, 2, "normal"), new(2, 1, "priority")];
        using var view = new GridDataView<Row>(rows);
        view.RegisterComparer("custom", static (a, b, descending) =>
        {
            var group = (a.Name != "priority").CompareTo(b.Name != "priority");
            return group != 0 ? group : descending ? b.Number.CompareTo(a.Number) : a.Number.CompareTo(b.Number);
        });

        // Act
        view.RestoreSortOrders([new("custom", true)]);

        // Assert
        Assert.Equal<int>([0, 2, 1], view.Select(static row => row.Id));
    }

    [Fact]
    public void CancelAndComparerFailureLeaveOrderSelectionAndHistoryUntouched()
    {
        // Arrange
        using var view = CreateView();
        view.SetSelected(1, true);
        var before = view.ToArray();
        static void Cancel(object? sender, GridSortRequestedEventArgs e) => e.Cancel = true;
        view.SortRequested += Cancel;

        // Act & Assert
        Assert.Equal(GridSortStatus.Canceled, view.SortBy("number").Status);

        // Arrange
        view.SortRequested -= Cancel;
        view.RegisterComparer("bad", static (_, _, _) => throw new InvalidOperationException("comparison failed"));
        var failures = 0;
        view.SortFailed += (_, _) => failures++;

        // Act & Assert
        Assert.Equal(GridSortStatus.Failed, view.SortBy("bad").Status);
        Assert.Equal(before, view);
        Assert.True(view.IsSelected(1));
        Assert.Empty(view.SortOrders);
        Assert.Equal(1, failures);
    }

    [Fact]
    public void SourceChangeDuringComparisonRejectsStaleResultsAndAppliesNewData()
    {
        // Arrange
        var rows = new ObservableCollection<Row> { new(0, 3, "c"), new(1, 2, "b"), new(2, 1, "a") };
        using var view = new GridDataView<Row>(rows);
        var changed = false;
        view.RegisterComparer("number", (a, b, _) =>
        {
            if (!changed)
            {
                changed = true;
                rows.Add(new Row(3, 0, "new"));
            }

            return a.Number.CompareTo(b.Number);
        });

        // Act
        var result = view.SortBy("number");

        // Assert
        Assert.Equal(GridSortStatus.Superseded, result.Status);
        Assert.Equal(rows, view);
        Assert.Empty(view.SortOrders);
    }

    [Fact]
    public void DuplicateRequestsAndExplicitCancellationDoNotCommitOldWork()
    {
        // Arrange
        using var view = CreateView();
        view.SortRequested += (_, _) =>
        {
            Assert.Equal(GridSortStatus.Rejected, view.SortBy("name").Status);
            view.CancelPendingSort();
        };

        // Act
        var result = view.SortBy("number");

        // Assert
        Assert.Equal(GridSortStatus.Superseded, result.Status);
        Assert.Empty(view.SortOrders);
    }

    [Fact]
    public void EveryNotificationObservesTheFinalSortedOrderAndSelection()
    {
        // Arrange
        using var view = CreateView();
        var selected = view[0];
        view.SetSelected(0, true);
        var notifications = 0;
        view.CollectionChanged += (_, _) => Check();
        view.SortChanged += (_, _) => Check();
        view.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(view.SortOrders))
            {
                Check();
            }
        };

        // Act
        view.SortBy("number");

        // Assert
        Assert.Equal(3, notifications);

        void Check()
        {
            notifications++;
            Assert.Equal("number", view.SortOrders[0].Key);
            Assert.Equal<int>([1, 2, 3], view.Select(static row => row.Number));
            Assert.Same(selected, Assert.Single(view.SelectedItems));
            Assert.True(view.IsSelected(2));
        }
    }

    private static GridDataView<Row> CreateView()
    {
        var view = new GridDataView<Row>(new[] { new Row(0, 3, "b"), new Row(1, 1, null), new Row(2, 2, "a") });
        view.RegisterSort("number", static row => row.Number);
        view.RegisterSort("name", static row => row.Name, StringComparer.Ordinal);
        return view;
    }

    private sealed record Row(int Id, int Number, string? Name);
}
