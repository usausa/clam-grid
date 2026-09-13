namespace Example.Models;

using System.ComponentModel;
using System.Globalization;

// Synthetic helpdesk ticket; Status is 0 pending / 1 started / 2 completed (AdvanceStatus cycles it) and flag columns are emoji
public sealed class TicketRow(int id) : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    public int Id { get; } = id;
    public int Status { get; private set; } = id % 3;
    public int RequestState => Id % 3;
    public bool IsStarted => Status != 0;
    public bool IsCompleted => Status == 2;
    public int CompanyNumber => (Id % 30) + 1;
    public string CompanySortKey { get; } = $"ｻﾝﾌﾟﾙ{(id % 30) + 1:D4}00120034";
    public int ReceiptOrder => Id % 100;
    public DateTime? StartedDate => IsStarted ? new DateTime(2026, 9, 11) : null;

    public void AdvanceStatus()
    {
        Status = (Status + 1) % 3;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(String.Empty));
    }

    public string GetText(string key) => key switch
    {
        "StatusMark" => Status switch { 0 => "🕒", 1 => "🔧", _ => "✅" },
        "Priority" => (Id % 2) == 0 ? String.Empty : "🔥",
        "DeptCode" => (100 + (Id % 8)).ToString(CultureInfo.InvariantCulture),
        "TicketNo" => $"T{Id + 1:D12}",
        "GroupCode" => $"G{Id % 20:D2}",
        "TicketType" => (Id % 2) == 0 ? "❓" : "🐞",
        "FollowupType" => (Id % 2) == 0 ? "🔁" : "👀",
        "Escalated" => (Id % 2) == 0 ? "⚠️" : String.Empty,
        "CustomerName" => $"山田 太郎 {Id % 20:D2}",
        "SpecialNote" => "要注意",
        "ReceiptOrder" => ReceiptOrder.ToString("D6", CultureInfo.InvariantCulture),
        "CaseNo" => $"{Id % 1000:D3}",
        "CompanyKana" => "ｻﾝﾌﾟﾙ",
        "CompanyName" => $"サンプル株式会社 第{CompanyNumber}事業部 長い名称の表示確認",
        "Department" => $"第{(Id % 50) + 1}営業課",
        "ProductNo" => $"000{Id + 1:D8}",
        "SiteCode" => $"拠点{Id % 100:D3}",
        "ManagementNo" => $"管理{Id + 1:D6}",
        "StartedDate" => StartedDate?.ToString("MM/dd", CultureInfo.InvariantCulture) ?? String.Empty,
        "CompletedAt" => IsCompleted ? "09/11 09:00～12:00" : String.Empty,
        "Contact" => (Id % 2) == 0 ? "📞" : String.Empty,
        "RequestType" => RequestState switch { 0 => "🆕", 1 => "🔧", _ => "✅" },
        "OfficeCode" => $"{Id % 5:D3}",
        "ChangedAt" => $"09/{(Id % 20) + 1:D2} 10:00～12:00",
        "RequesterType" => "顧客",
        "PlannedAt" => "09/11 09:00～12:00",
        "AssigneeName" => "担当 花子",
        "ReceivedAt" => $"09/10 {(Id % 12) + 8:D2}:30",
        "ReceiverName" => $"受付 {(Id % 4) + 1}",
        "GroupId" => $"{Id / 3:D8}",
        "LineNo" => $"{Id % 3:D2}",
        "ProductType" => $"{Id % 2}",
        _ => throw new ArgumentException($"Unknown sample field: {key}", nameof(key))
    };
}
