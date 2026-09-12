namespace Example.Components;

using System.Text.Json;

using BunnyTail.DependencyInjection;

public interface IColumnSettingsStore
{
    GridColumnOrder[]? Load(string key);

    void Save(string key, IEnumerable<GridColumnOrder> orders);

    void Remove(string key);
}

[Singleton(As = typeof(IColumnSettingsStore))]
public sealed class ColumnSettingsStore : IColumnSettingsStore
{
    public GridColumnOrder[]? Load(string key)
    {
        try
        {
            return ColumnSettingsCodec.Read(Preferences.Default.Get<string?>(key, null));
        }
        catch (JsonException)
        {
            return null;
        }
    }

    public void Save(string key, IEnumerable<GridColumnOrder> orders) =>
        Preferences.Default.Set(key, ColumnSettingsCodec.Write(orders));

    public void Remove(string key) => Preferences.Default.Remove(key);
}
