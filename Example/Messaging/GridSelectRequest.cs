namespace Example.Messaging;

using Smart.Mvvm.Messaging;

public sealed class GridSelectEventArgs(int index) : EventArgs
{
    public int Index { get; } = index;

    public bool IsSelected { get; set; }
}

// Requests a row selection toggle from the view model and receives the resulting state
public sealed class GridSelectRequest : IEventRequest<GridSelectEventArgs>
{
    public event EventHandler<GridSelectEventArgs>? Requested;

    public bool Select(int index)
    {
        var args = new GridSelectEventArgs(index);
        Requested?.Invoke(this, args);
        return args.IsSelected;
    }
}
