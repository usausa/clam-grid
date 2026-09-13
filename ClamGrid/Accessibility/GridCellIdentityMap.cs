namespace ClamGrid.Accessibility;

internal sealed class GridCellIdentityMap(IEqualityComparer<object> rowKeyComparer)
{
    private readonly Dictionary<object, Dictionary<string, int>> rows = [with(rowKeyComparer)];
    private readonly Dictionary<int, GridCellIdentity> cells = [];
    private int nextId;

    public int GetId(object rowKey, string columnKey)
    {
        ArgumentNullException.ThrowIfNull(rowKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(columnKey);
        if (!rows.TryGetValue(rowKey, out var columns))
        {
            columns = [with(StringComparer.Ordinal)];
            rows.Add(rowKey, columns);
        }

        if (!columns.TryGetValue(columnKey, out var id))
        {
            id = checked(++nextId);
            columns.Add(columnKey, id);
            cells.Add(id, new GridCellIdentity(rowKey, columnKey));
        }

        return id;
    }

    public GridCellIdentity? Find(int id) => cells.GetValueOrDefault(id);

    public void Prune(Func<object, bool> containsRow, ISet<string> columnKeys)
    {
        ArgumentNullException.ThrowIfNull(containsRow);
        ArgumentNullException.ThrowIfNull(columnKeys);
        foreach (var (id, cell) in cells.ToArray())
        {
            if (!containsRow(cell.RowKey) || !columnKeys.Contains(cell.ColumnKey))
            {
                cells.Remove(id);
                var columns = rows[cell.RowKey];
                columns.Remove(cell.ColumnKey);
                if (columns.Count == 0)
                {
                    rows.Remove(cell.RowKey);
                }
            }
        }
    }
}
