namespace ClamGrid;

public sealed class GridSortFailedEventArgs(Exception error) : EventArgs
{
    public Exception Error { get; } = error;
}
