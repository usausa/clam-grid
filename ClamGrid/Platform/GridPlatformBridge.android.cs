namespace ClamGrid.Platform;

using Android.Views;

internal sealed class GridPlatformBridge : IGridPlatformBridge
{
    private readonly View view;
    private readonly ClamGridView grid;
    private ViewTreeObserver? windowObserver;

    public GridPlatformBridge(View view, ClamGridView grid)
    {
        this.view = view;
        this.grid = grid;
        view.ViewAttachedToWindow += OnAttached;
        view.ViewDetachedFromWindow += OnDetached;
        if (view.IsAttachedToWindow)
        {
            AttachWindowObserver();
        }
    }

    void IGridPlatformBridge.SetParentIntercept(bool allow) => view.Parent?.RequestDisallowInterceptTouchEvent(!allow);

    public void Dispose()
    {
        DetachWindowObserver();
        view.ViewAttachedToWindow -= OnAttached;
        view.ViewDetachedFromWindow -= OnDetached;
    }

    private void OnAttached(object? sender, View.ViewAttachedToWindowEventArgs e) => AttachWindowObserver();

    private void OnDetached(object? sender, View.ViewDetachedFromWindowEventArgs e)
    {
        DetachWindowObserver();
        grid.CancelInput(true);
    }

    private void AttachWindowObserver()
    {
        DetachWindowObserver();
        if (view.ViewTreeObserver is { IsAlive: true } observer)
        {
            windowObserver = observer;
            observer.WindowFocusChange += OnWindowFocusChange;
        }
    }

    private void DetachWindowObserver()
    {
        // Detaching the view swaps its ViewTreeObserver, so unsubscribe from the observer that was subscribed
        if (windowObserver is { IsAlive: true } observer)
        {
            observer.WindowFocusChange -= OnWindowFocusChange;
        }

        windowObserver = null;
    }

    private void OnWindowFocusChange(object? sender, ViewTreeObserver.WindowFocusChangeEventArgs e)
    {
        if (!e.HasFocus)
        {
            grid.CancelInput(true);
        }
    }
}
