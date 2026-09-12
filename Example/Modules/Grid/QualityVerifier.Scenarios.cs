namespace Example.Modules.Grid;

using Example.Components;

public sealed partial class QualityVerifier
{
    private async Task VerifyScenariosAsync()
    {
        foreach (var scenario in ListScenario.All)
        {
            report($"一覧条件: {scenario.Title}");
            grid.ItemsSource = null;
            data?.Dispose();
            data = scenario.CreateData(120);
            var frame = await NextFrameAsync(() => ScenarioGridFactory.Configure(grid, scenario, data)).ConfigureAwait(true);
            Check($"{scenario.Key}: columns and visible painting", grid.Columns.Count == scenario.Columns.Count && frame.RenderedCells > 0 && !grid.GridStyle.ShowRowHeaders);
            Check($"{scenario.Key}: source keys and values", grid.Columns.All(column => column.ValueAccessor.GetValue(data[0]) is string));
            Check($"{scenario.Key}: header colors independent of sorting", grid.Columns.Zip(scenario.Columns).All(pair => pair.First.AllowSorting == pair.Second.AllowSorting && Equals(pair.First.HeaderBackground, Color.FromArgb(pair.Second.GreenHeader ? "#4CAF50" : "#9E9E9E"))));
            scenario.SelectAll(data, true);
            Check($"{scenario.Key}: bulk selection", data.SelectedCount == (scenario.SingleSelection ? 0 : Enumerable.Range(0, 120).Count(id => scenario.IsPending(new TicketRow(id)))));
            scenario.SelectAll(data, false);
            Check($"{scenario.Key}: clear selection", data.SelectedCount == 0);
            data.TryToggleSelection(0, out _);
            data.TryToggleSelection(1, out _);
            Check($"{scenario.Key}: single or multiple toggle", data.SelectedCount == (scenario.SingleSelection ? 1 : 2));
            data.ClearSelection();
            await NextFrameAsync(() => data.RestoreSortOrders([new("CaseNo", true), new("DeptCode")])).ConfigureAwait(true);
            Check($"{scenario.Key}: external selection after sort", grid.TryToggleSelectionByKey(41, out var selected) && selected && ReferenceEquals(grid.SelectedItems.Single(), data[data.IndexOfKey(41)]));
            var saved = data.SaveSortOrders();
            var order = data.Select(static row => row.Id).ToArray();
            data.SortBy("DeptCode");
            data.RestoreSortOrders(saved);
            Check($"{scenario.Key}: session sort roundtrip", data.Select(static row => row.Id).SequenceEqual(order) && data.SelectedCount == 1);
            var edited = grid.ColumnOrders.Select((order, index) => new GridColumnOrder(order.Key, index != 0)).Reverse().ToArray();
            var json = ColumnSettingsCodec.Write(edited);
            await NextFrameAsync(() => grid.ApplyColumnOrders(ColumnSettingsCodec.Read(json))).ConfigureAwait(true);
            Check($"{scenario.Key}: legacy columns roundtrip", grid.Columns.Count == scenario.Columns.Count - 1 && grid.Columns[0].Key == scenario.Columns[^1].Key && data.SelectedCount == 1);
            grid.ApplyColumnOrders(null);
            Check($"{scenario.Key}: default columns restored", grid.Columns.Count == scenario.Columns.Count);
            Check($"{scenario.Key}: three state colors", Enumerable.Range(0, 3).Select(id => grid.GridStyle.RowBackground!(new TicketRow(id))).Distinct().Count() == 3 && grid.GridStyle.SelectedBackground.Equals(Color.FromArgb("#448AFF")) && grid.GridStyle.SelectedTextColor.Equals(Colors.White));
            if (scenario.PendingField is not null)
            {
                data.RestoreSortOrders([new("CaseNo", true)]);
                Check($"{scenario.Key}: pending group stays first descending", data.TakeWhile(scenario.IsPending).Count() == data.Count(scenario.IsPending));
            }
        }
    }
}
