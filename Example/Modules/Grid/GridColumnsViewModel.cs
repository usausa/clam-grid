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
    public partial string Status { get; set; }

    //--------------------------------------------------------------------------------
    // Constructor
    //--------------------------------------------------------------------------------

    public GridColumnsViewModel()
    {
        Grid = new GridController(SampleColumns.CreateColumnSettings());
        Grid.CellTapped += OnCellTapped;
        Grid.CellValueChanged += OnCellValueChanged;
        Grid.RowMoved += OnRowMoved;
        Status = String.Empty;
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
        UpdateStatus();
        return Task.CompletedTask;
    }

    protected override Task OnNotifyBackAsync() => Navigator.PopAsync();

    protected override Task OnNotifyFunction1() => OnNotifyBackAsync();

    protected override Task OnNotifyFunction3()
    {
        foreach (var column in session.Columns)
        {
            column.Width = null;
        }

        Status = "全列の幅 既定";
        return Task.CompletedTask;
    }

    protected override Task OnNotifyFunction4() => Navigator.PopAsync(Parameters.MakeColumnStates(session.Export()));

    //--------------------------------------------------------------------------------
    // Event
    //--------------------------------------------------------------------------------

    private void OnCellTapped(object? sender, GridCellEventArgs e)
    {
        if ((e.ColumnKey == "width") && (e.Item is GridColumnOption { Width: not null } column))
        {
            column.Width = null;
            Status = $"{column.Header.ReplaceLineEndings(" ")} の幅 既定";
        }
    }

    private void OnCellValueChanged(object? sender, GridCellValueEventArgs e) => UpdateStatus();

    private void OnRowMoved(object? sender, GridRowMoveEventArgs e) =>
        Status = $"移動 {e.OldIndex + 1}行目 → {e.NewIndex + 1}行目";

    private void UpdateStatus() =>
        Status = $"表示 {session.Columns.Count(static x => x.IsVisible)} / {session.Columns.Count}列";
}
