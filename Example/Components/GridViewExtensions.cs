namespace Example.Components;

// Rebuilds the visible columns from the public definitions and states, the same way the grid arranges them
public static class GridViewExtensions
{
    public static IReadOnlyList<GridColumn> VisibleColumns(this ClamGridView grid)
    {
        var map = grid.ColumnDefinitions.ToDictionary(static column => column.Key, StringComparer.Ordinal);
        return grid.ColumnStates
            .Where(static state => state.IsVisible)
            .Select(state => state.Width is { } width ? map[state.Key] with { Width = GridColumnWidth.Absolute(width) } : map[state.Key])
            .ToArray();
    }
}
