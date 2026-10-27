namespace ClamGrid.Tests;

using Microsoft.Maui.Dispatching;

internal sealed class TestDispatcherProvider : IDispatcherProvider
{
    [ThreadStatic]
    private static TestDispatcher? current;

    public static TestDispatcherProvider Instance { get; } = new();

    public IDispatcher GetForCurrentThread() => current ??= new TestDispatcher();
}
