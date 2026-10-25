namespace Example.Modules.Grid;

using ClamChart;

using SkiaSharp.Views.Maui;

public sealed partial class GridSparklineViewModel : AppViewModelBase
{
    private const int FrameSamples = 120;

    private static readonly SKColor TrendColor = new(0x1E, 0x88, 0xE5);

    private static readonly ChartDrawingThreshold[] AchievementThresholds =
    [
        new(0, new SKColor(0xE5, 0x39, 0x35)),
        new(80, new SKColor(0xFB, 0x8C, 0x00)),
        new(100, new SKColor(0x43, 0xA0, 0x47))
    ];

    private readonly SalesRow[] sales;

    private readonly double[] frameTimes = new double[FrameSamples];

    private readonly double[] sortedTimes = new double[FrameSamples];

    private int frameCount;

    private bool statisticsPending;

    private GridFrameEventArgs? latestFrame;

    public GridDataView<SalesRow> Rows { get; }

    public GridController Grid { get; }

    [ObservableProperty]
    public partial GridStyle GridStyle { get; set; } = CreateStyle(16);

    [ObservableProperty]
    public partial bool IsSettingOpen { get; set; }

    [ObservableProperty]
    public partial ChartSparklineKind TrendKind { get; set; } = ChartSparklineKind.Line;

    [ObservableProperty]
    public partial string FontText { get; set; } = "文字 16";

    public IObserveCommand SelectKindCommand { get; }

    public IObserveCommand ToggleFontCommand { get; }

    [ObservableProperty]
    public partial string Statistics { get; set; } = String.Empty;

    //--------------------------------------------------------------------------------
    // Constructor
    //--------------------------------------------------------------------------------

    public GridSparklineViewModel()
    {
        sales = Enumerable.Range(0, 60).Select(static x => new SalesRow(x)).ToArray();
        Rows = new GridDataView<SalesRow>(sales, static x => x.Id);
        Disposables.Add(Rows);
        Rows.RegisterSort("name", static x => x.Id);
        Rows.RegisterSort("today", static x => x.Today);
        Rows.RegisterSort("change", static x => x.Change);
        Rows.RegisterSort("achievement", static x => x.Achievement);

        Grid = new GridController(CreateColumns());
        Grid.FrameRendered += OnFrameRendered;

        SelectKindCommand = MakeDelegateCommand<ChartSparklineKind>(SelectKind);
        ToggleFontCommand = MakeDelegateCommand(ToggleFont);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            Grid.FrameRendered -= OnFrameRendered;
        }

        base.Dispose(disposing);
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

    protected override Task OnNotifyFunction3()
    {
        foreach (var row in sales)
        {
            row.Advance();
        }

        Rows.Refresh();
        return Task.CompletedTask;
    }

    //--------------------------------------------------------------------------------
    // Operation
    //--------------------------------------------------------------------------------

    private void SelectKind(ChartSparklineKind value)
    {
        TrendKind = value;
        Grid.Invalidate();
    }

    private void ToggleFont()
    {
        GridStyle = CreateStyle(GridStyle.FontSize.Equals(16d) ? 22 : 16);
        FontText = $"文字 {GridStyle.FontSize}";
    }

    //--------------------------------------------------------------------------------
    // Column
    //--------------------------------------------------------------------------------

    private GridColumn[] CreateColumns() =>
    [
        new GridColumn("name", "店舗", new GridValueAccessor<SalesRow, string>(static x => x.Name)) { Width = GridColumnWidth.Auto },
        new GridColumn("today", "本日", new GridValueAccessor<SalesRow, double>(static x => x.Today)) { Width = GridColumnWidth.Auto, Alignment = TextAlignment.End, Format = "N0" },
        new GridColumn("change", "前日比", new GridValueAccessor<SalesRow, double>(static x => x.Change)) { Width = GridColumnWidth.Auto, Alignment = TextAlignment.End, Format = "+0.0%;-0.0%;0.0%" },
        new GridColumn("trend", "推移", new GridValueAccessor<SalesRow, double[]>(static x => x.GetSales())) { Width = GridColumnWidth.Absolute(140), AllowSorting = false, CellPainter = PaintTrend },
        new GridColumn("changes", "増減", new GridValueAccessor<SalesRow, double[]>(static x => x.GetChanges())) { Width = GridColumnWidth.Absolute(120), AllowSorting = false, CellPainter = PaintChanges },
        new GridColumn("achievement", "達成率", new GridValueAccessor<SalesRow, double>(static x => x.Achievement)) { Width = GridColumnWidth.Auto, Alignment = TextAlignment.End, Format = "0.0%" },
        new GridColumn("progress", "進捗", new GridValueAccessor<SalesRow, double>(static x => x.Achievement)) { Width = GridColumnWidth.Absolute(120), SortKey = "achievement", CellPainter = PaintProgress }
    ];

    private static SKRect Inset(SKRect bounds) => SKRect.Inflate(bounds, -8, -6);

    private void PaintTrend(SKCanvas canvas, GridCellPaintContext context)
    {
        if (context.Value is double[] values)
        {
            var color = context.IsSelected ? context.Colors.TextColor!.ToSKColor() : TrendColor;
            ChartDrawing.DrawSparkline(canvas, Inset(context.Bounds), values, new SparklineDrawOptions { Kind = TrendKind, Color = color, Markers = ChartSparklineMarkers.Last });
        }
    }

    private static void PaintChanges(SKCanvas canvas, GridCellPaintContext context)
    {
        if (context.Value is double[] values)
        {
            ChartDrawing.DrawSparkline(canvas, Inset(context.Bounds), values, new SparklineDrawOptions { Kind = ChartSparklineKind.WinLoss });
        }
    }

    private static void PaintProgress(SKCanvas canvas, GridCellPaintContext context)
    {
        if (context.Value is double ratio)
        {
            ReadOnlySpan<double> values = [ratio * 100];
            ChartDrawing.DrawBarGauge(canvas, Inset(context.Bounds), values, new BarGaugeDrawOptions { Thresholds = AchievementThresholds });
        }
    }

    //--------------------------------------------------------------------------------
    // Style
    //--------------------------------------------------------------------------------

    private static GridStyle CreateStyle(double fontSize) => new()
    {
        FontSize = fontSize,
        RowHeaderWidth = fontSize > 16 ? 64 : 48
    };

    //--------------------------------------------------------------------------------
    // Statistics
    //--------------------------------------------------------------------------------

    private void OnFrameRendered(object? sender, GridFrameEventArgs e)
    {
        frameTimes[frameCount % FrameSamples] = e.Milliseconds;
        frameCount++;
        latestFrame = e;
        if (!statisticsPending)
        {
            statisticsPending = true;
            Dispatcher.GetForCurrentThread()?.DispatchDelayed(TimeSpan.FromMilliseconds(250), UpdateStatistics);
        }
    }

    private void UpdateStatistics()
    {
        statisticsPending = false;
        if (IsDisposed || (latestFrame is not { } frame))
        {
            return;
        }

        var count = Math.Min(frameCount, FrameSamples);
        Array.Copy(frameTimes, sortedTimes, count);
        Array.Sort(sortedTimes, 0, count);
        var p95 = sortedTimes[Math.Min(count - 1, (int)Math.Ceiling(count * 0.95) - 1)];
        Statistics = $"描画 {frame.RenderedCells}セル {frame.Milliseconds:F2}ms / 直近 {count}フレームの p95 {p95:F2}ms";
    }
}
