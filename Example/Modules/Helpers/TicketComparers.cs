namespace Example.Modules.Helpers;

// 未完了の行を方向に関わらず先頭に固定し、続けて各列を比較する。
public static class TicketComparers
{
    public static IReadOnlyDictionary<string, Func<TicketRow, TicketRow, bool, int>> Default { get; } = new Dictionary<string, Func<TicketRow, TicketRow, bool, int>>(StringComparer.Ordinal)
    {
        { "DeptCode", Pending(Text("DeptCode")) },
        { "Priority", Pending(Text("Priority")) },
        { "TicketNo", Pending(Text("TicketNo")) },
        { "TicketType", Pending(Text("TicketType")) },
        { "CustomerName", Pending(Text("CustomerName")) },
        { "ReceiptOrder", Pending(ComparerFactory.Default<TicketRow, int>(static x => x.ReceiptOrder)) },
        { "CaseNo", Pending(Text("CaseNo")) },
        { "CompanyName", Pending(ComparerFactory.Default<TicketRow, string>(static x => x.CompanySortKey)) },
        { "Department", Pending(Text("Department")) },
        { "ProductNo", Pending(Text("ProductNo")) },
        { "SiteCode", Pending(Text("SiteCode")) },
        { "ManagementNo", Pending(Text("ManagementNo")) },
        { "GroupId", Text("GroupId") },
        { "LineNo", Text("LineNo") },
        { "SortOrder", ComparerFactory.Default<TicketRow, int>(static x => x.Id) },
        { "ProductType", Pending(Text("ProductType")) },
        { "IsCompleted", ComparerFactory.Default<TicketRow, bool>(static x => x.IsCompleted) }
    };

    private static Func<TicketRow, TicketRow, bool, int> Text(string key) =>
        ComparerFactory.Default<TicketRow, string>(x => x.GetText(key));

    private static Func<TicketRow, TicketRow, bool, int> Pending(Func<TicketRow, TicketRow, bool, int> comparer) =>
        ComparerFactory.Chain(ComparerFactory.Ascending<TicketRow, bool>(static x => x.IsCompleted), comparer);
}
