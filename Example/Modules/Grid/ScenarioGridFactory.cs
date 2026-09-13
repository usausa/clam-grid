namespace Example.Modules.Grid;

internal static class ScenarioGridFactory
{
    public static GridStyle CreateStyle(ListScenario scenario) => new()
    {
        ShowRowHeaders = false,
        FontFamily = "monospace",
        TextColor = Colors.Black,
        HeaderTextColor = Colors.White,
        AscendingHeaderBackground = Color.FromArgb("#1565C0"),
        DescendingHeaderBackground = Color.FromArgb("#EF6C00"),
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
            HeaderBackground = Color.FromArgb(column.GreenHeader ? "#2E7D32" : "#616161"),
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
