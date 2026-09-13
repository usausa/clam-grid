namespace ClamGrid;

// Body cell being painted; indexes follow the current display order and DefaultColors already include the selection colors
public readonly record struct GridCellColorContext(object Item, int RowIndex, GridColumn Column, int ColumnIndex, object? Value, bool IsSelected, GridColors DefaultColors);
