namespace Example.Modules.Grid;

public sealed partial class GridColumnsViewModel : AppViewModelBase
{
    private GridColumnEditSession session = default!;

    public GridController Grid { get; }

    [ObservableProperty]
    public partial GridDataView<GridColumnOption>? Rows { get; set; }

    [ObservableProperty]
    public partial IGridRowMover? RowMover { get; set; }

    [ObservableProperty]
    public partial GridStyle GridStyle { get; set; } = default!;

    [ObservableProperty]
    public partial string Status { get; set; } = default!;

    //--------------------------------------------------------------------------------
    // Constructor
    //--------------------------------------------------------------------------------

    public GridColumnsViewModel()
    {
        Grid = new GridController(SampleColumns.CreateColumnSettings());
        Grid.CellValueChanged += OnCellValueChanged;
        Grid.RowMoved += OnRowMoved;
        GridStyle = new GridStyle { RowHeaderWidth = 56, RowHeight = 46 };
        Status = "チェックで表示を切替。左の三本線をドラッグして順序を変更できます。変更は「Apply」で反映します。";
    }

    //--------------------------------------------------------------------------------
    // Navigation
    //--------------------------------------------------------------------------------

    public override Task OnNavigatingToAsync(INavigationContext context)
    {
        session = context.Parameter.GetColumnEditSession();
        var rows = new GridDataView<GridColumnOption>(session.Columns, static x => x.Key) { SelectionMode = GridSelectionMode.None };
        Disposables.Add(rows);
        Rows = rows;
        RowMover = new GridRowMover<GridColumnOption>(session.Columns, rows);
        return Task.CompletedTask;
    }

    protected override Task OnNotifyBackAsync() => Navigator.PopAsync();

    protected override Task OnNotifyFunction1() => OnNotifyBackAsync();

    protected override Task OnNotifyFunction4() => Navigator.PopAsync(Parameters.MakeColumnOrders(session.Export()));

    //--------------------------------------------------------------------------------
    // Event
    //--------------------------------------------------------------------------------

    private void OnCellValueChanged(object? sender, GridCellValueEventArgs e) =>
        Status = $"表示 {session.Columns.Count(static x => x.IsVisible)} / {session.Columns.Count}列";

    private void OnRowMoved(object? sender, GridRowMoveEventArgs e) =>
        Status = $"{e.OldIndex + 1}行目 → {e.NewIndex + 1}行目へ移動しました。";
}
