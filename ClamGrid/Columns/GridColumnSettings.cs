namespace ClamGrid.Columns;

internal static class GridColumnSettings
{
    // Unknown keys are dropped, new columns are appended hidden and a saved width is clamped to the minimum width of its column
    public static GridColumnState[] Normalize(IReadOnlyList<GridColumn> columns, IEnumerable<GridColumnState>? states)
    {
        ArgumentNullException.ThrowIfNull(columns);
        var minWidths = new Dictionary<string, double>(StringComparer.Ordinal);
        foreach (var column in columns)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(column.Key);
            if (!minWidths.TryAdd(column.Key, column.MinWidth))
            {
                throw new ArgumentException("Column keys must be unique.", nameof(columns));
            }
        }

        var result = new List<GridColumnState>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        if (states is not null)
        {
            foreach (var state in states)
            {
                if (minWidths.TryGetValue(state.Key, out var minWidth) && seen.Add(state.Key))
                {
                    result.Add(ClampWidth(state, minWidth));
                }
            }
        }

        if (!result.Any(static state => state.IsVisible))
        {
            // Saved widths survive the fallback to the default visibility
            var widths = result.ToDictionary(static state => state.Key, static state => state.Width, StringComparer.Ordinal);
            return columns.Select(column => new GridColumnState(column.Key, true) { Width = widths.GetValueOrDefault(column.Key) }).ToArray();
        }

        result.AddRange(columns.Where(column => !seen.Contains(column.Key)).Select(static column => new GridColumnState(column.Key, false)));
        return result.ToArray();
    }

    public static void ValidateKeys(IEnumerable<string> columnKeys)
    {
        ArgumentNullException.ThrowIfNull(columnKeys);
        var known = new HashSet<string>(StringComparer.Ordinal);
        foreach (var key in columnKeys)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(key);
            if (!known.Add(key))
            {
                throw new ArgumentException("Column keys must be unique.", nameof(columnKeys));
            }
        }
    }

    // Visible columns in display order; a saved width replaces the declared width
    public static GridColumn[] Arrange(IReadOnlyList<GridColumn> catalog, IEnumerable<GridColumnState> states)
    {
        var map = catalog.ToDictionary(static column => column.Key, StringComparer.Ordinal);
        return states.Where(static state => state.IsVisible).Select(state => state.Width is { } width ? map[state.Key] with { Width = GridColumnWidth.Absolute(width) } : map[state.Key]).ToArray();
    }

    private static GridColumnState ClampWidth(GridColumnState state, double minWidth)
    {
        if (state.Width is not { } width)
        {
            return state;
        }

        if (!Double.IsFinite(width) || (width <= 0))
        {
            return state with { Width = null };
        }

        return width < minWidth ? state with { Width = minWidth } : state;
    }
}
