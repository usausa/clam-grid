namespace ClamGrid.Tests;

public sealed class GridSortCallbackTests
{
    [Fact]
    public void CallbackOwnsStableMultiKeySortingAndSharesCommittedSelectionAndNotifications()
    {
        // Arrange
        Row[] rows = [new(0, 2, "x"), new(1, 1, "x"), new(2, 1, "x"), new(3, 1, "a")];
        using var view = new GridDataView<Row>(rows, static row => row.Id);
        view.RegisterComparer("number", static (_, _, _) => throw new InvalidOperationException("Default comparer must not run."));
        view.SetSortCallback(["number", "name"], SortRows);
        view.SetSelected(2, true);

        // Act & Assert
        Assert.True(view.CanSort("name"));
        Assert.Equal(GridSortStatus.Rejected, view.SortBy("missing").Status);

        // Act
        var result = view.RestoreSortOrders([new("number"), new("name"), new("name", true), new("missing")]);

        // Assert
        Assert.Equal("missing", Assert.Single(result.IgnoredKeys));
        Assert.Equal<int>([3, 1, 2, 0], view.Select(static row => row.Id));

        // Arrange
        var notifications = 0;
        view.CollectionChanged += (_, _) => Verify();
        view.SortChanged += (_, _) => Verify();

        // Act
        view.SortBy("number");

        // Assert
        Assert.Equal(2, notifications);
        Assert.Equal<int>([0, 1, 2, 3], rows.Select(static row => row.Id));

        // Act
        view.SortBy("name");

        // Assert
        Assert.Equal(new[] { new GridSortOrder("name"), new GridSortOrder("number", true) }, view.SortOrders);
        Assert.Equal<int>([3, 0, 1, 2], view.Select(static row => row.Id));

        void Verify()
        {
            if (view.SortOrders[0].Key != "number")
            {
                return;
            }

            notifications++;
            Assert.True(view.SortOrders[0].Descending);
            Assert.Equal<int>([0, 3, 1, 2], view.Select(static row => row.Id));
            Assert.Same(rows[2], Assert.Single(view.SelectedItems));
            Assert.Same(rows[2], view.Items[view.IndexOfKey(2)]);
            Assert.True(view.IsSelected(view.IndexOfKey(2)));
        }
    }

    [Fact]
    public void InputAndOrdersAreReadOnlyAndReturnedArrayIsNotRetained()
    {
        // Arrange
        Row[] rows = [new(0, 2, "b"), new(1, 1, "a")];
        using var view = new GridDataView<Row>(rows);
        Row[]? returned = null;
        IReadOnlyList<Row>? captured = null;
        view.SetSortCallback(["number"], (input, orders) =>
        {
            captured = input;
            Assert.Throws<NotSupportedException>(() => ((IList<Row>)input)[0] = rows[1]);
            Assert.Throws<NotSupportedException>(() => ((IList<GridSortOrder>)orders)[0] = new GridSortOrder("bad"));
            returned = input.Reverse().ToArray();
            return returned;
        });

        // Act & Assert
        Assert.Equal(GridSortStatus.Applied, view.SortBy("number").Status);

        // Act
        returned![0] = rows[0];

        // Assert
        Assert.Same(rows[1], view[0]);
        Assert.Same(rows[0], captured![0]);

        // Act
        view.RestoreSortOrders([]);

        // Assert
        Assert.Same(rows[1], captured[1]);
    }

    [Theory]
    [InlineData("missing")]
    [InlineData("duplicate")]
    [InlineData("extra")]
    [InlineData("replacement")]
    [InlineData("null-row")]
    [InlineData("null-result")]
    [InlineData("exception")]
    [InlineData("enumeration-exception")]
    public void InvalidOutputOrExceptionPreservesTheLastCommit(string failure)
    {
        // Arrange
        Row[] rows = [new(0, 2, "b"), new(1, 1, "a")];
        using var view = new GridDataView<Row>(rows, static row => row.Id);
        view.RegisterSort("number", static row => row.Number);
        view.SortBy("number");
        view.SetSelected(0, true);
        var before = view.ToArray();
        var history = view.SaveSortOrders();
        var failed = 0;
        var committed = 0;
        view.SortFailed += (_, _) => failed++;
        view.SortChanged += (_, _) => committed++;
        view.SetSortCallback(["number"], (input, _) => failure switch
        {
            "missing" => input.Take(1),
            "duplicate" => [input[0], input[0]],
            "extra" => input.Append(input[0]),
            "replacement" => [input[0] with { }, input[1]],
            "null-row" => [input[0], null!],
            "null-result" => null!,
            "exception" => throw new NotSupportedException("user code failed"),
            _ => BrokenSequence(input[0])
        });

        // Act
        var result = view.SortBy("number");

        // Assert
        Assert.Equal(GridSortStatus.Failed, result.Status);
        Assert.IsType<InvalidOperationException>(result.Error);
        Assert.Equal(1, failed);
        Assert.Equal(0, committed);
        Assert.Equal(before, view);
        Assert.Equal(history, view.SortOrders);
        Assert.Same(before[0], Assert.Single(view.SelectedItems));

        static IEnumerable<Row> BrokenSequence(Row row)
        {
            yield return row;
            throw new FormatException("deferred failure");
        }
    }

    [Fact]
    public void InvalidEnumerationIsBoundedAndDisposed()
    {
        // Arrange
        using var view = new GridDataView<Row>(new[] { new Row(0, 1, "a") });
        var reads = 0;
        var disposed = false;
        view.SetSortCallback(["number"], (input, _) => Repeated(input[0]));

        // Act
        var result = view.SortBy("number");

        // Assert
        Assert.Equal(GridSortStatus.Failed, result.Status);
        Assert.Equal(2, reads);
        Assert.True(disposed);

        IEnumerable<Row> Repeated(Row row)
        {
            try
            {
                while (true)
                {
                    reads++;
                    yield return row;
                }
            }
            finally
            {
                disposed = true;
            }
        }
    }

    [Fact]
    public void CanceledRequestsSkipCallbackAndNestedRequestsCannotCommit()
    {
        // Arrange
        using var view = new GridDataView<Row>(new[] { new Row(0, 1, "a") });
        var calls = 0;
        view.SetSortCallback(["number"], (input, _) =>
        {
            calls++;
            Assert.Equal(GridSortStatus.Rejected, view.SortBy("number").Status);
            view.CancelPendingSort();
            return input;
        });
        static void Cancel(object? sender, GridSortRequestedEventArgs e) => e.Cancel = true;
        view.SortRequested += Cancel;

        // Act & Assert
        Assert.Equal(GridSortStatus.Canceled, view.SortBy("number").Status);
        Assert.Equal(0, calls);

        // Arrange
        view.SortRequested -= Cancel;

        // Act & Assert
        Assert.Equal(GridSortStatus.Superseded, view.SortBy("number").Status);
        Assert.Equal(1, calls);
        Assert.Empty(view.SortOrders);
    }

    [Fact]
    public void SourceMutationRejectsOldResultsAndRefreshUsesTheCallback()
    {
        // Arrange
        var rows = new ObservableCollection<Row> { new(0, 2, "b"), new(1, 1, "a") };
        using var view = new GridDataView<Row>(rows);
        var mutate = true;
        view.SetSortCallback(["number"], (input, orders) =>
        {
            if (mutate)
            {
                mutate = false;
                rows.Add(new Row(2, 0, "new"));
            }

            return SortRows(input, orders);
        });

        // Act & Assert
        Assert.Equal(GridSortStatus.Superseded, view.SortBy("number").Status);
        Assert.Equal(rows, view);
        Assert.Empty(view.SortOrders);

        // Act & Assert
        Assert.Equal(GridSortStatus.Applied, view.SortBy("number").Status);

        // Act
        rows.Add(new Row(3, -1, "add"));

        // Assert
        Assert.Equal<int>([3, 2, 1, 0], view.Select(static row => row.Id));

        // Act
        view.Refresh();

        // Assert
        Assert.Equal<int>([3, 2, 1, 0], view.Select(static row => row.Id));

        // Act & Assert
        Assert.Equal(GridSortStatus.Applied, view.RestoreSortOrders([]).Status);
        Assert.Equal(rows, view);
    }

    [Fact]
    public void AutomaticSortFailureKeepsTheViewUntilAValidRefresh()
    {
        // Arrange
        var rows = new ObservableCollection<Row> { new(0, 2, "b"), new(1, 1, "a") };
        using var view = new GridDataView<Row>(rows);
        view.SetSortCallback(["number"], SortRows);
        view.SortBy("number");
        view.SetSelected(0, true);
        var before = view.ToArray();
        var failures = 0;
        view.SortFailed += (_, _) => failures++;
        view.SetSortCallback(["number"], static (input, _) => input.Take(1));

        // Act
        rows.Add(new Row(2, 0, "new"));

        // Assert
        Assert.Equal(1, failures);
        Assert.Equal(before, view);
        Assert.Same(before[0], Assert.Single(view.SelectedItems));

        // Act
        view.SetSortCallback(["number"], SortRows);
        view.Refresh();

        // Assert
        Assert.Equal<int>([2, 1, 0], view.Select(static row => row.Id));
        Assert.Same(before[0], Assert.Single(view.SelectedItems));
    }

    [Fact]
    public void ItemChangesAndResumeResortWhileKeepingSelectionByIdentity()
    {
        // Arrange
        var first = new MutableRow(2);
        var second = new MutableRow(1);
        var rows = new ObservableCollection<MutableRow> { first, second };
        using var view = new GridDataView<MutableRow>(rows);
        view.SetSortCallback(["number"], static (input, orders) => orders[0].Descending ? input.OrderByDescending(static row => row.Number) : input.OrderBy(static row => row.Number));
        view.SortBy("number");
        view.SetSelected(1, true);

        // Act
        first.Number = 0;

        // Assert
        Assert.Same(first, view[0]);
        Assert.True(view.IsSelected(0));

        // Act
        view.Suspend();
        second.Number = -1;
        view.Resume();

        // Assert
        Assert.Same(second, view[0]);

        // Act
        view.SetSource(new[] { first, second });

        // Assert
        Assert.Same(second, view[0]);
        Assert.Empty(view.SelectedItems);
    }

    [Fact]
    public void CallbackCanBeReplacedOrClearedWithoutUnsupportedActiveKeys()
    {
        // Arrange
        using var view = new GridDataView<Row>(new[] { new Row(0, 1, "a") });
        view.RegisterSort("number", static row => row.Number);
        view.SetSortCallback(["name", "number"], SortRows);
        view.SortBy("name");

        // Act & Assert
        Assert.Throws<ArgumentException>(() => view.SetSortCallback(["number"], SortRows));
        Assert.True(view.CanSort("name"));
        Assert.Throws<InvalidOperationException>(view.ClearSortCallback);
        Assert.Equal(GridSortStatus.Applied, view.SortBy("name").Status);

        // Arrange
        view.RestoreSortOrders([]);
        view.SetSortCallback(["number"], SortRows);
        view.SortBy("number");

        // Act
        view.ClearSortCallback();

        // Assert
        Assert.False(view.CanSort("name"));
        Assert.True(view.CanSort("number"));
        Assert.Equal(GridSortStatus.Applied, view.SortBy("number").Status);
        Assert.True(view.SortOrders[0].Descending);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => view.SetSortCallback([" "], SortRows));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public void CallbackSupportsEmptyOrSingleRowButIsBypassedWhenClearingOrders(int count)
    {
        // Arrange
        using var view = new GridDataView<Row>(Enumerable.Range(0, count).Select(static index => new Row(index, index, "a")));
        var calls = 0;
        view.SetSortCallback(["number"], (input, _) =>
        {
            calls++;
            return input;
        });

        // Act & Assert
        Assert.Equal(GridSortStatus.Applied, view.SortBy("number").Status);
        Assert.Equal(GridSortStatus.Applied, view.RestoreSortOrders([]).Status);
        Assert.Equal(1, calls);
    }

    private static IEnumerable<Row> SortRows(IReadOnlyList<Row> rows, IReadOnlyList<GridSortOrder> orders)
    {
        IOrderedEnumerable<Row>? sorted = null;
        foreach (var order in orders)
        {
            sorted = order.Key == "number"
                ? Append(rows, sorted, static row => row.Number, order.Descending, Comparer<int>.Default)
                : Append(rows, sorted, static row => row.Name, order.Descending, StringComparer.Ordinal);
        }

        return sorted is null ? rows : sorted;
    }

    private static IOrderedEnumerable<Row> Append<TValue>(IReadOnlyList<Row> rows, IOrderedEnumerable<Row>? sorted, Func<Row, TValue> selector, bool descending, IComparer<TValue> comparer) => sorted is null
        ? descending ? rows.OrderByDescending(selector, comparer) : rows.OrderBy(selector, comparer)
        : descending ? sorted.ThenByDescending(selector, comparer) : sorted.ThenBy(selector, comparer);

    private sealed record Row(int Id, int Number, string? Name);

    private sealed class MutableRow(int number) : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        public int Number
        {
            get => number;
            set
            {
                number = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Number)));
            }
        }
    }
}
