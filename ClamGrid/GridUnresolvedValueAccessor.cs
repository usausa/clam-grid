namespace ClamGrid;

// Default accessor of an unresolved column, painted as an empty read-only cell
internal sealed class GridUnresolvedValueAccessor : IGridValueAccessor
{
    public static GridUnresolvedValueAccessor Instance { get; } = new();

    public Type ItemType => typeof(object);

    public Type ValueType => typeof(object);

    public bool CanWrite => false;

    private GridUnresolvedValueAccessor()
    {
    }

    public object? GetValue(object item) => null;

    public void SetValue(object item, object? value) => throw new InvalidOperationException("The value accessor is not resolved.");
}
