namespace ClamGrid.Internal.Columns;

internal static class GridColumnSettings
{
    public static GridColumnOrder[] Normalize(IEnumerable<string> columnKeys, IEnumerable<GridColumnOrder>? orders)
    {
        ArgumentNullException.ThrowIfNull(columnKeys);
        var keys = columnKeys.ToArray();
        var known = new HashSet<string>(StringComparer.Ordinal);
        foreach (var key in keys)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(key);
            if (!known.Add(key))
            {
                throw new ArgumentException("Column keys must be unique.", nameof(columnKeys));
            }
        }

        var result = new List<GridColumnOrder>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        if (orders is not null)
        {
            foreach (var order in orders)
            {
                if (known.Contains(order.Key) && seen.Add(order.Key))
                {
                    result.Add(order);
                }
            }
        }

        if (!result.Any(static order => order.IsVisible))
        {
            return keys.Select(static key => new GridColumnOrder(key, true)).ToArray();
        }

        result.AddRange(keys.Where(key => !seen.Contains(key)).Select(static key => new GridColumnOrder(key, false)));
        return result.ToArray();
    }
}
