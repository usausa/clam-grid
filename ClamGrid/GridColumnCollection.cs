namespace ClamGrid;

using System.Collections.ObjectModel;

public sealed class GridColumnCollection : Collection<GridColumn>
{
    public event EventHandler? Changed;

    public void ReplaceAll(IEnumerable<GridColumn> columns)
    {
        ArgumentNullException.ThrowIfNull(columns);
        var replacement = columns.ToArray();
        var keys = new HashSet<string>(StringComparer.Ordinal);
        foreach (var column in replacement)
        {
            ArgumentNullException.ThrowIfNull(column);
            ArgumentException.ThrowIfNullOrWhiteSpace(column.Key, nameof(columns));
            if (!keys.Add(column.Key))
            {
                throw new ArgumentException("Column keys must be unique.", nameof(columns));
            }
        }

        Items.Clear();
        foreach (var column in replacement)
        {
            Items.Add(column);
        }

        Changed?.Invoke(this, EventArgs.Empty);
    }

    protected override void InsertItem(int index, GridColumn item)
    {
        Validate(item, -1);
        base.InsertItem(index, item);
        Changed?.Invoke(this, EventArgs.Empty);
    }

    protected override void SetItem(int index, GridColumn item)
    {
        Validate(item, index);
        base.SetItem(index, item);
        Changed?.Invoke(this, EventArgs.Empty);
    }

    protected override void RemoveItem(int index)
    {
        base.RemoveItem(index);
        Changed?.Invoke(this, EventArgs.Empty);
    }

    protected override void ClearItems()
    {
        base.ClearItems();
        Changed?.Invoke(this, EventArgs.Empty);
    }

    private void Validate(GridColumn item, int replacedIndex)
    {
        ArgumentNullException.ThrowIfNull(item);
        ArgumentException.ThrowIfNullOrWhiteSpace(item.Key, nameof(item));
        for (var i = 0; i < Count; i++)
        {
            if ((i != replacedIndex) && String.Equals(this[i].Key, item.Key, StringComparison.Ordinal))
            {
                throw new ArgumentException("Column keys must be unique.", nameof(item));
            }
        }
    }
}
