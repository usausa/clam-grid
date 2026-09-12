namespace Example.Messaging;

using Smart.Mvvm.Messaging;

public sealed class GridSelectEventArgs(int index) : EventArgs
{
    public int Index { get; } = index;

    public bool IsSelected { get; set; }
}

// ViewModelから行の選択切替を要求し、切替後の状態を受け取る。
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
