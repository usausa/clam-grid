namespace ClamGrid.Platform;

using Android.Views;

using AndroidX.Core.View;

using ClamGrid.Accessibility;

internal sealed class GridPlatformBridge : IGridPlatformBridge
{
    private readonly View view;
    private readonly ClamGridView grid;
    private readonly GridAccessibilityHelper accessibility;
    private ViewTreeObserver? windowObserver;

    public GridPlatformBridge(View view, ClamGridView grid)
    {
        this.view = view;
        this.grid = grid;
        accessibility = new GridAccessibilityHelper(view, grid);
        ViewCompat.SetAccessibilityDelegate(view, accessibility);
        view.Hover += OnHover;
        view.ViewAttachedToWindow += OnAttached;
        view.ViewDetachedFromWindow += OnDetached;
        if (view.IsAttachedToWindow)
        {
            AttachWindowObserver();
        }
    }

    void IGridPlatformBridge.InvalidateAccessibility() => accessibility.InvalidateRoot();

    void IGridPlatformBridge.PruneAccessibility() => accessibility.Prune();

    void IGridPlatformBridge.SetParentIntercept(bool allow) => view.Parent?.RequestDisallowInterceptTouchEvent(!allow);

    public void Dispose()
    {
        DetachWindowObserver();
        view.Hover -= OnHover;
        view.ViewAttachedToWindow -= OnAttached;
        view.ViewDetachedFromWindow -= OnDetached;
        ViewCompat.SetAccessibilityDelegate(view, null);
        accessibility.Dispose();
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
        // Viewを外すとViewTreeObserverが変わるため、購読した元のObserverから解除する。
        if (windowObserver is { IsAlive: true } observer)
        {
            observer.WindowFocusChange -= OnWindowFocusChange;
        }

        windowObserver = null;
    }

    private void OnHover(object? sender, View.HoverEventArgs e)
    {
        if (e.Event is { } motion)
        {
            e.Handled = accessibility.DispatchHoverEvent(motion);
        }
    }

    private void OnWindowFocusChange(object? sender, ViewTreeObserver.WindowFocusChangeEventArgs e)
    {
        if (!e.HasFocus)
        {
            grid.CancelInput(true);
        }
    }
}
