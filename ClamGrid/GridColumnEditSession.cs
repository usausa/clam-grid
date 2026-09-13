namespace ClamGrid;

using System.Collections.ObjectModel;

using ClamGrid.Columns;

public sealed class GridColumnEditSession
{
    public ObservableCollection<GridColumnOption> Columns { get; }

    public GridColumnEditSession(IEnumerable<GridColumnOption> columns)
    {
        ArgumentNullException.ThrowIfNull(columns);
        var copy = columns.Select(static column => new GridColumnOption(column.Key, column.Header, column.IsVisible)).ToArray();
        GridColumnSettings.Normalize(copy.Select(static column => column.Key), null);
        Columns = [with(copy)];
    }

    // 列定義と保存済みの表示・順序から編集用のコピーを作る。未知のキーは除外し、定義にだけある列は非表示として末尾に補う。
    public static GridColumnEditSession Create(IEnumerable<GridColumn> columns, IEnumerable<GridColumnOrder>? orders = null)
    {
        ArgumentNullException.ThrowIfNull(columns);
        var headers = columns.ToDictionary(static column => column.Key, static column => column.Header, StringComparer.Ordinal);
        return new GridColumnEditSession(GridColumnSettings.Normalize(headers.Keys, orders).Select(order => new GridColumnOption(order.Key, headers[order.Key], order.IsVisible)));
    }

    public GridColumnOrder[] Export() => Columns.Select(static column => new GridColumnOrder(column.Key, column.IsVisible)).ToArray();
}
