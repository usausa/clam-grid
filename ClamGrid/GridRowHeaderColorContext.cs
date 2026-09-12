namespace ClamGrid;

/// <summary>行見出し。RowIndexは現在の表示順で、DefaultColorsには選択色も反映済み。</summary>
public readonly record struct GridRowHeaderColorContext(object Item, int RowIndex, bool IsSelected, GridColors DefaultColors);
