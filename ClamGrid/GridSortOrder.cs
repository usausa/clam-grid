namespace ClamGrid;

public sealed record GridSortOrder
{
    public string Key { get; }

    public bool Descending { get; }

    public GridSortOrder(string key, bool descending = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        Key = key;
        Descending = descending;
    }
}
