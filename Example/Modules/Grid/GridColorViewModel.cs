namespace Example.Modules.Grid;

public sealed partial class GridColorViewModel : AppViewModelBase
{
    private static readonly Color WarningText = Color.FromArgb("#B91C1C");
    private static readonly Color WarningBackground = Color.FromArgb("#FEE2E2");
    private static readonly Color CompleteText = Color.FromArgb("#15803D");
    private static readonly Color CompleteBackground = Color.FromArgb("#DCFCE7");
    private static readonly Color EmphasisBackground = Color.FromArgb("#4F46E5");

    public GridDataView<SampleRow> Rows { get; }

    public GridController Grid { get; }

    [ObservableProperty]
    public partial bool IsSettingOpen { get; set; }

    [ObservableProperty]
    public partial string FontText { get; set; }

    public bool EmphasizeHeader
    {
        get;
        set
        {
            if (field != value)
            {
                field = value;
                RaisePropertyChanged(nameof(EmphasizeHeader));
                Grid.Invalidate();
                Status = value ? "見出し色 強調" : "見出し色 既定";
            }
        }
    }

    [ObservableProperty]
    public partial int LineIndex { get; set; }

    public IObserveCommand ToggleFontCommand { get; }

    public IObserveCommand SelectLineCommand { get; }

    [ObservableProperty]
    public partial GridStyle GridStyle { get; set; }

    [ObservableProperty]
    public partial string Status { get; set; }

    //--------------------------------------------------------------------------------
    // Constructor
    //--------------------------------------------------------------------------------

    public GridColorViewModel()
    {
        Rows = new GridDataView<SampleRow>(SampleData.CreateRows(1000).ToArray(), static x => x.Id);
        Disposables.Add(Rows);
        SampleColumns.RegisterSorts(Rows);

        Grid = new GridController(SampleColumns.CreateColor());
        GridStyle = CreateStyle(16);
        FontText = $"文字 {GridStyle.FontSize}";
        ToggleFontCommand = MakeDelegateCommand(ToggleFont);
        SelectLineCommand = MakeDelegateCommand<int>(SelectLine);
        Status = $"{Rows.Count:N0}行";
    }

    //--------------------------------------------------------------------------------
    // Navigation
    //--------------------------------------------------------------------------------

    protected override Task OnNotifyBackAsync()
    {
        if (IsSettingOpen)
        {
            IsSettingOpen = false;
            return Task.CompletedTask;
        }

        return Navigator.ForwardAsync(ViewId.GridMenu);
    }

    protected override Task OnNotifyFunction1() => OnNotifyBackAsync();

    protected override Task OnNotifyFunction2()
    {
        IsSettingOpen = !IsSettingOpen;
        return Task.CompletedTask;
    }

    protected override Task OnNotifyFunction4()
    {
        Rows.ClearSelection();
        Status = "選択なし";
        return Task.CompletedTask;
    }

    //--------------------------------------------------------------------------------
    // Operation
    //--------------------------------------------------------------------------------

    private void ToggleFont()
    {
        GridStyle = CreateStyle(GridStyle.FontSize.Equals(16d) ? 22 : 16);
        FontText = $"文字 {GridStyle.FontSize}";
        Status = $"文字 {GridStyle.FontSize}";
    }

    public void ShowCellBounds(string? columnKey, Rect? bounds) =>
        Status = bounds is { } rect ? $"{columnKey} {rect.X:F0},{rect.Y:F0} {rect.Width:F0}×{rect.Height:F0}" : "セルの外";

    private void SelectLine(int index)
    {
        LineIndex = index;
        GridStyle = CreateStyle(GridStyle.FontSize);
        Status = index switch { 1 => "罫線 横だけ", 2 => "罫線 なし", _ => "罫線 格子" };
    }

    //--------------------------------------------------------------------------------
    // Style
    //--------------------------------------------------------------------------------

    private GridStyle CreateStyle(double fontSize) => new()
    {
        FontSize = fontSize,
        RowHeaderWidth = fontSize > 16 ? 64 : 48,
        HorizontalLinesVisible = LineIndex != 2,
        VerticalLinesVisible = LineIndex == 0,
        CellColors = static context => context.IsSelected ? default : context.Column.Key switch
        {
            "difference" when context.Value is < 0 => new GridColors(WarningText, WarningBackground),
            "discontinued" when context.Value is true => new GridColors(CompleteText, CompleteBackground),
            "name" when ((SampleRow)context.Item).IsDiscontinued => new GridColors(CompleteText),
            _ => default
        },
        RowHeaderColors = static context => !context.IsSelected && ((SampleRow)context.Item).IsDiscontinued ? new GridColors(CompleteText, CompleteBackground) : default,
        ColumnHeaderColors = context => EmphasizeHeader && (context.Column.Key == "name") ? new GridColors(Colors.White, EmphasisBackground) : default
    };
}
