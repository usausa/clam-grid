namespace Example.Models;

// XAML で宣言した列（Key）に対応する値アクセサ。実際のアプリでは { nameof(Entry.DeptCode), static x => x.DeptCode } のように型付きプロパティを登録する。
public static class TicketRowAccessors
{
    public static GridValueAccessorCollection<TicketRow> Ticket { get; } = new()
    {
        { "StatusMark", static x => x.GetText("StatusMark") },
        { "Priority", static x => x.GetText("Priority") },
        { "DeptCode", static x => x.GetText("DeptCode") },
        { "TicketNo", static x => x.GetText("TicketNo") },
        { "GroupCode", static x => x.GetText("GroupCode") },
        { "TicketType", static x => x.GetText("TicketType") },
        { "Escalated", static x => x.GetText("Escalated") },
        { "CustomerName", static x => x.GetText("CustomerName") },
        { "SpecialNote", static x => x.GetText("SpecialNote") },
        { "ReceiptOrder", static x => x.GetText("ReceiptOrder") },
        { "CaseNo", static x => x.GetText("CaseNo") },
        { "CompanyName", static x => x.GetText("CompanyName") },
        { "Department", static x => x.GetText("Department") },
        { "ProductNo", static x => x.GetText("ProductNo") },
        { "SiteCode", static x => x.GetText("SiteCode") },
        { "ManagementNo", static x => x.GetText("ManagementNo") },
        { "StartedDate", static x => x.GetText("StartedDate") },
        { "CompletedAt", static x => x.GetText("CompletedAt") }
    };
}
