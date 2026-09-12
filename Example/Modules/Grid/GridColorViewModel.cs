namespace Example.Modules.Grid;

public sealed partial class GridColorViewModel : AppViewModelBase
{
    private static readonly Color WarningText = Color.FromArgb("#B91C1C");
    private static readonly Color WarningBackground = Color.FromArgb("#FEE2E2");
    private static readonly Color CompleteText = Color.FromArgb("#15803D");
    private static readonly Color CompleteBackground = Color.FromArgb("#DCFCE7");
    private static readonly Color EmphasisBackground = Color.FromArgb("#4F46E5");

    private bool emphasizeHeader;

    public GridDataView<SampleRow> Rows { get; }

    public GridController Grid { get; }

    [ObservableProperty]
    public partial GridStyle GridStyle { get; set; } = default!;

    [ObservableProperty]
    public partial string Status { get; set; } = default!;

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
        Status = "在庫差異が負の行は赤、廃番は緑。チェック変更で色も更新します。選択中は選択色を優先します。";
    }

    //--------------------------------------------------------------------------------
    // Navigation
    //--------------------------------------------------------------------------------

    protected override Task OnNotifyBackAsync() => Navigator.ForwardAsync(ViewId.GridMenu);

    protected override Task OnNotifyFunction1() => OnNotifyBackAsync();

    protected override Task OnNotifyFunction2()
    {
        GridStyle = CreateStyle(GridStyle.FontSize.Equals(16f) ? 22 : 16);
        Status = $"文字サイズ: {GridStyle.FontSize}";
        return Task.CompletedTask;
    }

    protected override Task OnNotifyFunction3()
    {
        emphasizeHeader = !emphasizeHeader;
        Grid.Invalidate();
        Status = emphasizeHeader ? "顧客名の見出し色を強調しました。" : "見出し色の強調を解除しました。";
        return Task.CompletedTask;
    }

    protected override Task OnNotifyFunction4()
    {
        Rows.ClearSelection();
        Status = "選択を解除しました。";
        return Task.CompletedTask;
    }

    //--------------------------------------------------------------------------------
    // Style
    //--------------------------------------------------------------------------------

    private GridStyle CreateStyle(float fontSize) => new()
    {
        FontSize = fontSize,
        RowHeaderWidth = fontSize > 16 ? 64 : 48,
        CellColors = static context => context.IsSelected ? default : context.Column.Key switch
        {
            "difference" when context.Value is < 0 => new GridColors(WarningText, WarningBackground),
            "discontinued" when context.Value is true => new GridColors(CompleteText, CompleteBackground),
            "name" when ((SampleRow)context.Item).IsDiscontinued => new GridColors(CompleteText),
            _ => default
        },
        RowHeaderColors = static context => !context.IsSelected && ((SampleRow)context.Item).IsDiscontinued ? new GridColors(CompleteText, CompleteBackground) : default,
        ColumnHeaderColors = context => emphasizeHeader && (context.Column.Key == "name") ? new GridColors(Colors.White, EmphasisBackground) : default
    };
}
