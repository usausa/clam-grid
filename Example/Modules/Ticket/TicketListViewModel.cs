namespace Example.Modules.Ticket;

using Example.Components;
using Example.Modules.Helpers;
using Example.State;

// View model of the list screen, connected to the grid part (Parts/TicketGrid) through IColumnEditable
public sealed partial class TicketListViewModel : AppViewModelBase, IColumnEditable
{
    private const string ColumnSettingsKey = "clamgrid.example.work.columns";

    private const int RowCount = 300;

    private static readonly GridSortOrder[] DefaultSortOrder =
    [
        new("IsCompleted"),
        new("ReceiptOrder"),
        new("TicketType"),
        new("DeptCode"),
        new("GroupId"),
        new("LineNo"),
        new("SortOrder")
    ];

    private readonly IColumnSettingsStore columnSettingsStore;

    private readonly AppSession session;

    private int scanIndex;

    public ICommand SelectCommand { get; }

    public ICommand ColumnEditCommand { get; }

    public GridSelectRequest GridSelectRequest { get; } = new();

    // Visibility and order of the columns, bound TwoWay and saved when the normalized value comes back
    public IReadOnlyList<GridColumnOrder>? ColumnOrders
    {
        get;
        set
        {
            if (ReferenceEquals(field, value))
            {
                return;
            }

            field = value;
            RaisePropertyChanged(nameof(ColumnOrders));
            if (value is not null)
            {
                columnSettingsStore.Save(ColumnSettingsKey, value);
            }
        }
    }

    // Rows, selection and sort state in one object
    public GridDataView<TicketRow> Items { get; }

    [ObservableProperty]
    public partial int PendingCount { get; set; }

    [ObservableProperty]
    public partial int CompletedCount { get; set; }

    [ObservableProperty]
    public partial string Status { get; set; } = default!;

    public IObserveCommand CommitCommand { get; }

    public IObserveCommand AdvanceCommand { get; }

    public IObserveCommand ScanCommand { get; }

    public IObserveCommand ReloadCommand { get; }

    //--------------------------------------------------------------------------------
    // Constructor
    //--------------------------------------------------------------------------------

    public TicketListViewModel(
        IColumnSettingsStore columnSettingsStore,
        AppSession session)
    {
        this.columnSettingsStore = columnSettingsStore;
        this.session = session;

        // Saved column settings applied through the binding; unknown columns are dropped and new ones are appended hidden
        ColumnOrders = columnSettingsStore.Load(ColumnSettingsKey);

        Items = new GridDataView<TicketRow>(Array.Empty<TicketRow>(), static x => x.Id);
        Items.RegisterComparers(TicketComparers.Default);
        Items.PropertyChanged += OnItemsPropertyChanged;
        Disposables.Add(Items);

        // Long press: selects only pending rows, or clears all
        SelectCommand = MakeDelegateCommand<bool>(x => Items.UpdateSelection(item => x && !item.IsCompleted));
        // Header long press: opens the column settings screen
        ColumnEditCommand = MakeAsyncCommand<GridColumnConfigurationEventArgs>(x => Navigator.PushAsync(ViewId.GridColumns, Parameters.MakeColumnEditSession(x.CreateEditSession())));

        CommitCommand = MakeDelegateCommand(Commit, () => Items.SelectedCount > 0);
        AdvanceCommand = MakeDelegateCommand(Advance, () => Items.SelectedCount == 1);
        ScanCommand = MakeDelegateCommand(Scan, () => Items.Count > 0);
        ReloadCommand = MakeDelegateCommand(Load);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            Items.PropertyChanged -= OnItemsPropertyChanged;
        }

        base.Dispose(disposing);
    }

    //--------------------------------------------------------------------------------
    // Navigation
    //--------------------------------------------------------------------------------

    public override Task OnNavigatingToAsync(INavigationContext context)
    {
        if (!context.Attribute.IsRestore())
        {
            Load();
        }
        else if (context.Parameter.TryGetColumnOrders(out var orders))
        {
            // Result of the column settings screen; the grid writes the normalized value back, so the setter saves it
            ColumnOrders = orders;
        }

        return Task.CompletedTask;
    }

    protected override Task OnNotifyBackAsync() => Navigator.ForwardAsync(ViewId.GridMenu);

    protected override Task OnNotifyFunction1() => OnNotifyBackAsync();

    protected override Task OnNotifyFunction2()
    {
        SelectCommand.Execute(true);
        return Task.CompletedTask;
    }

    protected override Task OnNotifyFunction3()
    {
        Items.ClearSelection();
        return Task.CompletedTask;
    }

    protected override Task OnNotifyFunction4()
    {
        // Restores the default column settings; null shows the default and the normalized value is written back
        columnSettingsStore.Remove(ColumnSettingsKey);
        ColumnOrders = null;
        Status = "列設定を既定の表示と順序へ戻しました。";
        return Task.CompletedTask;
    }

    //--------------------------------------------------------------------------------
    // Operation
    //--------------------------------------------------------------------------------

    private void Load()
    {
        var rows = Enumerable.Range(0, RowCount).Select(static id => new TicketRow(id)).ToArray();
        Items.SetSource(rows);
        // Sort orders kept across navigation take precedence
        Items.RestoreSortOrders(session.SortOrders ?? DefaultSortOrder);
        UpdateCounts();
        Status = "タップで選択、長押しで未完了を一括選択。見出しタップで並べ替え、見出し長押しで列設定。";
    }

    private void Commit()
    {
        var selected = Items.SelectedItems.Cast<TicketRow>().ToArray();
        var groups = selected.Select(static x => x.GetText("GroupId")).Distinct(StringComparer.Ordinal).Count();
        // Keep the sort state
        session.SortOrders = Items.SaveSortOrders();
        Status = $"確定: {selected.Length}件 / {groups}グループ / ID {String.Join(", ", selected.Take(5).Select(static x => x.Id + 1))}";
    }

    private void Advance()
    {
        var row = (TicketRow)Items.SelectedItems[0];
        // The INotifyPropertyChanged notification refreshes sorting and colors while the selection follows the row identity
        row.AdvanceStatus();
        UpdateCounts();
        Status = $"状態更新: ID={row.Id + 1} / {row.GetText("StatusMark")}";
    }

    private void Scan()
    {
        // Picks one product number and selects only the matching rows
        scanIndex = (scanIndex + 7) % Items.Count;
        var productNo = Items[scanIndex].GetText("ProductNo");
        for (var i = 0; i < Items.Count; i++)
        {
            if ((Items[i].GetText("ProductNo") != productNo) && Items.IsSelected(i))
            {
                GridSelectRequest.Select(i);
            }
        }

        for (var i = 0; i < Items.Count; i++)
        {
            if ((Items[i].GetText("ProductNo") == productNo) && !Items.IsSelected(i))
            {
                GridSelectRequest.Select(i);
            }
        }

        Status = $"検索: 製品番号 {productNo} / 選択 {Items.SelectedCount}件";
    }

    private void UpdateCounts()
    {
        PendingCount = Items.Count(static x => !x.IsCompleted);
        CompletedCount = Items.Count - PendingCount;
    }

    //--------------------------------------------------------------------------------
    // Event
    //--------------------------------------------------------------------------------

    private void OnItemsPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(Items.SelectedCount) or nameof(Items.Count))
        {
            // Updates the command states from the selection count
            CommitCommand.RaiseCanExecuteChanged();
            AdvanceCommand.RaiseCanExecuteChanged();
            ScanCommand.RaiseCanExecuteChanged();
        }
    }
}
