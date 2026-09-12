namespace ClamGrid;

public sealed class GridDataChangedEventArgs(GridDataChangeKind kind, bool orderChanged, bool selectionChanged) : EventArgs
{
    public GridDataChangeKind Kind { get; } = kind;

    public bool OrderChanged { get; } = orderChanged;

    public bool SelectionChanged { get; } = selectionChanged;
}
