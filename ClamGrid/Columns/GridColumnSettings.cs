namespace ClamGrid.Columns;

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
                    result.Add((order.Width is { } width) && (!Double.IsFinite(width) || (width <= 0)) ? order with { Width = null } : order);
                }
            }
        }

        if (!result.Any(static order => order.IsVisible))
        {
            // Saved widths survive the fallback to the default visibility
            var widths = result.ToDictionary(static order => order.Key, static order => order.Width, StringComparer.Ordinal);
            return keys.Select(key => new GridColumnOrder(key, true) { Width = widths.GetValueOrDefault(key) }).ToArray();
        }

        result.AddRange(keys.Where(key => !seen.Contains(key)).Select(static key => new GridColumnOrder(key, false)));
        return result.ToArray();
    }

    // Null when the key is unknown, the value is unchanged, or the last visible column would be hidden
    public static GridColumnOrder[]? SetVisible(IReadOnlyList<GridColumnOrder> orders, string key, bool value)
    {
        var index = IndexOf(orders, key);
        if ((index < 0) || (orders[index].IsVisible == value))
        {
            return null;
        }

        if (!value && (orders.Count(static order => order.IsVisible) <= 1))
        {
            return null;
        }

        var result = orders.ToArray();
        result[index] = result[index] with { IsVisible = value };
        return result;
    }

    // Null when the key is unknown or the width is unchanged; null width restores the declared width
    public static GridColumnOrder[]? SetWidth(IReadOnlyList<GridColumnOrder> orders, string key, double? width)
    {
        var index = IndexOf(orders, key);
        if ((index < 0) || Nullable.Equals(orders[index].Width, width))
        {
            return null;
        }

        var result = orders.ToArray();
        result[index] = result[index] with { Width = width };
        return result;
    }

    // Visible columns in display order; a saved width replaces the declared width
    public static GridColumn[] Arrange(IReadOnlyList<GridColumn> catalog, IEnumerable<GridColumnOrder> orders)
    {
        var map = catalog.ToDictionary(static column => column.Key, StringComparer.Ordinal);
        return orders.Where(static order => order.IsVisible).Select(order => ApplyWidth(map[order.Key], order.Width)).ToArray();
    }

    public static GridColumn ApplyWidth(GridColumn column, double? width) =>
        width is { } value ? column with { Width = GridColumnWidth.Absolute(Math.Max(value, column.MinWidth)) } : column;

    // Takes direct edits of the visible columns into the definitions; an edit that changes a saved width drops the saved width
    public static (GridColumn[] Catalog, GridColumnOrder[] Orders) Merge(IReadOnlyList<GridColumn> catalog, IEnumerable<GridColumn> visible, IReadOnlyList<GridColumnOrder> orders)
    {
        var edited = visible.ToDictionary(static column => column.Key, StringComparer.Ordinal);
        var widths = orders.ToDictionary(static order => order.Key, static order => order.Width, StringComparer.Ordinal);
        var dropped = new HashSet<string>(StringComparer.Ordinal);
        var result = new GridColumn[catalog.Count];
        for (var i = 0; i < catalog.Count; i++)
        {
            var declared = catalog[i];
            result[i] = declared;
            if (!edited.TryGetValue(declared.Key, out var column) || ReferenceEquals(column, declared))
            {
                continue;
            }

            var width = widths.GetValueOrDefault(declared.Key);
            if ((width is null) || !column.Width.Equals(ApplyWidth(declared, width).Width))
            {
                result[i] = column;
                if (width is not null)
                {
                    dropped.Add(declared.Key);
                }
            }
            else
            {
                // The saved width stays in effect, so the definition keeps its declared width
                var definition = column with { Width = declared.Width };
                result[i] = definition.Equals(declared) ? declared : definition;
            }
        }

        var nextOrders = orders.Select(order => dropped.Contains(order.Key) ? order with { Width = null } : order).ToArray();
        return (result, nextOrders);
    }

    private static int IndexOf(IReadOnlyList<GridColumnOrder> orders, string key)
    {
        for (var i = 0; i < orders.Count; i++)
        {
            if (String.Equals(orders[i].Key, key, StringComparison.Ordinal))
            {
                return i;
            }
        }

        return -1;
    }
}
