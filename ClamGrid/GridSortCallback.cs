namespace ClamGrid;

// Sorts a snapshot of the rows by the orders (primary key first) and returns every input row exactly once
public delegate IEnumerable<T> GridSortCallback<T>(IReadOnlyList<T> rows, IReadOnlyList<GridSortOrder> orders)
    where T : class;
