namespace Example.Modules.Grid;

[View(ViewId.GridColor)]
public sealed partial class GridColorView
{
    public GridColorView()
    {
        InitializeComponent();
        DataGrid.CellTapped += OnCellTapped;
    }

    private void OnCellTapped(object? sender, GridCellEventArgs e)
    {
        if (BindingContext is GridColorViewModel viewModel)
        {
            viewModel.ShowCellBounds(e.Hit.ColumnKey, e.Hit.ColumnKey is { } key ? DataGrid.GetCellBounds(e.Hit.RowIndex, key) : null);
        }
    }
}
