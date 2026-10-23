namespace ClamGrid;

public sealed class GridColumnConfigurationEventArgs(IReadOnlyList<GridColumn> columns, string? columnKey, IReadOnlyList<GridColumnState>? orders = null) : EventArgs
{
    public IReadOnlyList<GridColumn> Columns { get; } = columns;

    public string? ColumnKey { get; } = columnKey;

    public IReadOnlyList<GridColumnState> Orders { get; } = orders ?? Array.AsReadOnly(columns.Select(static column => new GridColumnState(column.Key, true)).ToArray());

    public bool Handled { get; set; }

    public GridColumnEditSession CreateEditSession() => GridColumnEditSession.Create(Columns, Orders);
}
