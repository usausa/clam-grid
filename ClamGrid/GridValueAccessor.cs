namespace ClamGrid;

public sealed class GridValueAccessor<TItem, TValue> : IGridValueAccessor
    where TItem : class
{
    private readonly Func<TItem, TValue> getter;

    private readonly Action<TItem, TValue>? setter;

    public Type ItemType => typeof(TItem);

    public Type ValueType => typeof(TValue);

    public bool CanWrite => setter is not null;

    public GridValueAccessor(Func<TItem, TValue> getter, Action<TItem, TValue>? setter = null)
    {
        ArgumentNullException.ThrowIfNull(getter);
        this.getter = getter;
        this.setter = setter;
    }

    public object? GetValue(object item) => getter(RequireItem(item));

    public void SetValue(object item, object? value)
    {
        if (setter is null)
        {
            throw new InvalidOperationException("The column is read-only.");
        }

        var target = RequireItem(item);
        if (value is TValue typedValue)
        {
            setter(target, typedValue);
        }
        else if ((value is null) && (default(TValue) is null))
        {
            setter(target, default!);
        }
        else
        {
            throw new ArgumentException("The value does not match the column value type.", nameof(value));
        }
    }

    private static TItem RequireItem(object item)
    {
        ArgumentNullException.ThrowIfNull(item);
        if (item is not TItem target)
        {
            throw new ArgumentException("The item does not match the column item type.", nameof(item));
        }

        return target;
    }
}
