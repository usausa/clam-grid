namespace Example.Models;

// Product row of the list sample; raises INotifyPropertyChanged and the order and discontinued flags are editable in the grid
public sealed class SampleRow : NotificationObject
{
    public int Id { get; }

    public string Code => $"P{Id + 1:D6}";

    public string Name
    {
        get;
        set => SetProperty(ref field, value);
    }

    public bool IsChecked
    {
        get;
        set => SetProperty(ref field, value);
    }

    public bool IsDiscontinued
    {
        get;
        set => SetProperty(ref field, value);
    }

    // Status and category are emoji, which also exercises the emoji font fallback
    public string Status => IsDiscontinued ? "⛔" : "🟢";

    public string Category => (Id % 4) switch { 0 => "🍎", 1 => "🧴", 2 => "✏️", _ => "🎁" };

    public int Warehouse => 100 + (Id % 80);

    public string Supplier => $"S{Id % 20:D2}";

    public int Difference => ((Id * 37) % 2000) - 500;

    public int DescriptionNumber => (Id % 30) + 1;

    public string Description => $"サンプル商品の説明 {DescriptionNumber} 行の高さと横スクロールを確認するための長い文章";

    public string Location => $"{(char)('A' + (Id % 6))}-{(Id % 40) + 1:D2}";

    public string Unit => (Id % 3) == 0 ? "箱" : "個";

    public DateTime ArrivalDate => new(2026, 9, (Id % 28) + 1);

    public DateTime UpdatedAt => new(2026, 9, (Id % 28) + 1, (Id % 12) + 8, 0, 0);

    public SampleRow(int id)
    {
        Id = id;
        Name = $"サンプル商品 {id + 1:D5}";
        IsChecked = (id % 3) != 0;
        IsDiscontinued = (id % 4) == 0;
    }
}
