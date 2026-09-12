namespace ClamGrid;

public sealed class GridSortRequestedEventArgs(IReadOnlyList<GridSortOrder> orders) : EventArgs
{
    public IReadOnlyList<GridSortOrder> Orders { get; } = orders;

    public bool Cancel { get; set; }
}
