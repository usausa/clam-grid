namespace Example.Components;

using System.Text.Json;

using BunnyTail.DependencyInjection;

public interface IColumnSettingsStore
{
    GridColumnState[]? Load(string key);

    void Save(string key, IEnumerable<GridColumnState> orders);

    void Remove(string key);
}

[Singleton(As = typeof(IColumnSettingsStore))]
public sealed class ColumnSettingsStore : IColumnSettingsStore
{
    public GridColumnState[]? Load(string key)
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

    public void Save(string key, IEnumerable<GridColumnState> orders) =>
        Preferences.Default.Set(key, ColumnSettingsCodec.Write(orders));

    public void Remove(string key) => Preferences.Default.Remove(key);
}
