namespace Example.Modules;

// View model contract bound by the list grid part
public interface IColumnEditable
{
    ICommand SelectCommand { get; }

    ICommand ColumnEditCommand { get; }

    // Visibility and order of the columns; the grid writes the normalized value back through the TwoWay binding
    IReadOnlyList<GridColumnState>? ColumnStates { get; set; }

    // Sort keys and directions; the grid writes the applied value back through the TwoWay binding
    IReadOnlyList<GridSortOrder>? SortOrders { get; set; }

    // Selected row keys; the grid writes the applied value back through the TwoWay binding
    IReadOnlyList<object>? SelectedKeys { get; set; }

    GridSelectRequest GridSelectRequest { get; }
}
