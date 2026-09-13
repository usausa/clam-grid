namespace Example.Behaviors;

using Smart.Maui.Interactivity;
using Smart.Mvvm.Messaging;

// Bridges selection requests (GridSelectRequest) from the view model; selection mode and commands bind directly to ClamGridView
public sealed class GridSelectBehavior : BehaviorBase<ClamGridView>
{
    public static readonly BindableProperty RequestProperty = BindableProperty.Create(
        nameof(Request),
        typeof(IEventRequest<GridSelectEventArgs>),
        typeof(GridSelectBehavior),
        propertyChanged: HandleRequestPropertyChanged);

    public IEventRequest<GridSelectEventArgs>? Request
    {
        get => (IEventRequest<GridSelectEventArgs>?)GetValue(RequestProperty);
        set => SetValue(RequestProperty, value);
    }

    protected override void OnDetachingFrom(ClamGridView bindable)
    {
        if (Request is not null)
        {
            Request.Requested -= EventRequestOnRequested;
        }

        base.OnDetachingFrom(bindable);
    }

    private static void HandleRequestPropertyChanged(BindableObject bindable, object? oldValue, object? newValue)
    {
        ((GridSelectBehavior)bindable).OnRequestPropertyChanged(oldValue as IEventRequest<GridSelectEventArgs>, newValue as IEventRequest<GridSelectEventArgs>);
    }

    private void OnRequestPropertyChanged(IEventRequest<GridSelectEventArgs>? oldValue, IEventRequest<GridSelectEventArgs>? newValue)
    {
        if (oldValue == newValue)
        {
            return;
        }

        if (oldValue is not null)
        {
            oldValue.Requested -= EventRequestOnRequested;
        }

        if (newValue is not null)
        {
            newValue.Requested += EventRequestOnRequested;
        }
    }

    private void EventRequestOnRequested(object? sender, GridSelectEventArgs e)
    {
        if ((AssociatedObject is { } grid) && grid.TryToggleSelection(e.Index, out var selected))
        {
            e.IsSelected = selected;
            grid.ScrollIntoView(e.Index, 0);
        }
    }
}
