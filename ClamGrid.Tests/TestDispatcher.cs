namespace ClamGrid.Tests;

using Microsoft.Maui.Dispatching;

internal sealed class TestDispatcher : IDispatcher
{
    private readonly int threadId = Environment.CurrentManagedThreadId;

    public bool IsDispatchRequired => Environment.CurrentManagedThreadId != threadId;

    public bool Dispatch(Action action)
    {
        action();
        return true;
    }

    public bool DispatchDelayed(TimeSpan delay, Action action) => Dispatch(action);

    public IDispatcherTimer CreateTimer() => new TestDispatcherTimer();
}
