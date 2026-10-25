namespace ClamGrid;

using SkiaSharp;

public readonly record struct GridCellPaintContext(SKRect Bounds, object Item, int RowIndex, GridColumn Column, int ColumnIndex, object? Value, bool IsSelected, GridColors Colors);
