namespace ClamGrid;

using System.Collections.ObjectModel;

using ClamGrid.Columns;

public sealed class GridColumnEditSession
{
    public ObservableCollection<GridColumnOption> Columns { get; }

    public GridColumnEditSession(IEnumerable<GridColumnOption> columns)
    {
        ArgumentNullException.ThrowIfNull(columns);
        var copy = columns.Select(static column => new GridColumnOption(column.Key, column.Header, column.IsVisible) { Width = column.Width }).ToArray();
        GridColumnSettings.ValidateKeys(copy.Select(static column => column.Key));
        Columns = [with(copy)];
    }

    // Creates an editable copy from the definitions and saved orders; unknown keys are dropped and new columns are appended hidden
    public static GridColumnEditSession Create(IEnumerable<GridColumn> columns, IEnumerable<GridColumnState>? orders = null)
    {
        ArgumentNullException.ThrowIfNull(columns);
        var catalog = columns.ToArray();
        var headers = catalog.ToDictionary(static column => column.Key, static column => column.Header, StringComparer.Ordinal);
        return new GridColumnEditSession(GridColumnSettings.Normalize(catalog, orders).Select(state => new GridColumnOption(state.Key, headers[state.Key], state.IsVisible) { Width = state.Width }));
    }

    public GridColumnState[] Export() => Columns.Select(static column => new GridColumnState(column.Key, column.IsVisible) { Width = column.Width }).ToArray();
}
