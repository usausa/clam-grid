namespace ClamGrid;

// Row header being painted; RowIndex follows the current display order
public readonly record struct GridRowHeaderTextContext(object Item, int RowIndex, bool IsSelected);
