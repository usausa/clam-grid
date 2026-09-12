namespace ClamGrid;

/// <summary>列見出し。SortPriorityは主キーが0、副キーが1以降、対象外は-1。DefaultColorsには主キーのソート色も反映済み。</summary>
public readonly record struct GridColumnHeaderColorContext(GridColumn Column, int ColumnIndex, GridSortOrder? SortOrder, int SortPriority, GridColors DefaultColors);
