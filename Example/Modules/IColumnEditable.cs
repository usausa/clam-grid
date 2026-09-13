namespace Example.Modules;

// View model contract bound by the list grid part
public interface IColumnEditable
{
    ICommand SelectCommand { get; }

    ICommand ColumnEditCommand { get; }

    // Visibility and order of the columns; the grid writes the normalized value back through the TwoWay binding
    IReadOnlyList<GridColumnOrder>? ColumnOrders { get; set; }

    GridSelectRequest GridSelectRequest { get; }
}
