namespace Example.Models;

// Creates direction aware comparisons (reversed when descending) for GridDataView.RegisterComparer
public static class ComparerFactory
{
    public static Func<T, T, bool, int> Default<T, TValue>(Func<T, TValue> selector)
    {
        var comparer = Comparer<TValue>.Default;
        return (x, y, descending) => comparer.Compare(selector(x), selector(y)) * (descending ? -1 : 1);
    }

    // Stays ascending regardless of the direction, used to pin the pending group first
    public static Func<T, T, bool, int> Ascending<T, TValue>(Func<T, TValue> selector)
    {
        var comparer = Comparer<TValue>.Default;
        return (x, y, _) => comparer.Compare(selector(x), selector(y));
    }

    public static Func<T, T, bool, int> Chain<T>(params Func<T, T, bool, int>[] comparers) =>
        (x, y, descending) =>
        {
            foreach (var comparer in comparers)
            {
                var result = comparer(x, y, descending);
                if (result != 0)
                {
                    return result;
                }
            }

            return 0;
        };
}

public static class GridDataViewExtensions
{
    public static void RegisterComparers<T>(this GridDataView<T> view, IReadOnlyDictionary<string, Func<T, T, bool, int>> comparers)
        where T : class
    {
        foreach (var (key, comparer) in comparers)
        {
            view.RegisterComparer(key, comparer);
        }
    }
}
