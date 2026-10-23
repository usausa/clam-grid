namespace ClamGrid;

public readonly record struct GridHit(GridCellType CellType, int RowIndex, int ColumnIndex, string? ColumnKey = null)
{
    public static GridHit None => new(GridCellType.None, -1, -1);
}
