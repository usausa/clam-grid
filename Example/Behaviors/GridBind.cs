namespace Example.Behaviors;

using Example.Messaging;

using Smart.Maui.Interactivity;

public static class GridBind
{
    public static readonly BindableProperty ControllerProperty = BindableProperty.CreateAttached(
        "Controller",
        typeof(GridController),
        typeof(GridBind),
        null,
        propertyChanged: BindChanged);

    public static GridController GetController(BindableObject bindable) =>
        (GridController)bindable.GetValue(ControllerProperty);

    public static void SetController(BindableObject bindable, GridController value) =>
        bindable.SetValue(ControllerProperty, value);

    private static void BindChanged(BindableObject bindable, object? oldValue, object? newValue)
    {
        if (bindable is not ClamGridView grid)
        {
            return;
        }

        if (oldValue is not null)
        {
            var behavior = grid.Behaviors.FirstOrDefault(static x => x is GridBindBehavior);
            if (behavior is not null)
            {
                grid.Behaviors.Remove(behavior);
            }
        }

        if (newValue is not null)
        {
            grid.Behaviors.Add(new GridBindBehavior());
        }
    }

    private sealed class GridBindBehavior : BehaviorBase<ClamGridView>
    {
        private GridController? controller;

        protected override void OnAttachedTo(ClamGridView bindable)
        {
            base.OnAttachedTo(bindable);

            controller = GetController(bindable);
            if (controller is not null)
            {
                bindable.ConfigureColumns(controller.Columns, controller.ColumnOrders);
                controller.ColumnOrders = bindable.ColumnOrders;
                controller.PropertyChanged += ControllerOnPropertyChanged;
                controller.ScrollRequest += ControllerOnScrollRequest;
                controller.InvalidateRequest += ControllerOnInvalidateRequest;
            }

            bindable.CellTapped += GridOnCellTapped;
            bindable.CellLongPressed += GridOnCellLongPressed;
            bindable.CellValueChanged += GridOnCellValueChanged;
            bindable.ColumnWidthChanged += GridOnColumnWidthChanged;
            bindable.RowMoved += GridOnRowMoved;
            bindable.FrameRendered += GridOnFrameRendered;
        }

        protected override void OnDetachingFrom(ClamGridView bindable)
        {
            if (controller is not null)
            {
                controller.PropertyChanged -= ControllerOnPropertyChanged;
                controller.ScrollRequest -= ControllerOnScrollRequest;
                controller.InvalidateRequest -= ControllerOnInvalidateRequest;
            }

            bindable.CellTapped -= GridOnCellTapped;
            bindable.CellLongPressed -= GridOnCellLongPressed;
            bindable.CellValueChanged -= GridOnCellValueChanged;
            bindable.ColumnWidthChanged -= GridOnColumnWidthChanged;
            bindable.RowMoved -= GridOnRowMoved;
            bindable.FrameRendered -= GridOnFrameRendered;

            controller = null;

            base.OnDetachingFrom(bindable);
        }

        private void ControllerOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if ((e.PropertyName != nameof(GridController.ColumnOrders)) || (controller is null) || (AssociatedObject is not { } grid))
            {
                return;
            }

            var orders = controller.ColumnOrders;
            if ((orders is not null) && orders.SequenceEqual(grid.ColumnOrders))
            {
                return;
            }

            grid.ApplyColumnOrders(orders);
            controller.ColumnOrders = grid.ColumnOrders;
        }

        private void ControllerOnScrollRequest(object? sender, GridScrollRequestEventArgs e)
        {
            if (AssociatedObject is not { } grid)
            {
                return;
            }

            if (e.RowIndex >= 0)
            {
                grid.ScrollIntoView(e.RowIndex, e.ColumnIndex);
            }
            else
            {
                grid.ScrollBy(e.DeltaX, e.DeltaY);
            }
        }

        private void ControllerOnInvalidateRequest(object? sender, EventArgs e)
        {
            AssociatedObject?.InvalidateSurface();
        }

        private void GridOnCellTapped(object? sender, GridCellEventArgs e) => controller?.HandleCellTapped(e);

        private void GridOnCellLongPressed(object? sender, GridCellEventArgs e) => controller?.HandleCellLongPressed(e);

        private void GridOnCellValueChanged(object? sender, GridCellValueEventArgs e) => controller?.HandleCellValueChanged(e);

        private void GridOnColumnWidthChanged(object? sender, GridColumnWidthEventArgs e) => controller?.HandleColumnWidthChanged(e);

        private void GridOnRowMoved(object? sender, GridRowMoveEventArgs e) => controller?.HandleRowMoved(e);

        private void GridOnFrameRendered(object? sender, GridFrameEventArgs e) => controller?.HandleFrameRendered(e);
    }
}
