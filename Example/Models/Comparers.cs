namespace Example.Models;

// 方向付き比較（descending時は反転）を生成する。GridDataView.RegisterComparerへ渡す。
public static class ComparerFactory
{
    public static Func<T, T, bool, int> Default<T, TValue>(Func<T, TValue> selector)
    {
        var comparer = Comparer<TValue>.Default;
        return (x, y, descending) => comparer.Compare(selector(x), selector(y)) * (descending ? -1 : 1);
    }

    // 方向に関わらず昇順を保つ（未処理グループの先頭固定などに使う）
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
