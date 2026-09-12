namespace ClamGrid;

/// <summary>描画中の本文セル。インデックスは現在の表示順で、DefaultColorsには選択色も反映済み。</summary>
public readonly record struct GridCellColorContext(object Item, int RowIndex, GridColumn Column, int ColumnIndex, object? Value, bool IsSelected, GridColors DefaultColors);
