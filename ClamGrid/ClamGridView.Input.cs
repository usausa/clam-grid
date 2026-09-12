namespace ClamGrid;

using System.Diagnostics;
using System.Windows.Input;

using ClamGrid.Internal.Input;
using ClamGrid.Internal.Layout;

using Microsoft.Maui.Dispatching;

using SkiaSharp;
using SkiaSharp.Views.Maui;

public partial class ClamGridView
{
    public static readonly BindableProperty CellTappedCommandProperty = BindableProperty.Create(nameof(CellTappedCommand), typeof(ICommand), typeof(ClamGridView));
    public static readonly BindableProperty CellLongPressedCommandProperty = BindableProperty.Create(nameof(CellLongPressedCommand), typeof(ICommand), typeof(ClamGridView));
    public static readonly BindableProperty SelectAllCommandProperty = BindableProperty.Create(nameof(SelectAllCommand), typeof(ICommand), typeof(ClamGridView));
    public static readonly BindableProperty ColumnConfigurationCommandProperty = BindableProperty.Create(nameof(ColumnConfigurationCommand), typeof(ICommand), typeof(ClamGridView));
    public static readonly BindableProperty ColumnWidthChangedCommandProperty = BindableProperty.Create(nameof(ColumnWidthChangedCommand), typeof(ICommand), typeof(ClamGridView));
    public static readonly BindableProperty AllowColumnResizingProperty = BindableProperty.Create(nameof(AllowColumnResizing), typeof(bool), typeof(ClamGridView), true, propertyChanged: OnInputOptionChanged);
    public static readonly BindableProperty AllowColumnConfigurationProperty = BindableProperty.Create(nameof(AllowColumnConfiguration), typeof(bool), typeof(ClamGridView), true, propertyChanged: OnInputOptionChanged);

    private readonly GridGestureController gesture = new();
    private readonly GridInertia inertia = new();
    private IDispatcherTimer? inputTimer;
    private long lastInputTick;
    private Point pressPoint;
    private GridHit pressHit;
    private int resizeColumn = -1;
    private double originalWidth;
    private double previewWidth;

    public event EventHandler<GridCellEventArgs>? CellLongPressed;

    public event EventHandler<GridColumnConfigurationEventArgs>? ColumnConfigurationRequested;

    public event EventHandler<GridColumnWidthEventArgs>? ColumnWidthChanging;

    public event EventHandler<GridColumnWidthEventArgs>? ColumnWidthChanged;

    public ICommand? CellTappedCommand
    {
        get => (ICommand?)GetValue(CellTappedCommandProperty);
        set => SetValue(CellTappedCommandProperty, value);
    }

    public ICommand? CellLongPressedCommand
    {
        get => (ICommand?)GetValue(CellLongPressedCommandProperty);
        set => SetValue(CellLongPressedCommandProperty, value);
    }

    public ICommand? SelectAllCommand
    {
        get => (ICommand?)GetValue(SelectAllCommandProperty);
        set => SetValue(SelectAllCommandProperty, value);
    }

    public ICommand? ColumnConfigurationCommand
    {
        get => (ICommand?)GetValue(ColumnConfigurationCommandProperty);
        set => SetValue(ColumnConfigurationCommandProperty, value);
    }

    public ICommand? ColumnWidthChangedCommand
    {
        get => (ICommand?)GetValue(ColumnWidthChangedCommandProperty);
        set => SetValue(ColumnWidthChangedCommandProperty, value);
    }

    public bool AllowColumnResizing
    {
        get => (bool)GetValue(AllowColumnResizingProperty);
        set => SetValue(AllowColumnResizingProperty, value);
    }

    public bool AllowColumnConfiguration
    {
        get => (bool)GetValue(AllowColumnConfigurationProperty);
        set => SetValue(AllowColumnConfigurationProperty, value);
    }

    public GridGestureState InputState => gesture.State;

    public bool IsInertiaRunning => inertia.IsRunning;

    private static double InputTime => Stopwatch.GetTimestamp() * 1000d / Stopwatch.Frequency;

    public void CancelInteraction()
    {
        RequireUiThread();
        CancelInput();
    }

    public bool RefreshRows(int startIndex, int count)
    {
        RequireUiThread();
        if ((startIndex < 0) || (count <= 0) || (startIndex >= RowCount) || (count > RowCount - startIndex))
        {
            return false;
        }

        CancelInput();
        renderer?.ClearCache();
        if (startIndex < AutoMeasureRowLimit)
        {
            InvalidateLayout();
        }

        InvalidateSurface();
        InvalidateAccessibility();
        return true;
    }

    private static void OnInputOptionChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var grid = (ClamGridView)bindable;
        grid.CancelInteraction();
        grid.InvalidateSurface();
        grid.InvalidateAccessibility();
    }

    private void OnGridTouch(object? sender, SKTouchEventArgs e)
    {
        if (disposed || !IsEnabled || (Width <= 0) || (Height <= 0) || (CanvasSize.Width <= 0) || (CanvasSize.Height <= 0))
        {
            return;
        }

        var transform = new GridCoordinateTransform(CanvasSize.Width / Width, CanvasSize.Height / Height);
        var point = transform.ToDip(e.Location.X, e.Location.Y);
        e.Handled = true;
        switch (e.ActionType)
        {
            case SKTouchAction.Pressed:
                StopInertia();
                if (gesture.State == GridGestureState.Idle)
                {
                    pressPoint = point;
                    pressHit = HitTest(point.X, point.Y);
                    PrepareRowDrag();
                    resizeColumn = AllowColumnResizing ? CurrentLayout?.HitTestColumnBoundary(point.X, point.Y) ?? -1 : -1;
                    if ((resizeColumn >= 0) && !Columns[resizeColumn].AllowResizing)
                    {
                        resizeColumn = -1;
                    }

                    if (resizeColumn >= 0)
                    {
                        originalWidth = CurrentLayout!.GetColumnHeaderBounds(resizeColumn).Width;
                        previewWidth = originalWidth;
                    }
                }

                var axis = pressHit.CellType switch { GridCellType.ColumnHeader => GridPanAxis.Horizontal, GridCellType.RowHeader => GridPanAxis.Vertical, _ => GridPanAxis.Both };
                if (gesture.Press(e.Id, point, InputTime, resizeColumn >= 0, axis, dragRowKey is not null).Action == GridGestureAction.Canceled)
                {
                    CancelInput();
                }
                else
                {
                    StartInputTimer();
                    SetParentIntercept(false);
                }

                break;
            case SKTouchAction.Moved:
                if ((gesture.State == GridGestureState.Pressed) && !new GridRect(0, 0, Width, Height).Contains(point.X, point.Y))
                {
                    CancelInput();
                    break;
                }

                ApplyMovement(gesture.Move(e.Id, point, InputTime));
                break;
            case SKTouchAction.Released:
                ApplyMovement(gesture.Move(e.Id, point, InputTime));
                var inside = new GridRect(0, 0, Width, Height).Contains(point.X, point.Y);
                if (inside && (HitTest(point.X, point.Y) == pressHit))
                {
                    ApplyLongPress(gesture.Tick(InputTime));
                }

                var result = gesture.Release(e.Id, point, InputTime, inside && ((HitTest(point.X, point.Y) == pressHit) || (gesture.State == GridGestureState.Resizing) || ((gesture.State == GridGestureState.RowDragging) && (point.Y >= (CurrentLayout?.HeaderHeight ?? 0)))));
                var rowKey = dragRowKey;
                var from = dragFrom;
                var insertion = dragInsertion;
                ClearRowDrag();
                var column = resizeColumn;
                var width = previewWidth;
                var oldWidth = originalWidth;
                ClearResizePreview();
                StopInputTimer();
                SetParentIntercept(true);
                if (result.Action == GridGestureAction.Tap)
                {
                    ActivateCell(pressHit, point);
                }
                else if (result.Action == GridGestureAction.ResizeCompleted)
                {
                    CommitResize(column, oldWidth, width);
                }
                else if (result.Action == GridGestureAction.RowDragCompleted)
                {
                    CommitRowDrag(rowKey, from, insertion);
                }
                else if (result.Action == GridGestureAction.PanCompleted)
                {
                    inertia.Start(result.Velocity);
                    if (CurrentLayout is { } layout)
                    {
                        inertia.StopAtBounds(layout);
                    }

                    if (inertia.IsRunning)
                    {
                        StartInputTimer();
                    }
                }

                break;
            case SKTouchAction.Cancelled:
                CancelInput(true);
                break;
        }
    }

    private void ApplyMovement(GridGestureResult result)
    {
        if (result.Action == GridGestureAction.Pan)
        {
            ScrollCore(ScrollX + result.Delta.X, ScrollY + result.Delta.Y);
            StopInputTimer();
        }
        else if ((result.Action == GridGestureAction.Resize) && (resizeColumn >= 0))
        {
            previewWidth = Math.Max(Columns[resizeColumn].MinWidth, originalWidth + result.Delta.X);
            InvalidateLayout();
            StopInputTimer();
        }
        else if ((result.Action == GridGestureAction.RowDrag) && (dragRowKey is not null))
        {
            UpdateRowDrag(new Point(pressPoint.X + result.Delta.X, pressPoint.Y + result.Delta.Y));
        }
    }

    private void ApplyLongPress(GridGestureResult result)
    {
        if (result.Action == GridGestureAction.LongPress)
        {
            StopInputTimer();
            ActivateCell(pressHit, pressPoint, true);
        }
    }

    private void CommitResize(int column, double oldWidth, double newWidth)
    {
        if ((column < 0) || (column >= Columns.Count) || (Math.Abs(oldWidth - newWidth) < 0.01))
        {
            return;
        }

        var generation = inputGeneration;
        var args = new GridColumnWidthEventArgs(Columns[column].Key, column, oldWidth, newWidth);
        ColumnWidthChanging?.Invoke(this, args);
        if (args.Cancel || (generation != inputGeneration))
        {
            return;
        }

        Columns[column] = Columns[column] with { Width = GridColumnWidth.Absolute(newWidth) };
        EnsureLayout();
        ColumnWidthChanged?.Invoke(this, args);
        ExecuteCommand(ColumnWidthChangedCommand, args);
    }

    private static void ExecuteCommand(ICommand? command, object parameter)
    {
        if (command?.CanExecute(parameter) ?? false)
        {
            command.Execute(parameter);
        }
    }

    private void StartInputTimer()
    {
        if (inputTimer is null)
        {
            inputTimer = Dispatcher.CreateTimer();
            inputTimer.Interval = TimeSpan.FromMilliseconds(16);
            inputTimer.Tick += OnInputTick;
        }

        lastInputTick = Stopwatch.GetTimestamp();
        inputTimer.Start();
    }

    private void OnInputTick(object? sender, EventArgs e)
    {
        if (disposed || (Handler is null) || !IsVisible || !IsEnabled)
        {
            CancelInput();
            return;
        }

        ApplyLongPress(gesture.Tick(InputTime));
        if ((gesture.State == GridGestureState.RowDragging) && (CurrentLayout is { } dragLayout))
        {
            var now = Stopwatch.GetTimestamp();
            var elapsed = Math.Min(0.05, (now - lastInputTick) / (double)Stopwatch.Frequency);
            lastInputTick = now;
            ScrollCore(ScrollX, ScrollY + (GridRowDragLayout.GetScrollVelocity(dragLayout, dragPoint) * elapsed));
            dragInsertion = GridRowDragLayout.GetInsertionIndex(dragLayout, dragPoint.Y);
            InvalidateSurface();
            return;
        }

        if (inertia.IsRunning)
        {
            var now = Stopwatch.GetTimestamp();
            var delta = inertia.Step((now - lastInputTick) / (double)Stopwatch.Frequency);
            lastInputTick = now;
            ScrollCore(ScrollX + delta.X, ScrollY + delta.Y);
            if (CurrentLayout is { } layout)
            {
                inertia.StopAtBounds(layout);
            }
        }

        if (!inertia.IsRunning && (gesture.State != GridGestureState.Pressed))
        {
            StopInputTimer();
        }
    }

    private void StopInputTimer() => inputTimer?.Stop();

    private void StopInertia()
    {
        inertia.Stop();
        StopInputTimer();
    }

    private void ClearResizePreview()
    {
        if (resizeColumn >= 0)
        {
            resizeColumn = -1;
            InvalidateLayout();
        }
    }

    internal void CancelInput(bool releasePointers = false)
    {
        inputGeneration++;
        if (releasePointers)
        {
            gesture.Cancel();
        }
        else
        {
            gesture.Block();
        }

        StopInertia();
        ClearResizePreview();
        ClearRowDrag();
        SetParentIntercept(true);
    }

    private void DrawResizeIndicator(SKCanvas canvas, GridLayout layout)
    {
        if ((gesture.State == GridGestureState.Resizing) && (resizeColumn >= 0))
        {
            var edge = (float)layout.GetColumnHeaderBounds(resizeColumn).Right;
            using var paint = new SKPaint();
            paint.Color = AccentColor;
            paint.StrokeWidth = 3;
            paint.IsAntialias = true;
            canvas.DrawLine(edge, 0, edge, (float)layout.ViewportHeight, paint);
        }
    }
}
