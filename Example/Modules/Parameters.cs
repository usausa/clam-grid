namespace Example.Modules;

#pragma warning disable CA1724
public static class Parameters
{
    private const string ColumnEditSession = nameof(ColumnEditSession);

    private const string ColumnStates = nameof(ColumnStates);

    public static NavigationParameter Make() => new();

    public static NavigationParameter MakeColumnEditSession(GridColumnEditSession session) =>
        new NavigationParameter().SetValue(ColumnEditSession, session);

    public static GridColumnEditSession GetColumnEditSession(this INavigationParameter parameter) =>
        parameter.GetValue<GridColumnEditSession>(ColumnEditSession);

    public static NavigationParameter MakeColumnStates(GridColumnState[] orders) =>
        new NavigationParameter().SetValue(ColumnStates, orders);

    public static bool TryGetColumnStates(this INavigationParameter parameter, [NotNullWhen(true)] out GridColumnState[]? orders) =>
        parameter.TryGetValue(ColumnStates, out orders);
}
#pragma warning restore CA1724
