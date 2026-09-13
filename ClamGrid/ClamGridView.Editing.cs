namespace ClamGrid;

using System.Windows.Input;

using ClamGrid.Layout;

using SkiaSharp;

public partial class ClamGridView
{
    public static readonly BindableProperty IsReadOnlyProperty = BindableProperty.Create(nameof(IsReadOnly), typeof(bool), typeof(ClamGridView), true, propertyChanged: OnInputOptionChanged);
    public static readonly BindableProperty AllowRowDraggingProperty = BindableProperty.Create(nameof(AllowRowDragging), typeof(bool), typeof(ClamGridView), false, propertyChanged: OnInputOptionChanged);
    public static readonly BindableProperty RowMoverProperty = BindableProperty.Create(nameof(RowMover), typeof(IGridRowMover), typeof(ClamGridView), propertyChanged: OnInputOptionChanged);
    public static readonly BindableProperty CellValueChangedCommandProperty = BindableProperty.Create(nameof(CellValueChangedCommand), typeof(ICommand), typeof(ClamGridView));
    public static readonly BindableProperty RowMovedCommandProperty = BindableProperty.Create(nameof(RowMovedCommand), typeof(ICommand), typeof(ClamGridView));

    private object? dragRowKey;
    private int dragFrom = -1;
    private int dragInsertion = -1;
    private Point dragPoint;

    public event EventHandler<GridCellValueEventArgs>? CellValueChanging;

    public event EventHandler<GridCellValueEventArgs>? CellValueChanged;

    public event EventHandler<GridRowMoveEventArgs>? RowMoveRequested;

    public event EventHandler<GridRowMoveEventArgs>? RowMoved;

    public bool IsReadOnly
    {
        get => (bool)GetValue(IsReadOnlyProperty);
        set => SetValue(IsReadOnlyProperty, value);
    }

    public bool AllowRowDragging
    {
        get => (bool)GetValue(AllowRowDraggingProperty);
        set => SetValue(AllowRowDraggingProperty, value);
    }

    public IGridRowMover? RowMover
    {
        get => (IGridRowMover?)GetValue(RowMoverProperty);
        set => SetValue(RowMoverProperty, value);
    }

    public ICommand? CellValueChangedCommand
    {
        get => (ICommand?)GetValue(CellValueChangedCommandProperty);
        set => SetValue(CellValueChangedCommandProperty, value);
    }

    public ICommand? RowMovedCommand
    {
        get => (ICommand?)GetValue(RowMovedCommandProperty);
        set => SetValue(RowMovedCommandProperty, value);
    }

    internal bool CanEditBoolean(GridColumn column) => column.IsBoolean && !(column.IsReadOnly ?? IsReadOnly) && column.ValueAccessor.CanWrite && (column.ValueAccessor.ValueType == typeof(bool));

    private bool ActivateBoolean(GridHit hit, object item, bool longPress)
    {
        var column = Columns[hit.ColumnIndex];
        if (!column.IsBoolean)
        {
            return false;
        }

        if (longPress || !CanEditBoolean(column))
        {
            return true;
        }

        var generation = inputGeneration;
        var before = column.ValueAccessor.GetValue(item) is true;
        var args = new GridCellValueEventArgs(hit, item, column.Key, before, !before);
        CellValueChanging?.Invoke(this, args);
        if (args.Cancel || (generation != inputGeneration))
        {
            return true;
        }

        var view = DataView;
        var version = view?.Version;
        column.ValueAccessor.SetValue(item, !before);
        if (!disposed && ReferenceEquals(view, DataView) && (view?.Version == version))
        {
            view?.Refresh();
        }

        if (!disposed)
        {
            InvalidateSurface();
            CellValueChanged?.Invoke(this, args);
            ExecuteCommand(CellValueChangedCommand, args);
        }

        return true;
    }

    private void PrepareRowDrag()
    {
        if (AllowRowDragging && (pressHit.CellType == GridCellType.RowHeader) && (DataView is { } view) && (RowMover?.CanMove ?? false))
        {
            dragFrom = pressHit.RowIndex;
            dragRowKey = view.GetRowKey(dragFrom);
            dragPoint = pressPoint;
            dragInsertion = dragFrom;
        }
    }

    private void UpdateRowDrag(Point point)
    {
        dragPoint = point;
        if (CurrentLayout is { } layout)
        {
            dragInsertion = GridRowDragLayout.GetInsertionIndex(layout, point.Y);
        }

        InvalidateSurface();
        if (!(inputTimer?.IsRunning ?? false))
        {
            StartInputTimer();
        }
    }

    private void CommitRowDrag(object? key, int from, int insertion)
    {
        var target = GridRowDragLayout.GetTargetIndex(from, insertion, RowCount);
        TryCommitRowMove(key, from, target);
    }

    private bool TryCommitRowMove(object? key, int from, int target)
    {
        if ((key is null) || (target < 0) || (target >= RowCount) || (from == target) || (DataView?.IndexOfKey(key) != from) || !(RowMover?.CanMove ?? false))
        {
            return false;
        }

        var generation = inputGeneration;
        var args = new GridRowMoveEventArgs(key, from, target);
        RowMoveRequested?.Invoke(this, args);
        if (!args.Cancel && (generation == inputGeneration) && (RowMover?.Move(key, target) ?? false))
        {
            RowMoved?.Invoke(this, args);
            ExecuteCommand(RowMovedCommand, args);
            return true;
        }

        return false;
    }

    private void ClearRowDrag()
    {
        if (dragRowKey is not null)
        {
            dragRowKey = null;
            dragFrom = -1;
            dragInsertion = -1;
            InvalidateSurface();
        }
    }

    private void DrawRowDragIndicator(SKCanvas canvas, GridLayout layout)
    {
        if ((gesture.State != GridGestureState.RowDragging) || (dragFrom < 0) || (dragInsertion < 0))
        {
            return;
        }

        canvas.Save();
        canvas.ClipRect(new SKRect(0, (float)layout.HeaderHeight, (float)layout.ViewportWidth, (float)layout.ViewportHeight));
        using var paint = new SKPaint();
        paint.Color = AccentColor.WithAlpha(40);
        paint.IsAntialias = true;
        var sourceY = layout.HeaderHeight + (dragFrom * layout.RowHeight) - layout.ScrollY;
        canvas.DrawRect(0, (float)sourceY, (float)layout.ViewportWidth, (float)layout.RowHeight, paint);
        var y = (float)(layout.HeaderHeight + (dragInsertion * layout.RowHeight) - layout.ScrollY);
        paint.Color = AccentColor;
        paint.StrokeWidth = 4;
        canvas.DrawLine(0, y, (float)layout.ViewportWidth, y, paint);
        canvas.DrawCircle(8, y, 6, paint);
        canvas.Restore();
    }
}
