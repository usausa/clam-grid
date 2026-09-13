namespace ClamGrid;

using System.Collections;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

public sealed class GridDataView<T> : IReadOnlyList<T>, IGridDataView, INotifyCollectionChanged, IDisposable
    where T : class
{
    private readonly int ownerThread = Environment.CurrentManagedThreadId;
    private readonly Func<T, object?>? keySelector;
    private readonly Dictionary<string, Func<T, T, bool, int>> comparers = [with(StringComparer.Ordinal)];
    private readonly HashSet<INotifyPropertyChanged> observedItems = [with(ReferenceEqualityComparer.Instance)];
    private readonly HashSet<T> removedItems = [with(ReferenceEqualityComparer.Instance)];
    private readonly HashSet<object> selected;
    private Dictionary<T, object> keysByItem = [with(ReferenceEqualityComparer.Instance)];
    private IEnumerable source;
    private T[] items = [];
    private object[] keys = [];
    private Dictionary<object, int> indices;
    private GridSortCallback<T>? sortCallback;
    private HashSet<string> callbackKeys = [with(StringComparer.Ordinal)];
    private bool connected;
    private bool disposed;
    private bool sorting;
    private bool synchronizing;
    private bool publishing;
    private GridDataChangeKind? pendingChange;
    private bool pendingReset;

    public event PropertyChangedEventHandler? PropertyChanged;

    public event NotifyCollectionChangedEventHandler? CollectionChanged;

    public event EventHandler<GridDataChangedEventArgs>? Changed;

    public event EventHandler? SelectionChanged;

    public event EventHandler<GridSortRequestedEventArgs>? SortRequested;

    public event EventHandler? SortChanged;

    public event EventHandler<GridSortFailedEventArgs>? SortFailed;

    public int Count => items.Length;

    public T this[int index] => items[index];

    public IReadOnlyList<object> Items { get; private set; } = Array.Empty<object>();

    public IEqualityComparer<object> RowKeyComparer { get; }

    public long Version { get; private set; }

    public long ResetVersion { get; private set; }

    public int SelectedCount => selected.Count;

    public IReadOnlyList<object> SelectedItems => Array.AsReadOnly(items.Where((_, index) => selected.Contains(keys[index])).Cast<object>().ToArray());

    public IReadOnlyList<GridSortOrder> SortOrders { get; private set; } = Array.Empty<GridSortOrder>();

    public GridSelectionMode SelectionMode
    {
        get;
        set
        {
            RequireMutation();
            if (!Enum.IsDefined(value))
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            if (field == value)
            {
                return;
            }

            field = value;
            var next = keys.Where(selected.Contains).Take(value == GridSelectionMode.None ? 0 : value == GridSelectionMode.SingleToggle ? 1 : Count);
            ApplySelection(new HashSet<object>(next, RowKeyComparer), true);
        }
    }

    public GridDataView(IEnumerable source, Func<T, object?>? keySelector = null)
    {
        ArgumentNullException.ThrowIfNull(source);
        this.source = source;
        this.keySelector = keySelector;
        RowKeyComparer = keySelector is null ? ReferenceEqualityComparer.Instance : EqualityComparer<object>.Default;
        indices = [with(RowKeyComparer)];
        selected = [with(RowKeyComparer)];
        SelectionMode = GridSelectionMode.MultipleToggle;
        Synchronize(GridDataChangeKind.Reset, true);
        Connect();
    }

    //--------------------------------------------------------------------------------
    // Selection
    //--------------------------------------------------------------------------------

    public object GetRowKey(int rowIndex) => keys[rowIndex];

    public int IndexOfKey(object key) => indices.GetValueOrDefault(key, -1);

    public bool IsSelected(int rowIndex) => (rowIndex >= 0) && (rowIndex < Count) && selected.Contains(keys[rowIndex]);

    public bool SetSelected(int rowIndex, bool value)
    {
        RequireMutation();
        if ((rowIndex < 0) || (rowIndex >= Count) || ((SelectionMode == GridSelectionMode.None) && value))
        {
            return false;
        }

        var next = new HashSet<object>(selected, RowKeyComparer);
        if (value)
        {
            if (SelectionMode == GridSelectionMode.SingleToggle)
            {
                next.Clear();
            }

            next.Add(keys[rowIndex]);
        }
        else
        {
            next.Remove(keys[rowIndex]);
        }

        ApplySelection(next);
        return true;
    }

    public bool TryToggleSelection(int rowIndex, out bool isSelected)
    {
        isSelected = false;
        if ((SelectionMode == GridSelectionMode.None) || !SetSelected(rowIndex, !IsSelected(rowIndex)))
        {
            return false;
        }

        isSelected = IsSelected(rowIndex);
        return true;
    }

    public void SelectAll() => UpdateSelection(static _ => true);

    public void ClearSelection() => UpdateSelection(static _ => false);

    public void UpdateSelection(Func<T, bool> predicate)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        RequireMutation();
        var version = Version;
        var snapshotItems = items;
        var snapshotKeys = keys;
        var next = new HashSet<object>(RowKeyComparer);
        if (SelectionMode != GridSelectionMode.None)
        {
            for (var i = 0; i < snapshotItems.Length; i++)
            {
                if (predicate(snapshotItems[i]))
                {
                    next.Add(snapshotKeys[i]);
                    if (SelectionMode == GridSelectionMode.SingleToggle)
                    {
                        break;
                    }
                }
            }
        }

        if (version != Version)
        {
            throw new InvalidOperationException("Selection predicates must not mutate the source.");
        }

        ApplySelection(next);
    }

    //--------------------------------------------------------------------------------
    // Sort
    //--------------------------------------------------------------------------------

    public void RegisterComparer(string key, Func<T, T, bool, int> comparison)
    {
        RequireMutation();
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentNullException.ThrowIfNull(comparison);
        comparers[key] = comparison;
        Version++;
    }

    public void RegisterSort<TValue>(string key, Func<T, TValue> selector, IComparer<TValue>? comparer = null)
    {
        ArgumentNullException.ThrowIfNull(selector);
        comparer ??= Comparer<TValue>.Default;
        RegisterComparer(key, (left, right, descending) => descending ? comparer.Compare(selector(right), selector(left)) : comparer.Compare(selector(left), selector(right)));
    }

    // Delegates sorting for the given keys (hidden ones included) to the callback from the next sort or data sync
    public void SetSortCallback(IEnumerable<string> sortKeys, GridSortCallback<T> callback)
    {
        RequireMutation();
        ArgumentNullException.ThrowIfNull(sortKeys);
        ArgumentNullException.ThrowIfNull(callback);
        var nextKeys = new HashSet<string>(StringComparer.Ordinal);
        foreach (var key in sortKeys)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(key);
            nextKeys.Add(key);
        }

        if (SortOrders.Any(order => !nextKeys.Contains(order.Key)))
        {
            throw new ArgumentException("The callback must support all active sort keys. Clear sort orders before changing supported keys.", nameof(sortKeys));
        }

        callbackKeys = nextKeys;
        sortCallback = callback;
        Version++;
    }

    // Returns to the standard sort with the registered comparers, which must cover every current sort key
    public void ClearSortCallback()
    {
        RequireMutation();
        if (sortCallback is null)
        {
            return;
        }

        if (SortOrders.Any(order => !comparers.ContainsKey(order.Key)))
        {
            throw new InvalidOperationException("Register comparers for all active keys or clear sort orders before returning to the default sort.");
        }

        sortCallback = null;
        callbackKeys.Clear();
        Version++;
    }

    public bool CanSort(string key) => sortCallback is null ? comparers.ContainsKey(key) : callbackKeys.Contains(key);

    public GridSortResult SortBy(string key)
    {
        RequireAccess();
        if (!CanSort(key))
        {
            return new GridSortResult(GridSortStatus.Rejected, Array.AsReadOnly([key]));
        }

        return SortBy(key, GridSortCycle.AscendingDescending);
    }

    // Repeated sorts on the primary key follow the cycle; any other key becomes the primary key in the first direction
    public GridSortResult SortBy(string key, GridSortCycle cycle)
    {
        RequireAccess();
        if (!CanSort(key))
        {
            return new GridSortResult(GridSortStatus.Rejected, Array.AsReadOnly([key]));
        }

        var first = cycle is GridSortCycle.DescendingAscending or GridSortCycle.DescendingAscendingNone;
        var rest = SortOrders.Where(order => order.Key != key);
        var primary = (SortOrders.Count > 0) && (SortOrders[0].Key == key) ? SortOrders[0] : null;
        if (primary is null)
        {
            return RestoreSortOrders(rest.Prepend(new GridSortOrder(key, first)));
        }

        if (primary.Descending == first)
        {
            return RestoreSortOrders(rest.Prepend(new GridSortOrder(key, !first)));
        }

        var clears = cycle is GridSortCycle.AscendingDescendingNone or GridSortCycle.DescendingAscendingNone;
        return RestoreSortOrders(clears ? rest : rest.Prepend(new GridSortOrder(key, first)));
    }

    public GridSortResult RestoreSortOrders(IEnumerable<GridSortOrder> orders)
    {
        RequireAccess();
        ArgumentNullException.ThrowIfNull(orders);
        if (sorting || synchronizing || publishing)
        {
            return new GridSortResult(GridSortStatus.Rejected, Array.Empty<string>());
        }

        var known = new List<GridSortOrder>();
        var ignored = new List<string>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var order in orders)
        {
            ArgumentNullException.ThrowIfNull(order);
            if (seen.Add(order.Key))
            {
                if (CanSort(order.Key))
                {
                    known.Add(order);
                }
                else
                {
                    ignored.Add(order.Key);
                }
            }
        }

        var nextOrders = Array.AsReadOnly(known.ToArray());
        var ignoredKeys = Array.AsReadOnly(ignored.ToArray());
        sorting = true;
        var version = Version;
        try
        {
            var request = new GridSortRequestedEventArgs(nextOrders);
            SortRequested?.Invoke(this, request);
            if (request.Cancel)
            {
                return new GridSortResult(GridSortStatus.Canceled, ignoredKeys);
            }

            if (version != Version)
            {
                return new GridSortResult(GridSortStatus.Superseded, ignoredKeys);
            }

            T[] ordered;
            try
            {
                ordered = SortItems(nextOrders.Count == 0 ? SnapshotSource() : items, nextOrders);
            }
            catch (Exception error) when (error is InvalidOperationException or ArgumentException)
            {
                SortFailed?.Invoke(this, new GridSortFailedEventArgs(error));
                return new GridSortResult(GridSortStatus.Failed, ignoredKeys, error);
            }

            if (version != Version)
            {
                return new GridSortResult(GridSortStatus.Superseded, ignoredKeys);
            }

            Commit(ordered, GridDataChangeKind.Sort, false, nextOrders);
            return new GridSortResult(GridSortStatus.Applied, ignoredKeys);
        }
        finally
        {
            sorting = false;
            DrainChanges();
        }
    }

    public GridSortOrder[] SaveSortOrders() => SortOrders.ToArray();

    public void CancelPendingSort()
    {
        RequireAccess();
        Version++;
    }

    //--------------------------------------------------------------------------------
    // Source
    //--------------------------------------------------------------------------------

    public void Refresh()
    {
        RequireMutation();
        Version++;
        Synchronize(GridDataChangeKind.Refresh, false);
    }

    public void SetSource(IEnumerable value)
    {
        RequireMutation();
        ArgumentNullException.ThrowIfNull(value);
        var previous = source;
        var wasConnected = connected;
        Disconnect();
        source = value;
        T[] replacement;
        try
        {
            replacement = SnapshotSource();
            ValidateKeys(replacement);
        }
        catch
        {
            source = previous;
            if (wasConnected)
            {
                Connect();
            }

            throw;
        }

        Version++;
        Synchronize(GridDataChangeKind.Reset, true, replacement);
        Connect();
    }

    public void Suspend()
    {
        RequireAccess();
        Version++;
        Disconnect();
    }

    public void Resume()
    {
        RequireMutation();
        if (connected)
        {
            return;
        }

        // Notifications may have been missed while disconnected, including Reset
        Synchronize(GridDataChangeKind.Reset, true);
        Connect();
    }

    public IEnumerator<T> GetEnumerator() => ((IEnumerable<T>)items).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public void Dispose()
    {
        if (!disposed)
        {
            RequireAccess();
            Disconnect();
            disposed = true;
            Version++;
        }
    }

    //--------------------------------------------------------------------------------
    // Internal
    //--------------------------------------------------------------------------------

    private static T[] Snapshot(IEnumerable value)
    {
        var result = new List<T>();
        foreach (var item in value)
        {
            if ((item is not T typed) || item.GetType().IsValueType)
            {
                throw new ArgumentException("Rows must be non-null instances of the declared reference type.", nameof(value));
            }

            result.Add(typed);
        }

        return result.ToArray();
    }

    private T[] SnapshotSource() => Snapshot(source);

    private object[] ValidateKeys(T[] rows)
    {
        var result = new object[rows.Length];
        var seen = new HashSet<object>(RowKeyComparer);
        for (var i = 0; i < rows.Length; i++)
        {
            var key = keySelector is null ? rows[i] : keySelector(rows[i]);
            if ((key is null) || !seen.Add(key))
            {
                throw new ArgumentException("Row keys must be non-null and unique.");
            }

            if (keysByItem.TryGetValue(rows[i], out var previous) && !RowKeyComparer.Equals(previous, key))
            {
                throw new InvalidOperationException("Row keys are immutable. Replace the row object when its identity changes.");
            }

            result[i] = key;
        }

        return result;
    }

    private T[] SortItems(T[] rows, IReadOnlyList<GridSortOrder> orders)
    {
        ValidateKeys(rows);
        if (orders.Count == 0)
        {
            return rows;
        }

        if (sortCallback is { } callback)
        {
            return SortWithCallback(rows, orders, callback);
        }

        var comparisons = orders.Select(order => (Comparison: comparers[order.Key], order.Descending)).ToArray();
        var positions = Enumerable.Range(0, rows.Length).ToArray();
        Array.Sort(positions, (left, right) =>
        {
            foreach (var entry in comparisons)
            {
                var result = entry.Comparison(rows[left], rows[right], entry.Descending);
                if (result != 0)
                {
                    return result;
                }
            }

            return left.CompareTo(right);
        });
        return positions.Select(index => rows[index]).ToArray();
    }

    [SuppressMessage("Design", "CA1031", Justification = "User sort code and lazy enumeration failures take the same failure path as the standard sort so the committed view is kept")]
    private static T[] SortWithCallback(T[] rows, IReadOnlyList<GridSortOrder> orders, GridSortCallback<T> callback)
    {
        try
        {
            var remaining = new HashSet<T>(rows, ReferenceEqualityComparer.Instance);
            var result = new T[rows.Length];
            var input = Array.AsReadOnly(rows.ToArray());
            var output = callback(input, orders) ?? throw new InvalidOperationException("Sort callbacks must return a sequence.");
            var index = 0;
            foreach (var row in output)
            {
                if ((index >= result.Length) || !remaining.Remove(row))
                {
                    throw new InvalidOperationException("Sort callbacks must return each input row exactly once, without replacements or extra rows.");
                }

                result[index++] = row;
            }

            if (index != rows.Length)
            {
                throw new InvalidOperationException("Sort callbacks must not omit input rows.");
            }

            return result;
        }
        catch (Exception error) when (error is not OutOfMemoryException)
        {
            // Like an Array.Sort comparer, user exceptions are forwarded to the common failure notification
            throw new InvalidOperationException("The sort callback failed or returned an invalid row sequence.", error);
        }
    }

    private void ApplySelection(HashSet<object> next, bool modeChanged = false)
    {
        var changed = !selected.SetEquals(next);
        selected.Clear();
        selected.UnionWith(next);
        if (changed || modeChanged)
        {
            Publish(new GridDataChangedEventArgs(GridDataChangeKind.Selection, false, changed));
        }
    }

    private void Synchronize(GridDataChangeKind kind, bool reset, T[]? snapshot = null)
    {
        synchronizing = true;
        try
        {
            var version = Version;
            var replacement = snapshot ?? (kind == GridDataChangeKind.Item ? items : SnapshotSource());
            ValidateKeys(replacement);
            if (SortOrders.Count > 0)
            {
                try
                {
                    replacement = SortItems(replacement, SortOrders);
                }
                catch (Exception error) when (error is InvalidOperationException or ArgumentException)
                {
                    // Preserve the last committed view until a valid Refresh succeeds
                    SortFailed?.Invoke(this, new GridSortFailedEventArgs(error));
                    return;
                }
            }

            if (version == Version)
            {
                Commit(replacement, kind, reset);
            }
        }
        finally
        {
            synchronizing = false;
        }

        DrainChanges();
    }

    private void Commit(T[] replacement, GridDataChangeKind kind, bool reset, IReadOnlyList<GridSortOrder>? orders = null)
    {
        var nextKeys = ValidateKeys(replacement);
        var nextIndices = new Dictionary<object, int>(RowKeyComparer);
        var nextKeysByItem = new Dictionary<T, object>(ReferenceEqualityComparer.Instance);
        var keep = new HashSet<object>(RowKeyComparer);
        for (var i = 0; i < replacement.Length; i++)
        {
            var key = nextKeys[i];
            nextIndices.Add(key, i);
            nextKeysByItem.Add(replacement[i], key);
            if (!reset && indices.TryGetValue(key, out var oldIndex) && ReferenceEquals(items[oldIndex], replacement[i]) && !removedItems.Contains(replacement[i]) && selected.Contains(key))
            {
                keep.Add(key);
            }
        }

        var orderChanged = !items.SequenceEqual(replacement, ReferenceEqualityComparer.Instance);
        var selectionChanged = !selected.SetEquals(keep);
        items = replacement;
        Items = Array.AsReadOnly(replacement);
        keys = nextKeys;
        indices = nextIndices;
        keysByItem = nextKeysByItem;
        selected.Clear();
        selected.UnionWith(keep);
        removedItems.Clear();
        if (orders is not null)
        {
            SortOrders = orders;
        }

        Version++;
        if (kind == GridDataChangeKind.Reset)
        {
            ResetVersion = Version;
        }

        if (connected)
        {
            ObserveItems();
        }

        Publish(new GridDataChangedEventArgs(kind, orderChanged, selectionChanged));
    }

    private void Publish(GridDataChangedEventArgs args)
    {
        publishing = true;
        try
        {
            // All state is committed before any notification can inspect it
            Changed?.Invoke(this, args);
            if (args.Kind != GridDataChangeKind.Selection)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Count)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Items)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Item[]"));
                if (args.OrderChanged || (args.Kind == GridDataChangeKind.Reset))
                {
                    CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
                }
            }

            if (args.SelectionChanged || args.OrderChanged)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedCount)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedItems)));
            }

            if (args.SelectionChanged)
            {
                SelectionChanged?.Invoke(this, EventArgs.Empty);
            }

            if (args.Kind == GridDataChangeKind.Selection)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectionMode)));
            }

            if (args.Kind == GridDataChangeKind.Sort)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SortOrders)));
                SortChanged?.Invoke(this, EventArgs.Empty);
            }
        }
        finally
        {
            publishing = false;
        }

        DrainChanges();
    }

    private void OnSourceChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        RequireAccess();
        Version++;
        pendingReset |= e.Action == NotifyCollectionChangedAction.Reset;
        pendingChange = e.Action switch
        {
            NotifyCollectionChangedAction.Add => GridDataChangeKind.Add,
            NotifyCollectionChangedAction.Remove => GridDataChangeKind.Remove,
            NotifyCollectionChangedAction.Move => GridDataChangeKind.Move,
            NotifyCollectionChangedAction.Replace => GridDataChangeKind.Replace,
            _ => GridDataChangeKind.Reset
        };
        if (((e.Action == NotifyCollectionChangedAction.Remove) || (e.Action == NotifyCollectionChangedAction.Replace)) && (e.OldItems is not null))
        {
            foreach (var item in e.OldItems.OfType<T>())
            {
                removedItems.Add(item);
            }
        }

        DrainChanges();
    }

    private void OnItemChanged(object? sender, PropertyChangedEventArgs e)
    {
        RequireAccess();
        Version++;
        pendingChange ??= GridDataChangeKind.Item;
        DrainChanges();
    }

    private void DrainChanges()
    {
        if (sorting || synchronizing || publishing || disposed)
        {
            return;
        }

        if (pendingChange is { } kind)
        {
            var reset = pendingReset;
            pendingChange = null;
            pendingReset = false;
            Synchronize(reset ? GridDataChangeKind.Reset : kind, reset);
        }
    }

    private void Connect()
    {
        if (source is INotifyCollectionChanged notifier)
        {
            notifier.CollectionChanged += OnSourceChanged;
        }

        connected = true;
        ObserveItems();
    }

    private void ObserveItems()
    {
        var current = new HashSet<INotifyPropertyChanged>(items.OfType<INotifyPropertyChanged>(), ReferenceEqualityComparer.Instance);
        foreach (var item in observedItems.Where(item => !current.Contains(item)).ToArray())
        {
            item.PropertyChanged -= OnItemChanged;
            observedItems.Remove(item);
        }

        foreach (var item in current)
        {
            if (observedItems.Add(item))
            {
                item.PropertyChanged += OnItemChanged;
            }
        }
    }

    private void Disconnect()
    {
        if (connected && (source is INotifyCollectionChanged notifier))
        {
            notifier.CollectionChanged -= OnSourceChanged;
        }

        foreach (var item in observedItems)
        {
            item.PropertyChanged -= OnItemChanged;
        }

        observedItems.Clear();
        connected = false;
    }

    private void RequireMutation()
    {
        RequireAccess();
        if (publishing || sorting || synchronizing)
        {
            throw new InvalidOperationException("Reentrant view mutations are not supported; defer them until the notification returns.");
        }
    }

    private void RequireAccess()
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        if (Environment.CurrentManagedThreadId != ownerThread)
        {
            throw new InvalidOperationException("Use the thread that created the data view; dispatch background results to that thread.");
        }
    }
}
