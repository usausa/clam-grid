namespace ClamGrid;

using System.Diagnostics.CodeAnalysis;

public partial class ClamGridView
{
    public static readonly BindableProperty SortOrdersProperty = BindableProperty.Create(nameof(SortOrders), typeof(IReadOnlyList<GridSortOrder>), typeof(ClamGridView), defaultBindingMode: BindingMode.TwoWay, propertyChanged: OnSortOrdersChanged);
    public static readonly BindableProperty SortCycleProperty = BindableProperty.Create(nameof(SortCycle), typeof(GridSortCycle), typeof(ClamGridView), GridSortCycle.AscendingDescending);

    private GridSortOrder[]? requestedSortOrders;
    private bool applyingSortOrders;

    // Sort keys and directions of the data view; the applied value is written back for TwoWay bindings, an empty list clears the sort and null leaves the view unchanged
    [AllowNull]
    public IReadOnlyList<GridSortOrder> SortOrders
    {
        get => (IReadOnlyList<GridSortOrder>?)GetValue(SortOrdersProperty) ?? DataView?.SortOrders ?? Array.Empty<GridSortOrder>();
        set => SetValue(SortOrdersProperty, value);
    }

    // Direction sequence of repeated header taps on the same column
    public GridSortCycle SortCycle
    {
        get => (GridSortCycle)GetValue(SortCycleProperty);
        set => SetValue(SortCycleProperty, value);
    }

    private static void OnSortOrdersChanged(BindableObject bindable, object? oldValue, object? newValue)
    {
        var grid = (ClamGridView)bindable;
        if (grid.applyingSortOrders)
        {
            return;
        }

        // Orders set before the data view are applied when it arrives
        grid.requestedSortOrders = (newValue as IEnumerable<GridSortOrder>)?.ToArray();
        grid.ApplyRequestedSortOrders();
    }

    // Owned views wrap plain sources without sort keys, so requests wait for a real data view
    private void ApplyRequestedSortOrders()
    {
        if ((requestedSortOrders is not { } orders) || (ownedDataView is not null) || (DataView is not { } view))
        {
            return;
        }

        if (!orders.SequenceEqual(view.SortOrders))
        {
            view.RestoreSortOrders(orders);
        }

        PublishSortOrders();
    }

    // Writes the value back as a control side change so OneWay bindings survive and TwoWay bindings update the source
    private void PublishSortOrders()
    {
        if ((ownedDataView is not null) || (DataView is not { } view))
        {
            return;
        }

        requestedSortOrders = view.SortOrders.ToArray();
        if ((GetValue(SortOrdersProperty) is IReadOnlyList<GridSortOrder> current) && current.SequenceEqual(view.SortOrders))
        {
            return;
        }

        applyingSortOrders = true;
        try
        {
            SetValueFromRenderer(SortOrdersProperty, view.SortOrders);
        }
        finally
        {
            applyingSortOrders = false;
        }
    }
}
