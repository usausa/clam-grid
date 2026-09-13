namespace ClamGrid;

using System.ComponentModel;

public interface IGridDataView : INotifyPropertyChanged
{
    event EventHandler<GridDataChangedEventArgs>? Changed;

    event EventHandler? SelectionChanged;

    event EventHandler<GridSortRequestedEventArgs>? SortRequested;

    event EventHandler? SortChanged;

    event EventHandler<GridSortFailedEventArgs>? SortFailed;

    IReadOnlyList<object> Items { get; }

    IEqualityComparer<object> RowKeyComparer { get; }

    long Version { get; }

    long ResetVersion { get; }

    int SelectedCount { get; }

    IReadOnlyList<object> SelectedItems { get; }

    GridSelectionMode SelectionMode { get; set; }

    IReadOnlyList<GridSortOrder> SortOrders { get; }

    object GetRowKey(int rowIndex);

    int IndexOfKey(object key);

    bool IsSelected(int rowIndex);

    bool SetSelected(int rowIndex, bool value);

    bool TryToggleSelection(int rowIndex, out bool isSelected);

    void SelectAll();

    void ClearSelection();

    bool CanSort(string key);

    GridSortResult SortBy(string key);

    GridSortResult SortBy(string key, GridSortCycle cycle);

    GridSortResult RestoreSortOrders(IEnumerable<GridSortOrder> orders);

    void CancelPendingSort();

    void Refresh();
}
