namespace ClamGrid;

using System.Collections.ObjectModel;

public sealed class GridRowMover<T>(ObservableCollection<T> source, GridDataView<T> view) : IGridRowMover
    where T : class
{
    public bool CanMove
    {
        get
        {
            if ((view.SortOrders.Count != 0) || (source.Count != view.Count))
            {
                return false;
            }

            for (var index = 0; index < source.Count; index++)
            {
                if (!ReferenceEquals(source[index], view[index]))
                {
                    return false;
                }
            }

            return true;
        }
    }

    public bool Move(object rowKey, int targetIndex)
    {
        var from = view.IndexOfKey(rowKey);
        if ((from < 0) || (targetIndex < 0) || (targetIndex >= source.Count) || (from == targetIndex) || !CanMove)
        {
            return false;
        }

        source.Move(from, targetIndex);
        return true;
    }
}
