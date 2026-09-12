namespace Example.State;

using BunnyTail.DependencyInjection;

// 画面遷移をまたいで保持する状態。
[Singleton]
public sealed class AppSession
{
    public IReadOnlyList<GridSortOrder>? SortOrders { get; set; }
}
