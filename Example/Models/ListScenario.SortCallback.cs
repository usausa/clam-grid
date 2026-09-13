namespace Example.Models;

public sealed partial record ListScenario
{
    // Sample callback using the stable LINQ sort with one key extraction per row and key
    public IEnumerable<TicketRow> SortRows(IReadOnlyList<TicketRow> rows, IReadOnlyList<GridSortOrder> orders)
    {
        IOrderedEnumerable<TicketRow>? sorted = null;
        foreach (var order in orders)
        {
            var key = order.Key;
            if ((PendingField is not null) && (Columns.Any(column => column.Key == key) || (key == "ProductType")))
            {
                // The pending group stays first even when descending
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
