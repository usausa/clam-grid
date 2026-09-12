namespace Example.Modules.Grid;

using Example.Components;

public sealed partial class GridScenarioViewModel : AppViewModelBase
{
    private const string ColumnSettingsKeyPrefix = "clamgrid.example.scenario.columns.";

    private readonly IColumnSettingsStore columnSettingsStore;

    private readonly Dictionary<string, GridSortOrder[]> sessions = [with(StringComparer.Ordinal)];

    private ListScenario? scenario;

    public IReadOnlyList<string> ScenarioNames { get; } = ListScenario.All.Select(static x => $"{x.Title} / {x.Key}").ToArray();

    public int SelectedIndex
    {
        get;
        set
        {
            if (field != value)
            {
                field = value;
                RaisePropertyChanged(nameof(SelectedIndex));
                SwitchScenario();
            }
        }
    }

    [ObservableProperty]
    public partial GridDataView<TicketRow>? Data { get; set; }

    [ObservableProperty]
    public partial GridController? Grid { get; set; }

    [ObservableProperty]
    public partial GridStyle GridStyle { get; set; } = default!;

    [ObservableProperty]
    public partial GridSelectionMode SelectionMode { get; set; }

    [ObservableProperty]
    public partial string Status { get; set; } = default!;

    [ObservableProperty]
    public partial bool CanBulkSelect { get; set; }

    [ObservableProperty]
    public partial bool CanCommit { get; set; }

    public IObserveCommand SelectAllCommand { get; }

    public IObserveCommand ColumnConfigurationCommand { get; }

    public IObserveCommand CommitCommand { get; }

    public IObserveCommand SelectKeyCommand { get; }

    public IObserveCommand AdvanceCommand { get; }

    public IObserveCommand ResetColumnsCommand { get; }

    public IObserveCommand ReloadCommand { get; }

    //--------------------------------------------------------------------------------
    // Constructor
    //--------------------------------------------------------------------------------

    public GridScenarioViewModel(IColumnSettingsStore columnSettingsStore)
    {
        this.columnSettingsStore = columnSettingsStore;

        GridStyle = new GridStyle();

        SelectAllCommand = MakeDelegateCommand<bool>(x => scenario?.SelectAll(Data!, x));
        ColumnConfigurationCommand = MakeAsyncCommand<GridColumnConfigurationEventArgs>(_ => OpenColumnSettingsAsync());
        CommitCommand = MakeDelegateCommand(() => Status = $"選択データ: {String.Join(", ", Data!.SelectedItems.Cast<TicketRow>().Take(5).Select(static row => row.Id + 1))}（合成データ）", () => CanCommit);
        SelectKeyCommand = MakeDelegateCommand(SelectKey);
        AdvanceCommand = MakeDelegateCommand(() => (Data!.SelectedItems.Cast<TicketRow>().FirstOrDefault() ?? Data[0]).AdvanceStatus());
        ResetColumnsCommand = MakeDelegateCommand(ResetColumns);
        ReloadCommand = MakeDelegateCommand(SwitchScenario);

        SwitchScenario();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            DetachData();
        }

        base.Dispose(disposing);
    }

    //--------------------------------------------------------------------------------
    // Navigation
    //--------------------------------------------------------------------------------

    public override Task OnNavigatedToAsync(INavigationContext context)
    {
        if (context.Attribute.IsRestore() && context.Parameter.TryGetColumnOrders(out var orders) && (Grid is { } grid))
        {
            grid.ColumnOrders = orders;
            columnSettingsStore.Save(ColumnSettingsKey, grid.ColumnOrders!);
            UpdateStatus();
        }

        return Task.CompletedTask;
    }

    protected override Task OnNotifyBackAsync() => Navigator.ForwardAsync(ViewId.GridMenu);

    protected override Task OnNotifyFunction1() => OnNotifyBackAsync();

    protected override Task OnNotifyFunction2()
    {
        if (CanBulkSelect)
        {
            scenario?.SelectAll(Data!, true);
        }

        return Task.CompletedTask;
    }

    protected override Task OnNotifyFunction3()
    {
        Data?.ClearSelection();
        return Task.CompletedTask;
    }

    protected override Task OnNotifyFunction4() => OpenColumnSettingsAsync();

    //--------------------------------------------------------------------------------
    // Operation
    //--------------------------------------------------------------------------------

    private string ColumnSettingsKey => ColumnSettingsKeyPrefix + scenario!.Family;

    private void SwitchScenario()
    {
        if (IsDisposed || (SelectedIndex < 0))
        {
            return;
        }

        if ((scenario is not null) && (Data is not null))
        {
            sessions[scenario.Key] = Data.SaveSortOrders();
        }

        DetachData();
        scenario = ListScenario.All[SelectedIndex];
        var data = scenario.CreateData(300);
        if (sessions.TryGetValue(scenario.Key, out var saved))
        {
            data.RestoreSortOrders(saved);
        }

        data.Changed += OnDataChanged;
        GridStyle = ScenarioGridFactory.CreateStyle(scenario);
        SelectionMode = scenario.SingleSelection ? GridSelectionMode.SingleToggle : GridSelectionMode.MultipleToggle;
        Grid = new GridController(ScenarioGridFactory.CreateColumns(scenario), columnSettingsStore.Load(ColumnSettingsKey));
        Data = data;
        UpdateStatus();
    }

    private void DetachData()
    {
        if (Data is { } data)
        {
            data.Changed -= OnDataChanged;
            Data = null;
            data.Dispose();
        }
    }

    private Task<bool> OpenColumnSettingsAsync() =>
        Grid is { } grid ? Navigator.PushAsync(ViewId.GridColumns, Parameters.MakeColumnEditSession(grid.CreateColumnEditSession())) : Task.FromResult(false);

    private void SelectKey()
    {
        if ((Data is not { } data) || (Grid is not { } grid))
        {
            return;
        }

        var index = data.IndexOfKey(41);
        if (data.TryToggleSelection(index, out _))
        {
            grid.ScrollIntoView(index, 0);
        }
    }

    private void ResetColumns()
    {
        if (Grid is { } grid)
        {
            columnSettingsStore.Remove(ColumnSettingsKey);
            grid.ColumnOrders = [];
            UpdateStatus();
        }
    }

    private void UpdateStatus()
    {
        if ((Data is not { } data) || (scenario is null) || (Grid is not { } grid))
        {
            return;
        }

        CanCommit = data.SelectedCount > 0;
        CanBulkSelect = !scenario.SingleSelection;
        Status = $"{scenario.Family}: {data.Count}行 / {grid.VisibleColumnCount}列 / 選択 {data.SelectedCount}件\n" +
            (scenario.SingleSelection ? "単一選択" : scenario.PendingField is null ? "複数選択・長押しで全選択" : $"長押し: {(scenario.PendingField == "IsStarted" ? "未対応" : "未完了")}を選択") +
            $" / ソート {String.Join(" → ", data.SortOrders.Take(2).Select(order => SortLabel(order.Key) + (order.Descending ? "↓" : "↑")))}";
    }

    private string SortLabel(string key) => scenario!.Columns.FirstOrDefault(column => column.Key == key)?.Header ?? key switch
    {
        "IsStarted" => "対応状態",
        "IsCompleted" => "完了状態",
        "GroupId" => "案件グループ",
        "SortOrder" => "基本順序",
        "LineNo" => "枝番",
        "ProductType" => "製品種別",
        _ => key
    };

    private void OnDataChanged(object? sender, GridDataChangedEventArgs e) => UpdateStatus();
}
