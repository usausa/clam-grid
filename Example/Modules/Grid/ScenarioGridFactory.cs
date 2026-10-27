namespace Example.Modules.Grid;

internal static class ScenarioGridFactory
{
    public static Func<object, Color?> CreateRowBackground(ListScenario scenario) =>
        item => item is TicketRow row ? Color.FromArgb(scenario.Background(row)) : null;

    public static void ApplyStyle(ClamGridView grid, ListScenario scenario)
    {
        grid.Style = (Style)Application.Current!.Resources["ScenarioGrid"];
        grid.RowBackground = CreateRowBackground(scenario);
    }

    public static GridColumn[] CreateColumns(ListScenario scenario) =>
        scenario.Columns.Select(static column => new GridColumn(column.Key, column.Header, new GridValueAccessor<TicketRow, string>(row => row.GetText(column.Key)))
        {
            Width = GridColumnWidth.Absolute(column.Width),
            Alignment = column.AlignEnd ? TextAlignment.End : TextAlignment.Start,
            HeaderBackground = Color.FromArgb(column.GreenHeader ? "#2E7D32" : "#616161"),
            AllowSorting = column.AllowSorting
        }).ToArray();

    public static void Configure(ClamGridView grid, ListScenario scenario, GridDataView<TicketRow> data, IEnumerable<GridColumnState>? orders = null)
    {
        ApplyStyle(grid, scenario);
        grid.ColumnStates = orders?.ToArray();
        grid.ColumnDefinitions.ReplaceAll(CreateColumns(scenario));
        grid.ItemsSource = data;
        grid.SelectionMode = scenario.SingleSelection ? GridSelectionMode.SingleToggle : GridSelectionMode.MultipleToggle;
        grid.SelectAllCommand = new Command<bool>(select => scenario.SelectAll(data, select));
    }
}
