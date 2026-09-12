namespace ClamGrid;

using System.Collections;

// 列キーと型付きアクセサの対応表。コレクション初期化子で { "Key", static x => x.Property } のように登録する。
public sealed class GridValueAccessorCollection<TItem> : IGridValueAccessorProvider, IEnumerable<KeyValuePair<string, IGridValueAccessor>>
    where TItem : class
{
    private readonly Dictionary<string, IGridValueAccessor> accessors = [with(StringComparer.Ordinal)];

    public int Count => accessors.Count;

    public void Add<TValue>(string key, Func<TItem, TValue> getter, Action<TItem, TValue>? setter = null) =>
        Add(key, new GridValueAccessor<TItem, TValue>(getter, setter));

    public void Add(string key, IGridValueAccessor accessor)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentNullException.ThrowIfNull(accessor);
        accessors.Add(key, accessor);
    }

    public IGridValueAccessor? GetValueAccessor(string key) => accessors.GetValueOrDefault(key);

    public IEnumerator<KeyValuePair<string, IGridValueAccessor>> GetEnumerator() => accessors.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
