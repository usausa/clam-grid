namespace Example.Modules.Grid;

using System.Diagnostics;

using Example.Components;

public sealed partial class GridListViewModel : AppViewModelBase
{
    private const string ColumnSettingsKey = "clamgrid.example.list.columns";

    private const string CancelText = "閉じる";

    private readonly IActionSheet actionSheet;

    private readonly IColumnSettingsStore columnSettingsStore;

    private readonly List<GridFrameEventArgs> frames = [];

    private readonly List<double> intervals = [];

    private int rowCount = 10000;

    private int nextId;

    private GridSortOrder[] savedSortOrders = [];

    private GridFrameEventArgs? latestFrame;

    private bool statisticsPending;

    private long sortStarted;

    private IDispatcherTimer? benchmarkTimer;

    private TaskCompletionSource? benchmarkCompletion;

    private CancellationTokenSource? benchmarkCancellation;

    private int frameNumber;

    private long previousFrame;

    public ObservableCollection<SampleRow> Source { get; private set; } = [];

    public GridDataView<SampleRow> Rows { get; }

    public GridController Grid { get; }

    [ObservableProperty]
    public partial GridSelectionMode SelectionMode { get; set; }

    [ObservableProperty]
    public partial GridStyle GridStyle { get; set; } = default!;

    [ObservableProperty]
    public partial string SelectionText { get; set; } = default!;

    [ObservableProperty]
    public partial string Statistics { get; set; } = default!;

    [ObservableProperty]
    public partial string Status { get; set; } = default!;

    [ObservableProperty]
    public partial bool IsIdle { get; set; } = true;

    public IObserveCommand SelectAllCommand { get; }

    public IObserveCommand ColumnConfigurationCommand { get; }

    public IObserveCommand ScrollTopCommand { get; }

    public IObserveCommand ScrollEndCommand { get; }

    public IObserveCommand FontCommand { get; }

    public IObserveCommand ActionCommand { get; }

    //--------------------------------------------------------------------------------
    // Constructor
    //--------------------------------------------------------------------------------

    public GridListViewModel(
        IActionSheet actionSheet,
        IColumnSettingsStore columnSettingsStore)
    {
        this.actionSheet = actionSheet;
        this.columnSettingsStore = columnSettingsStore;

        Rows = new GridDataView<SampleRow>(Source, static x => x.Id);
        Disposables.Add(Rows);
        SampleColumns.RegisterSorts(Rows);
        Rows.Changed += OnRowsChanged;
        Rows.SortRequested += OnSortRequested;
        Rows.SortChanged += OnSortChanged;
        Rows.SortFailed += OnSortFailed;

        Grid = new GridController(SampleColumns.CreateList(), columnSettingsStore.Load(ColumnSettingsKey));
        Grid.CellTapped += OnCellTapped;
        Grid.CellLongPressed += OnCellLongPressed;
        Grid.CellValueChanged += OnCellValueChanged;
        Grid.ColumnWidthChanged += OnColumnWidthChanged;
        Grid.FrameRendered += OnFrameRendered;

        SelectionMode = GridSelectionMode.MultipleToggle;
        GridStyle = CreateStyle(16);
        Statistics = String.Empty;

        SelectAllCommand = MakeDelegateCommand<bool>(x =>
        {
            if (x)
            {
                Rows.UpdateSelection(static row => !row.IsDiscontinued);
                Status = "長押し: 販売中の行を選択しました。";
            }
            else
            {
                Rows.ClearSelection();
                Status = "長押し: 選択を解除しました。";
            }
        });
        ColumnConfigurationCommand = MakeAsyncCommand<GridColumnConfigurationEventArgs>(_ => OpenColumnSettingsAsync(), _ => IsIdle);
        ScrollTopCommand = MakeDelegateCommand(() => Grid.ScrollIntoView(0, 0), () => IsIdle);
        ScrollEndCommand = MakeDelegateCommand(() => Grid.ScrollIntoView(Rows.Count - 1, Grid.VisibleColumnCount - 1), () => IsIdle);
        FontCommand = MakeDelegateCommand(() => GridStyle = CreateStyle(GridStyle.FontSize.Equals(16f) ? 22 : 16), () => IsIdle);
        ActionCommand = MakeAsyncCommand(ExecuteActionAsync, () => IsIdle);

        Load();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            benchmarkCancellation?.Cancel();
            benchmarkCancellation?.Dispose();
            benchmarkCancellation = null;
            benchmarkTimer?.Stop();
            Rows.Changed -= OnRowsChanged;
            Rows.SortRequested -= OnSortRequested;
            Rows.SortChanged -= OnSortChanged;
            Rows.SortFailed -= OnSortFailed;
            Grid.CellTapped -= OnCellTapped;
            Grid.CellLongPressed -= OnCellLongPressed;
            Grid.CellValueChanged -= OnCellValueChanged;
            Grid.ColumnWidthChanged -= OnColumnWidthChanged;
            Grid.FrameRendered -= OnFrameRendered;
        }

        base.Dispose(disposing);
    }

    //--------------------------------------------------------------------------------
    // Navigation
    //--------------------------------------------------------------------------------

    public override Task OnNavigatedToAsync(INavigationContext context)
    {
        if (context.Attribute.IsRestore() && context.Parameter.TryGetColumnOrders(out var orders))
        {
            Grid.ColumnOrders = orders;
            columnSettingsStore.Save(ColumnSettingsKey, Grid.ColumnOrders!);
            Status = $"列設定を保存しました。表示 {Grid.VisibleColumnCount} / {Grid.Columns.Count}列";
        }

        return Task.CompletedTask;
    }

    protected override Task OnNotifyBackAsync() => IsIdle ? Navigator.ForwardAsync(ViewId.GridMenu) : Task.CompletedTask;

    protected override Task OnNotifyFunction1() => OnNotifyBackAsync();

    protected override Task OnNotifyFunction2()
    {
        if (IsIdle)
        {
            rowCount = rowCount switch { 10000 => 1000, 1000 => 0, _ => 10000 };
            Load();
        }

        return Task.CompletedTask;
    }

    protected override Task OnNotifyFunction3()
    {
        if (IsIdle)
        {
            SelectionMode = SelectionMode switch
            {
                GridSelectionMode.MultipleToggle => GridSelectionMode.SingleToggle,
                GridSelectionMode.SingleToggle => GridSelectionMode.None,
                _ => GridSelectionMode.MultipleToggle
            };
            Status = SelectionMode switch { GridSelectionMode.SingleToggle => "単一選択", GridSelectionMode.None => "選択なし", _ => "複数選択" };
        }

        return Task.CompletedTask;
    }

    protected override Task OnNotifyFunction4() => IsIdle ? OpenColumnSettingsAsync() : Task.CompletedTask;

    //--------------------------------------------------------------------------------
    // Operation
    //--------------------------------------------------------------------------------

    private static GridStyle CreateStyle(float fontSize) => new()
    {
        FontSize = fontSize,
        RowHeaderWidth = fontSize > 16 ? 88 : 72,
        AscendingSortMark = "▲",
        DescendingSortMark = "▼",
        SortMarkPosition = GridSortMarkPosition.End,
        ShowSortPriority = true,
        CornerText = "No.",
        RowHeaderText = static context => context.IsSelected ? "✓" : null,
        RowBackground = static x => x is SampleRow { IsDiscontinued: true } ? SampleColumns.DiscontinuedBackground : null
    };

    private void Load()
    {
        Rows.RestoreSortOrders([]);
        Source = SampleData.CreateSource(rowCount);
        nextId = rowCount;
        Rows.SetSource(Source);
        Status = $"{rowCount:N0}行を読み込みました。チェックのタップで編集、長押しで一括選択、見出し境界のドラッグで列幅変更。";
    }

    private Task<bool> OpenColumnSettingsAsync() =>
        Navigator.PushAsync(ViewId.GridColumns, Parameters.MakeColumnEditSession(Grid.CreateColumnEditSession()));

    private async Task ExecuteActionAsync()
    {
        var operation = await actionSheet.ShowAsync("操作例", CancelText,
            "全選択", "選択解除", "列設定を初期化", "行を追加", "先頭行を末尾へ移動", "選択行を削除", "同じIDの別データに置換", "名前と状態を更新", "発注チェックを外部から変更", "販売中だけを選択", "外部からID42を選択", "ソート順を保存", "ソート順を復元", "再読込（選択解除）", "性能計測");
        if ((operation is null) || IsDisposed)
        {
            return;
        }

        var row = Rows.SelectedItems.OfType<SampleRow>().FirstOrDefault() ?? (Rows.Count == 0 ? null : Rows[0]);
        switch (operation)
        {
            case "全選択":
                Rows.SelectAll();
                Status = "全ての行を選択しました。";
                break;
            case "選択解除":
                Rows.ClearSelection();
                Status = "選択を解除しました。";
                break;
            case "列設定を初期化":
                Grid.ColumnOrders = [];
                columnSettingsStore.Remove(ColumnSettingsKey);
                Status = "列設定を既定の表示と順序へ戻しました。";
                break;
            case "行を追加":
                Source.Add(new SampleRow(nextId++));
                Status = $"追加 ID={nextId}";
                break;
            case "先頭行を末尾へ移動":
                if (Source.Count > 1)
                {
                    Source.Move(0, Source.Count - 1);
                }

                Status = "元データの先頭行を末尾へ移動しました。";
                break;
            case "選択行を削除":
                foreach (var item in Rows.SelectedItems.OfType<SampleRow>())
                {
                    Source.Remove(item);
                }

                Status = "選択した行を削除しました。";
                break;
            case "同じIDの別データに置換":
                if (row is not null)
                {
                    Source[Source.IndexOf(row)] = new SampleRow(row.Id) { Name = "置換した商品" };
                }

                Status = $"置換 ID={row?.Id + 1}（置換行の選択は解除）";
                break;
            case "名前と状態を更新":
                if (row is not null)
                {
                    row.Name = "更新した商品";
                    row.IsDiscontinued = !row.IsDiscontinued;
                }

                Status = $"値更新 ID={row?.Id + 1}";
                break;
            case "発注チェックを外部から変更":
                if (row is not null)
                {
                    row.IsChecked = !row.IsChecked;
                }

                Status = $"発注チェック変更 ID={row?.Id + 1}";
                break;
            case "販売中だけを選択":
                Rows.UpdateSelection(static item => !item.IsDiscontinued);
                Status = "販売中の行を選択しました。";
                break;
            case "外部からID42を選択":
                SelectByKey(41);
                break;
            case "ソート順を保存":
                savedSortOrders = Rows.SaveSortOrders();
                Status = $"{savedSortOrders.Length}個のソートキーを保存しました。";
                break;
            case "ソート順を復元":
                Status = $"ソート復元: {Rows.RestoreSortOrders(savedSortOrders).Status}";
                break;
            case "再読込（選択解除）":
                Source = SampleData.CreateSource(Source.Count);
                Rows.SetSource(Source);
                Status = "再読込で選択を解除しました。";
                break;
            case "性能計測":
                await BenchmarkAsync().ConfigureAwait(true);
                break;
        }
    }

    private void SelectByKey(int key)
    {
        var index = Rows.IndexOfKey(key);
        if (Rows.TryToggleSelection(index, out var selected))
        {
            Grid.ScrollIntoView(index, 0);
            Status = $"外部指定 ID={key + 1} / 選択={selected} / 表示行={index + 1}";
        }
        else
        {
            Status = $"ID{key + 1}を選択できません。";
        }
    }

    private void UpdateSelectionText()
    {
        var ids = Rows.Where((_, index) => Rows.IsSelected(index)).Take(3).Select(static x => x.Id + 1);
        SelectionText = $"選択 {Rows.SelectedCount:N0} / {Rows.Count:N0}件  ID: {String.Join(", ", ids)}";
    }

    //--------------------------------------------------------------------------------
    // Event
    //--------------------------------------------------------------------------------

    private void OnRowsChanged(object? sender, GridDataChangedEventArgs e) => UpdateSelectionText();

    private void OnCellTapped(object? sender, GridCellEventArgs e) =>
        Status = $"{e.Hit.CellType} 行={e.Hit.RowIndex + 1} 列={e.Hit.ColumnIndex + 1} / ID={(e.Item as SampleRow)?.Id + 1}";

    private void OnCellLongPressed(object? sender, GridCellEventArgs e) =>
        Status = $"長押し: {e.Hit.CellType} 行={e.Hit.RowIndex + 1} 列={e.Hit.ColumnIndex + 1}";

    private void OnCellValueChanged(object? sender, GridCellValueEventArgs e) =>
        Status = $"セル編集: {e.ColumnKey} ID={((SampleRow)e.Item).Id + 1} → {e.NewValue}";

    private void OnColumnWidthChanged(object? sender, GridColumnWidthEventArgs e) =>
        Status = $"列幅変更: {e.ColumnKey} / {e.OldWidth:F0} → {e.NewWidth:F0} DIP";

    private void OnSortRequested(object? sender, GridSortRequestedEventArgs e) => sortStarted = Stopwatch.GetTimestamp();

    private void OnSortChanged(object? sender, EventArgs e)
    {
        var orders = Rows.SortOrders;
        var header = orders.Count == 0 ? null : Grid.Columns.FirstOrDefault(x => (x.SortKey ?? x.Key) == orders[0].Key)?.Header;
        Status = orders.Count == 0 ? "並べ替えを解除しました。" : $"並べ替え: {header ?? orders[0].Key} {(orders[0].Descending ? "降順" : "昇順")}";
        global::Android.Util.Log.Info("ClamGrid.Sort", FormattableString.Invariant($"rows={Rows.Count};elapsed_ms={Stopwatch.GetElapsedTime(sortStarted).TotalMilliseconds:F3};keys={String.Join(',', orders.Select(static x => x.Key))}"));
    }

    private void OnSortFailed(object? sender, GridSortFailedEventArgs e)
    {
        Status = "並べ替えに失敗しました。データと比較条件を確認してください。";
        global::Android.Util.Log.Error("ClamGrid.Sort", e.Error.ToString());
    }

    private void OnFrameRendered(object? sender, GridFrameEventArgs e)
    {
        var now = Stopwatch.GetTimestamp();
        if (benchmarkCompletion is { } completion)
        {
            frameNumber++;
            if (frameNumber > 20)
            {
                frames.Add(e);
                intervals.Add(Stopwatch.GetElapsedTime(previousFrame, now).TotalMilliseconds);
            }

            previousFrame = now;
            if (frameNumber >= 140)
            {
                benchmarkTimer?.Stop();
                completion.TrySetResult();
            }
        }

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
        if (!IsDisposed && (latestFrame is { } frame))
        {
            var rows = frame.Rows.Count == 0 ? "なし" : $"{frame.Rows.Start + 1}–{frame.Rows.End}";
            var columns = frame.Columns.Count == 0 ? "なし" : $"{frame.Columns.Start + 1}–{frame.Columns.End}";
            Statistics = $"{Rows.Count:N0}行 × {Grid.VisibleColumnCount}列 / 可視 {rows}行 {columns}列 / 描画 {frame.RenderedCells}セル {frame.Milliseconds:F2}ms / X={frame.ScrollX:F0} Y={frame.ScrollY:F0}";
        }
    }

    //--------------------------------------------------------------------------------
    // Benchmark
    //--------------------------------------------------------------------------------

    private async Task BenchmarkAsync()
    {
        if (benchmarkCancellation is not null)
        {
            return;
        }

        benchmarkCancellation = new CancellationTokenSource();
        IsIdle = false;
        try
        {
            var summaries = new List<string>();
            foreach (var count in new[] { 1000, 10000 })
            {
                rowCount = count;
                GridStyle = CreateStyle(16);
                Load();
                frames.Clear();
                intervals.Clear();
                frameNumber = 0;
                previousFrame = Stopwatch.GetTimestamp();
                benchmarkCompletion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                benchmarkTimer = Dispatcher.GetForCurrentThread()!.CreateTimer();
                benchmarkTimer.Interval = TimeSpan.FromMilliseconds(16);
                benchmarkTimer.Tick += OnBenchmarkTick;
                benchmarkTimer.Start();
                await benchmarkCompletion.Task.WaitAsync(TimeSpan.FromSeconds(30), benchmarkCancellation.Token).ConfigureAwait(true);
                benchmarkTimer.Tick -= OnBenchmarkTick;
                benchmarkCompletion = null;
                var p95 = Percentile(frames.Select(static frame => frame.Milliseconds));
                var interval95 = Percentile(intervals);
                var maxCells = frames.Max(static frame => frame.RenderedCells);
                var maxMeasurements = frames.Max(static frame => frame.TextMeasurements);
                var pss = global::Android.OS.Debug.Pss;
                global::Android.Util.Log.Info("ClamGrid.Benchmark", FormattableString.Invariant($"rows={count};frames={frames.Count};paint_p95_ms={p95:F3};interval_p95_ms={interval95:F3};max_cells={maxCells};max_text_measurements={maxMeasurements};managed_bytes={GC.GetTotalMemory(false)};pss_kb={pss}"));
                summaries.Add($"{count:N0}行: p95 {p95:F2}ms / 最大{maxCells}セル");
            }

            Status = String.Join("\n", summaries);
        }
        catch (OperationCanceledException)
        {
            Status = "計測を中止しました。";
        }
        catch (TimeoutException)
        {
            Status = "計測がタイムアウトしました。";
        }
        finally
        {
            benchmarkTimer?.Stop();
            if (benchmarkTimer is not null)
            {
                benchmarkTimer.Tick -= OnBenchmarkTick;
            }

            benchmarkTimer = null;
            benchmarkCompletion = null;
            benchmarkCancellation?.Dispose();
            benchmarkCancellation = null;
            IsIdle = true;
        }
    }

    private void OnBenchmarkTick(object? sender, EventArgs e) => Grid.ScrollBy(2, 12);

    private static double Percentile(IEnumerable<double> values)
    {
        var ordered = values.Order().ToArray();
        return ordered[(int)Math.Ceiling(ordered.Length * 0.95) - 1];
    }
}
