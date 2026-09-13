namespace ClamGrid;

// Column header being painted; SortPriority is 0 for the primary key, 1 and up for secondary keys, -1 when unsorted, and DefaultColors include the sort colors
public readonly record struct GridColumnHeaderColorContext(GridColumn Column, int ColumnIndex, GridSortOrder? SortOrder, int SortPriority, GridColors DefaultColors);
