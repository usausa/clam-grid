namespace Example.Modules.Grid;

internal static class ScenarioGridFactory
{
    public static GridStyle CreateStyle(ListScenario scenario) => new()
    {
        ShowRowHeaders = false,
        FontFamily = "monospace",
        TextColor = Colors.Black,
        HeaderTextColor = Colors.Black,
        AscendingHeaderBackground = Color.FromArgb("#90CAF9"),
        DescendingHeaderBackground = Color.FromArgb("#FFCC80"),
        GridLineColor = Colors.Gray,
        SelectedBackground = Color.FromArgb("#448AFF"),
        SelectedTextColor = Colors.White,
        RowBackground = item => item is TicketRow row ? Color.FromArgb(scenario.Background(row)) : null
    };

    public static GridColumn[] CreateColumns(ListScenario scenario) =>
        scenario.Columns.Select(static column => new GridColumn(column.Key, column.Header, new GridValueAccessor<TicketRow, string>(row => row.GetText(column.Key)))
        {
            Width = GridColumnWidth.Absolute(column.Width),
            Alignment = column.AlignEnd ? TextAlignment.End : TextAlignment.Start,
            HeaderBackground = Color.FromArgb(column.GreenHeader ? "#4CAF50" : "#9E9E9E"),
            AllowSorting = column.AllowSorting
        }).ToArray();

    public static void Configure(ClamGridView grid, ListScenario scenario, GridDataView<TicketRow> data, IEnumerable<GridColumnOrder>? orders = null)
    {
        grid.GridStyle = CreateStyle(scenario);
        grid.ConfigureColumns(CreateColumns(scenario), orders);
        grid.ItemsSource = data;
        grid.SelectionMode = scenario.SingleSelection ? GridSelectionMode.SingleToggle : GridSelectionMode.MultipleToggle;
        grid.SelectAllCommand = new Command<bool>(select => scenario.SelectAll(data, select));
    }
}
