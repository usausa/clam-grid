namespace Example.Modules;

#pragma warning disable CA1724
public static class Parameters
{
    private const string ColumnEditSession = nameof(ColumnEditSession);

    private const string ColumnOrders = nameof(ColumnOrders);

    public static NavigationParameter Make() => new();

    public static NavigationParameter MakeColumnEditSession(GridColumnEditSession session) =>
        new NavigationParameter().SetValue(ColumnEditSession, session);

    public static GridColumnEditSession GetColumnEditSession(this INavigationParameter parameter) =>
        parameter.GetValue<GridColumnEditSession>(ColumnEditSession);

    public static NavigationParameter MakeColumnOrders(GridColumnOrder[] orders) =>
        new NavigationParameter().SetValue(ColumnOrders, orders);

    public static bool TryGetColumnOrders(this INavigationParameter parameter, [NotNullWhen(true)] out GridColumnOrder[]? orders) =>
        parameter.TryGetValue(ColumnOrders, out orders);
}
#pragma warning restore CA1724
