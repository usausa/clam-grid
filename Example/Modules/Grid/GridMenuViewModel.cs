namespace Example.Modules.Grid;

public sealed class GridMenuViewModel : AppViewModelBase
{
    public IObserveCommand ForwardCommand { get; }

    public GridMenuViewModel()
    {
        ForwardCommand = MakeAsyncCommand<ViewId>(x => Navigator.ForwardAsync(x));
    }

    protected override Task OnNotifyBackAsync()
    {
        AndroidHelper.MoveTaskToBack();
        return Task.CompletedTask;
    }

    protected override Task OnNotifyFunction1() => OnNotifyBackAsync();
}
