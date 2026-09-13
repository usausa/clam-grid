namespace Example.State;

using BunnyTail.DependencyInjection;

// State kept across navigation
[Singleton]
public sealed class AppSession
{
    public IReadOnlyList<GridSortOrder>? SortOrders { get; set; }
}
