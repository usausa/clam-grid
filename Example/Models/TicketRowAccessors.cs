namespace Example.Models;

// Value accessors for the columns declared in XAML; ReceiptOrder and StartedDate are typed and formatted by the column Format
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
        { "ReceiptOrder", static x => x.ReceiptOrder },
        { "CaseNo", static x => x.GetText("CaseNo") },
        { "CompanyName", static x => x.GetText("CompanyName") },
        { "Department", static x => x.GetText("Department") },
        { "ProductNo", static x => x.GetText("ProductNo") },
        { "SiteCode", static x => x.GetText("SiteCode") },
        { "ManagementNo", static x => x.GetText("ManagementNo") },
        { "StartedDate", static x => x.StartedDate },
        { "CompletedAt", static x => x.GetText("CompletedAt") }
    };
}
