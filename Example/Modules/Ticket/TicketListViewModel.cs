namespace Example.Modules.Ticket;

using Example.Components;
using Example.Modules.Helpers;
using Example.State;

// 一覧画面のViewModel。グリッド部品（Parts/TicketGrid）とはIColumnEditableで接続する。
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

    // 列の表示と順序。グリッドへTwoWayでバインドし、正規化された値が戻ったら保存する
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

    // 一覧・選択・ソートを1つで担う。
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

        // 保存済みの列設定。バインディングで適用され、未知の列は除外・新しい列は非表示で補われる
        ColumnOrders = columnSettingsStore.Load(ColumnSettingsKey);

        Items = new GridDataView<TicketRow>(Array.Empty<TicketRow>(), static x => x.Id);
        Items.RegisterComparers(TicketComparers.Default);
        Items.PropertyChanged += OnItemsPropertyChanged;
        Disposables.Add(Items);

        // 長押し: 未完了だけを一括選択（解除時は全解除）
        SelectCommand = MakeDelegateCommand<bool>(x => Items.UpdateSelection(item => x && !item.IsCompleted));
        // 見出し長押し: 列設定画面へ
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
            // 列設定画面の結果。グリッドが正規化した値を書き戻すので、保存は setter に任せる
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
        // 列設定を既定へ戻す（null で既定の表示になり、正規化された既定値が書き戻される）
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
        // ソート条件は画面遷移をまたいで保持した内容を優先する
        Items.RestoreSortOrders(session.SortOrders ?? DefaultSortOrder);
        UpdateCounts();
        Status = "タップで選択、長押しで未完了を一括選択。見出しタップで並べ替え、見出し長押しで列設定。";
    }

    private void Commit()
    {
        var selected = Items.SelectedItems.Cast<TicketRow>().ToArray();
        var groups = selected.Select(static x => x.GetText("GroupId")).Distinct(StringComparer.Ordinal).Count();
        // 状態保持
        session.SortOrders = Items.SaveSortOrders();
        Status = $"確定: {selected.Length}件 / {groups}グループ / ID {String.Join(", ", selected.Take(5).Select(static x => x.Id + 1))}";
    }

    private void Advance()
    {
        var row = (TicketRow)Items.SelectedItems[0];
        // INotifyPropertyChangedの通知で並べ替えと着色が更新され、選択は行の同一性で維持される
        row.AdvanceStatus();
        UpdateCounts();
        Status = $"状態更新: ID={row.Id + 1} / {row.GetText("StatusMark")}";
    }

    private void Scan()
    {
        // 製品番号を1件決め、一致する行だけを選択状態にする
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
            // 選択数に応じてコマンドの可否を更新する
            CommitCommand.RaiseCanExecuteChanged();
            AdvanceCommand.RaiseCanExecuteChanged();
            ScanCommand.RaiseCanExecuteChanged();
        }
    }
}
