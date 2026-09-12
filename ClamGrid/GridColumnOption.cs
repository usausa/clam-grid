namespace ClamGrid;

using System.ComponentModel;

public sealed class GridColumnOption : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    public string Key { get; }

    public string Header { get; }

    public bool IsVisible
    {
        get;
        set
        {
            if (field != value)
            {
                field = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsVisible)));
            }
        }
    }

    public GridColumnOption(string key, string header, bool isVisible)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentNullException.ThrowIfNull(header);
        Key = key;
        Header = header;
        IsVisible = isVisible;
    }
}
