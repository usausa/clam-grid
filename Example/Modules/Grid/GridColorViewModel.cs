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
    public partial string FontText { get; set; } = "文字 16";

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
    public partial double FontSize { get; set; }

    [ObservableProperty]
    public partial double RowHeaderWidth { get; set; }

    [ObservableProperty]
    public partial bool HorizontalLinesVisible { get; set; } = true;

    [ObservableProperty]
    public partial bool VerticalLinesVisible { get; set; } = true;

    public Func<GridCellColorContext, GridColors> CellColors { get; } = static context => context.IsSelected ? default : context.Column.Key switch
    {
        "difference" when context.Value is < 0 => new GridColors(WarningText, WarningBackground),
        "discontinued" when context.Value is true => new GridColors(CompleteText, CompleteBackground),
        "name" when ((SampleRow)context.Item).IsDiscontinued => new GridColors(CompleteText),
        _ => default
    };

    public Func<GridRowHeaderColorContext, GridColors> RowHeaderColors { get; } = static context =>
        !context.IsSelected && ((SampleRow)context.Item).IsDiscontinued ? new GridColors(CompleteText, CompleteBackground) : default;

    public Func<GridColumnHeaderColorContext, GridColors> ColumnHeaderColors { get; }

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
        ColumnHeaderColors = context => EmphasizeHeader && (context.Column.Key == "name") ? new GridColors(Colors.White, EmphasisBackground) : default;
        ApplyFont(16);
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
        ApplyFont(FontSize.Equals(16d) ? 22 : 16);
        Status = FontText;
    }

    public void ShowCellBounds(string? columnKey, Rect? bounds) =>
        Status = bounds is { } rect ? $"{columnKey} {rect.X:F0},{rect.Y:F0} {rect.Width:F0}×{rect.Height:F0}" : "セルの外";

    private void SelectLine(int index)
    {
        LineIndex = index;
        HorizontalLinesVisible = index != 2;
        VerticalLinesVisible = index == 0;
        Status = index switch { 1 => "罫線 横だけ", 2 => "罫線 なし", _ => "罫線 格子" };
    }

    //--------------------------------------------------------------------------------
    // Style
    //--------------------------------------------------------------------------------

    private void ApplyFont(double size)
    {
        FontSize = size;
        RowHeaderWidth = size > 16 ? 64 : 48;
        FontText = $"文字 {size}";
    }
}
