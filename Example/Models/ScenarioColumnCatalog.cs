namespace Example.Models;

public sealed record ScenarioColumnDefinition(string Key, string Header, double Width, bool AlignEnd, bool GreenHeader, bool AllowSorting);

public static class ScenarioColumnCatalog
{
    public static IReadOnlyList<ScenarioColumnDefinition> Ticket { get; } = Array.AsReadOnly<ScenarioColumnDefinition>([
        new("StatusMark", "状", 45, false, false, false),
        new("Priority", "優先", 55, false, true, true),
        new("DeptCode", "部門", 85, true, true, true),
        new("TicketNo", "チケット番号", 190, true, true, true),
        new("GroupCode", "グループ", 80, false, false, false),
        new("TicketType", "種別", 55, false, true, true),
        new("Escalated", "エスカレ", 55, false, false, false),
        new("CustomerName", "顧客名", 200, false, true, true),
        new("SpecialNote", "特記", 100, false, false, false),
        new("ReceiptOrder", "受付順", 95, false, true, true),
        new("CaseNo", "案件番号", 95, false, true, true),
        new("CompanyName", "会社名", 200, false, true, true),
        new("Department", "部署", 200, false, true, true),
        new("ProductNo", "製品番号", 125, false, true, true),
        new("SiteCode", "拠点", 130, false, true, true),
        new("ManagementNo", "管理番号", 100, false, true, true),
        new("StartedDate", "対応開始日", 80, true, false, false),
        new("CompletedAt", "完了日時", 230, true, false, false),
    ]);

    public static IReadOnlyList<string> TicketSortKeys { get; } = Array.AsReadOnly<string>(["DeptCode", "Priority", "TicketNo", "TicketType", "CustomerName", "ReceiptOrder", "CaseNo", "CompanyName", "Department", "ProductNo", "SiteCode", "ManagementNo", "GroupId", "LineNo", "SortOrder", "ProductType", "IsCompleted", "IsStarted"]);

    public static IReadOnlyList<ScenarioColumnDefinition> Followup { get; } = Array.AsReadOnly<ScenarioColumnDefinition>([
        new("FollowupType", "種類", 80, false, true, true),
        new("Priority", "優先", 45, false, false, false),
        new("DeptCode", "部門", 85, true, true, true),
        new("TicketNo", "チケット番号", 190, true, true, true),
        new("GroupCode", "グループ", 80, false, false, false),
        new("Escalated", "エスカレ", 55, false, false, false),
        new("CustomerName", "顧客名", 200, false, true, true),
        new("SpecialNote", "特記", 100, false, false, false),
        new("CaseNo", "案件番号", 95, false, true, true),
        new("CompanyKana", "会社名カナ", 200, false, true, true),
        new("CompanyName", "会社名", 200, false, true, true),
        new("Department", "部署", 200, false, false, false),
        new("ProductNo", "製品番号", 125, false, true, true),
        new("SiteCode", "拠点", 130, false, true, true),
        new("ManagementNo", "管理番号", 100, false, false, false),
        new("Contact", "連絡", 45, false, false, false),
        new("CompletedAt", "完了日時", 230, true, false, false),
    ]);

    public static IReadOnlyList<string> FollowupSortKeys { get; } = Array.AsReadOnly<string>(["FollowupType", "DeptCode", "TicketNo", "CustomerName", "CaseNo", "CompanyKana", "CompanyName", "ProductNo", "SiteCode", "LineNo"]);

    public static IReadOnlyList<ScenarioColumnDefinition> Escalation { get; } = Array.AsReadOnly<ScenarioColumnDefinition>([
        new("StatusMark", "状", 45, false, false, false),
        new("Priority", "優先", 45, false, false, true),
        new("DeptCode", "部門", 85, true, true, true),
        new("TicketNo", "チケット番号", 180, true, true, true),
        new("GroupCode", "グループ", 80, false, false, false),
        new("TicketType", "種別", 55, false, true, true),
        new("Escalated", "エスカレ", 55, false, false, false),
        new("CustomerName", "顧客名", 200, false, true, true),
        new("SpecialNote", "特記", 100, false, false, false),
        new("ReceiptOrder", "受付順", 95, false, true, true),
        new("CaseNo", "案件番号", 95, false, true, true),
        new("CompanyName", "会社名", 200, false, true, true),
        new("Department", "部署", 200, false, true, true),
        new("ProductNo", "製品番号", 125, false, true, true),
        new("SiteCode", "拠点", 130, false, true, true),
        new("ManagementNo", "管理番号", 100, false, true, true),
        new("StartedDate", "対応開始日", 80, true, false, false),
        new("CompletedAt", "完了日時", 230, true, false, false),
    ]);

    public static IReadOnlyList<string> EscalationSortKeys { get; } = Array.AsReadOnly<string>(["DeptCode", "Priority", "TicketNo", "TicketType", "CustomerName", "ReceiptOrder", "CaseNo", "CompanyName", "Department", "ProductNo", "SiteCode", "ManagementNo", "GroupId", "LineNo", "SortOrder", "ProductType", "IsCompleted", "IsStarted"]);

    public static IReadOnlyList<ScenarioColumnDefinition> Request { get; } = Array.AsReadOnly<ScenarioColumnDefinition>([
        new("Contact", "連絡", 50, false, false, false),
        new("RequestType", "依頼区分", 120, false, false, false),
        new("OfficeCode", "拠点コード", 80, false, false, true),
        new("DeptCode", "部門", 90, true, true, true),
        new("TicketNo", "チケット番号", 190, true, true, true),
        new("Priority", "優先", 45, false, false, false),
        new("ReceiptOrder", "受付順", 95, false, false, false),
        new("CaseNo", "案件番号", 95, false, true, true),
        new("CustomerName", "顧客名", 200, false, false, false),
        new("CompanyName", "会社名", 200, false, false, false),
        new("ChangedAt", "変更後日時", 200, true, true, true),
        new("RequesterType", "依頼者", 200, false, false, false),
        new("PlannedAt", "当初予定", 200, true, true, false),
        new("AssigneeName", "担当者", 200, false, false, false),
        new("ProductNo", "製品番号", 125, false, false, false),
        new("SiteCode", "拠点", 130, false, false, false),
        new("ReceivedAt", "受付日時", 150, true, true, true),
        new("ReceiverName", "受付者", 200, false, false, true),
    ]);

    public static IReadOnlyList<string> RequestSortKeys { get; } = Array.AsReadOnly<string>(["OfficeCode", "DeptCode", "TicketNo", "CaseNo", "ChangedAt", "ReceivedAt", "ReceiverName"]);
}
