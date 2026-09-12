namespace Example.Models;

public sealed partial record ListScenario(string Key, string Title, string Family, string? PendingField, bool SingleSelection, IReadOnlyList<string> DefaultKeys)
{
    public static IReadOnlyList<ListScenario> All { get; } = Array.AsReadOnly<ListScenario>([
        new("TodoList", "未対応一覧", "Ticket", "IsStarted", false, ["IsStarted", "ReceiptOrder", "TicketType", "DeptCode", "GroupId", "SortOrder", "LineNo"]),
        new("TicketList", "チケット一覧", "Ticket", "IsCompleted", false, ["IsCompleted", "ReceiptOrder", "TicketType", "DeptCode", "GroupId", "LineNo", "SortOrder"]),
        new("TicketSearch", "検索・チケット", "Ticket", null, false, ["DeptCode", "TicketType", "GroupId", "SortOrder", "ProductType"]),
        new("FollowupList", "フォロー一覧", "Followup", null, false, ["FollowupType", "DeptCode", "GroupId", "SortOrder", "LineNo"]),
        new("FollowupSearch", "検索・フォロー", "Followup", null, false, ["FollowupType", "DeptCode", "GroupId", "SortOrder", "LineNo"]),
        new("EscalationSearch", "検索・エスカレーション", "Escalation", null, false, ["TicketType", "DeptCode", "GroupId", "SortOrder", "LineNo"]),
        new("RequestList", "依頼一覧", "Request", null, true, ["OfficeCode", "TicketNo", "ReceivedAt"])
    ]);

    public IReadOnlyList<ScenarioColumnDefinition> Columns => Family switch
    {
        "Ticket" => ScenarioColumnCatalog.Ticket,
        "Followup" => ScenarioColumnCatalog.Followup,
        "Escalation" => ScenarioColumnCatalog.Escalation,
        _ => ScenarioColumnCatalog.Request
    };

    public string Background(TicketRow row) => (SingleSelection ? row.RequestState : row.Status) switch
    {
        0 => "#B3E5FC",
        1 => "#FFF59D",
        _ => SingleSelection ? "#EF9A9A" : "#F8BBD0"
    };

    public bool IsPending(TicketRow row) => PendingField switch
    {
        "IsStarted" => !row.IsStarted,
        "IsCompleted" => !row.IsCompleted,
        _ => true
    };

    public void SelectAll(GridDataView<TicketRow> view, bool select)
    {
        if (!select)
        {
            view.ClearSelection();
        }
        else if (!SingleSelection)
        {
            view.UpdateSelection(IsPending);
        }
    }

    public GridDataView<TicketRow> CreateData(int count, bool useSortCallback = false)
    {
        var view = new GridDataView<TicketRow>(Enumerable.Range(0, count).Select(static id => new TicketRow(id)).ToArray(), static row => row.Id)
        {
            SelectionMode = SingleSelection ? GridSelectionMode.SingleToggle : GridSelectionMode.MultipleToggle
        };
        var keys = GetSortKeys();
        foreach (var key in keys)
        {
            var pinPending = (PendingField is not null) && (Columns.Any(column => column.Key == key) || (key == "ProductType"));
            view.RegisterComparer(key, (left, right, descending) =>
            {
                var group = pinPending ? (!IsPending(left)).CompareTo(!IsPending(right)) : 0;
                return group != 0 ? group : descending ? Compare(key, right, left) : Compare(key, left, right);
            });
        }

        if (useSortCallback)
        {
            view.SetSortCallback(keys, SortRows);
        }

        view.RestoreSortOrders(DefaultKeys.Select(static key => new GridSortOrder(key)));
        return view;
    }

    public string[] GetSortKeys()
    {
        var keys = Family switch
        {
            "Ticket" => ScenarioColumnCatalog.TicketSortKeys,
            "Followup" => ScenarioColumnCatalog.FollowupSortKeys,
            "Escalation" => ScenarioColumnCatalog.EscalationSortKeys,
            _ => ScenarioColumnCatalog.RequestSortKeys
        };
        return keys.Where(key => (PendingField is null) || (key is not ("IsStarted" or "IsCompleted")) || (key == PendingField)).ToArray();
    }

    private static int Compare(string key, TicketRow left, TicketRow right) => key switch
    {
        "IsStarted" => left.IsStarted.CompareTo(right.IsStarted),
        "IsCompleted" => left.IsCompleted.CompareTo(right.IsCompleted),
        "SortOrder" => left.Id.CompareTo(right.Id),
        "CompanyName" => StringComparer.CurrentCulture.Compare(left.CompanySortKey, right.CompanySortKey),
        _ => StringComparer.CurrentCulture.Compare(left.GetText(key), right.GetText(key))
    };
}
