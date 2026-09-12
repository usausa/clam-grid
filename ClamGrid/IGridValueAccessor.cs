namespace ClamGrid;

public interface IGridValueAccessor
{
    Type ItemType { get; }

    Type ValueType { get; }

    bool CanWrite { get; }

    object? GetValue(object item);

    void SetValue(object item, object? value);
}
