namespace ClamGrid;

// Row header being painted; RowIndex follows the current display order and DefaultColors already include the selection colors
public readonly record struct GridRowHeaderColorContext(object Item, int RowIndex, bool IsSelected, GridColors DefaultColors);
