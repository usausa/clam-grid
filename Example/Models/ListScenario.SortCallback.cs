namespace Example.Models;

public sealed partial record ListScenario
{
    // LINQの安定ソートを使い、比較値の取得をキーごと・行ごとの1回に抑える利用側実装例。
    public IEnumerable<TicketRow> SortRows(IReadOnlyList<TicketRow> rows, IReadOnlyList<GridSortOrder> orders)
    {
        IOrderedEnumerable<TicketRow>? sorted = null;
        foreach (var order in orders)
        {
            var key = order.Key;
            if ((PendingField is not null) && (Columns.Any(column => column.Key == key) || (key == "ProductType")))
            {
                // 未処理グループは降順でも先頭に固定する。
                sorted = Append(rows, sorted, row => !IsPending(row), false, Comparer<bool>.Default);
            }

            sorted = key switch
            {
                "IsStarted" => Append(rows, sorted, static row => row.IsStarted, order.Descending, Comparer<bool>.Default),
                "IsCompleted" => Append(rows, sorted, static row => row.IsCompleted, order.Descending, Comparer<bool>.Default),
                "SortOrder" => Append(rows, sorted, static row => row.Id, order.Descending, Comparer<int>.Default),
                "CompanyName" => Append(rows, sorted, static row => row.CompanySortKey, order.Descending, StringComparer.CurrentCulture),
                _ => Append(rows, sorted, row => row.GetText(key), order.Descending, StringComparer.CurrentCulture)
            };
        }

        return sorted is null ? rows : sorted;
    }

    private static IOrderedEnumerable<TicketRow> Append<TValue>(IReadOnlyList<TicketRow> rows, IOrderedEnumerable<TicketRow>? sorted, Func<TicketRow, TValue> selector, bool descending, IComparer<TValue> comparer) => sorted is null
        ? descending ? rows.OrderByDescending(selector, comparer) : rows.OrderBy(selector, comparer)
        : descending ? sorted.ThenByDescending(selector, comparer) : sorted.ThenBy(selector, comparer);
}
