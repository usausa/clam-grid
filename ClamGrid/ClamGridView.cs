namespace ClamGrid;

using System.Collections;
using System.Diagnostics;

using ClamGrid.Layout;
using ClamGrid.Platform;
using ClamGrid.Rendering;

using SkiaSharp;
using SkiaSharp.Views.Maui;
using SkiaSharp.Views.Maui.Controls;

public partial class ClamGridView : SKCanvasView, IDisposable
{
    public static readonly BindableProperty ItemsSourceProperty = BindableProperty.Create(nameof(ItemsSource), typeof(IEnumerable), typeof(ClamGridView), propertyChanged: OnItemsSourceChanged);
    public static readonly BindableProperty GridStyleProperty = BindableProperty.Create(nameof(GridStyle), typeof(GridStyle), typeof(ClamGridView), defaultValueCreator: static _ => new GridStyle(), propertyChanged: OnStyleChanged);
    public static readonly BindableProperty SelectionModeProperty = BindableProperty.Create(nameof(SelectionMode), typeof(GridSelectionMode), typeof(ClamGridView), GridSelectionMode.MultipleToggle, propertyChanged: OnSelectionModeChanged);

    private static readonly SKColor AccentColor = new(0x25, 0x63, 0xEB);

    private GridDataView<object>? ownedDataView;
    private bool dataSubscribed;
    private long inputGeneration;
    private long lastResetVersion;
    private GridRenderer? renderer;
    private IGridPlatformBridge? platform;
    private bool disposed;
    private bool layoutDirty = true;

    public event EventHandler<GridCellEventArgs>? CellTapped;

    public event EventHandler<GridFrameEventArgs>? FrameRendered;

    public event EventHandler? SelectionChanged;

    public event EventHandler<GridSortRequestedEventArgs>? SortRequested;

    public event EventHandler? SortChanged;

    public event EventHandler<GridSortFailedEventArgs>? SortFailed;

    public IEnumerable? ItemsSource
    {
        get => (IEnumerable?)GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    public GridStyle GridStyle
    {
        get => (GridStyle)GetValue(GridStyleProperty);
        set => SetValue(GridStyleProperty, value);
    }

    public GridSelectionMode SelectionMode
    {
        get => DataView?.SelectionMode ?? (GridSelectionMode)GetValue(SelectionModeProperty);
        set => SetValue(SelectionModeProperty, value);
    }

    public GridColumnCollection Columns { get; } = [];

    public IGridDataView? DataView { get; private set; }

    public int RowCount => Items.Count;

    public int SelectedCount => DataView?.SelectedCount ?? 0;

    public IReadOnlyList<object> SelectedItems => DataView?.SelectedItems ?? Array.Empty<object>();

    public int AutoMeasureRowLimit
    {
        get;
        set
        {
            ArgumentOutOfRangeException.ThrowIfNegative(value);
            RequireUiThread();
            field = value;
            InvalidateLayout();
        }
    }

    public double ScrollX => CurrentLayout?.ScrollX ?? 0;

    public double ScrollY => CurrentLayout?.ScrollY ?? 0;

    private IReadOnlyList<object> Items => DataView?.Items ?? Array.Empty<object>();

    private GridLayout? CurrentLayout { get; set; }

    public ClamGridView()
    {
        AutoMeasureRowLimit = 32;
        BackgroundColor = Colors.White;
        EnableTouchEvents = true;
        Columns.Changed += OnColumnsChanged;
        ColumnDefinitions.Changed += OnColumnDefinitionsChanged;
        Touch += OnGridTouch;
        SizeChanged += OnSizeChanged;
        Unloaded += OnUnloaded;
        SetDataSource(null);
    }

    //--------------------------------------------------------------------------------
    // Platform
    //--------------------------------------------------------------------------------

    partial void AttachPlatform();

    private void DetachPlatform()
    {
        platform?.Dispose();
        platform = null;
    }

    private void ResetPlatform()
    {
        DetachPlatform();
        AttachPlatform();
    }

    private void SetParentIntercept(bool allow) => platform?.SetParentIntercept(allow);

    //--------------------------------------------------------------------------------
    // Lifecycle
    //--------------------------------------------------------------------------------

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposing || disposed)
        {
            return;
        }

        DataView?.CancelPendingSort();
        disposed = true;
        UnsubscribeData();
        ownedDataView?.Dispose();
        ownedDataView = null;
        CancelInput(true);
        if (inputTimer is not null)
        {
            inputTimer.Tick -= OnInputTick;
            inputTimer = null;
        }

        DetachPlatform();
        renderer?.Dispose();
        renderer = null;
        Columns.Changed -= OnColumnsChanged;
        ColumnDefinitions.Changed -= OnColumnDefinitionsChanged;
        Touch -= OnGridTouch;
        SizeChanged -= OnSizeChanged;
        Unloaded -= OnUnloaded;
    }

    protected override void OnHandlerChanging(HandlerChangingEventArgs args)
    {
        if (!disposed)
        {
            DataView?.CancelPendingSort();
        }

        UnsubscribeData();
        ownedDataView?.Suspend();
        CancelInput(true);
        DetachPlatform();
        renderer?.Dispose();
        renderer = null;
        layoutDirty = true;
        base.OnHandlerChanging(args);
    }

    protected override void OnHandlerChanged()
    {
        base.OnHandlerChanged();
        if (!disposed && (Handler is not null))
        {
            ownedDataView?.Resume();
            if ((DataView is { } view) && (lastResetVersion != view.ResetVersion))
            {
                CurrentLayout = null;
                lastResetVersion = view.ResetVersion;
            }

            SubscribeData();
            layoutDirty = true;
            NotifyDataProperties();
        }

        AttachPlatform();
        InvalidateSurface();
    }

    protected override void OnPropertyChanged(string? propertyName = null)
    {
        base.OnPropertyChanged(propertyName);
        if ((propertyName is nameof(IsEnabled) or nameof(IsVisible)) && (!IsEnabled || !IsVisible))
        {
            CancelInput(true);
        }
    }

    //--------------------------------------------------------------------------------
    // Selection
    //--------------------------------------------------------------------------------

    public bool IsSelected(int rowIndex) => DataView?.IsSelected(rowIndex) ?? false;

    public bool SetSelected(int rowIndex, bool value)
    {
        RequireUiThread();
        return DataView?.SetSelected(rowIndex, value) ?? false;
    }

    public bool TryToggleSelection(int rowIndex, out bool isSelected)
    {
        RequireUiThread();
        isSelected = false;
        return DataView?.TryToggleSelection(rowIndex, out isSelected) ?? false;
    }

    public bool TryToggleSelectionByKey(object key, out bool isSelected)
    {
        var row = DataView?.IndexOfKey(key) ?? -1;
        if (!TryToggleSelection(row, out isSelected))
        {
            return false;
        }

        ScrollIntoView(row, 0);
        return true;
    }

    public void SelectAll()
    {
        RequireUiThread();
        DataView?.SelectAll();
    }

    public void ClearSelection()
    {
        RequireUiThread();
        DataView?.ClearSelection();
    }

    //--------------------------------------------------------------------------------
    // Sort
    //--------------------------------------------------------------------------------

    public GridSortResult SortByColumn(int columnIndex)
    {
        RequireUiThread();
        if ((columnIndex < 0) || (columnIndex >= Columns.Count) || !Columns[columnIndex].AllowSorting || (DataView is null))
        {
            return new GridSortResult(GridSortStatus.Rejected, Array.Empty<string>());
        }

        var column = Columns[columnIndex];
        return DataView.SortBy(column.SortKey ?? column.Key);
    }

    //--------------------------------------------------------------------------------
    // Scroll
    //--------------------------------------------------------------------------------

    public void Refresh()
    {
        RequireUiThread();
        DataView?.Refresh();
        if (!dataSubscribed)
        {
            InvalidateLayout();
        }
    }

    public void ScrollBy(double x, double y) => ScrollTo(ScrollX + x, ScrollY + y);

    public void ScrollTo(double x, double y)
    {
        RequireUiThread();
        CancelInput();
        ScrollCore(x, y);
    }

    public bool ScrollIntoView(int rowIndex, int columnIndex)
    {
        RequireUiThread();
        CancelInput();
        EnsureLayout();
        if (!(CurrentLayout?.ScrollIntoView(rowIndex, columnIndex) ?? false))
        {
            return false;
        }

        InvalidateSurface();
        return true;
    }

    public GridHit HitTest(double x, double y)
    {
        RequireUiThread();
        EnsureLayout();
        return CurrentLayout?.HitTest(x, y) ?? GridHit.None;
    }

    //--------------------------------------------------------------------------------
    // Render
    //--------------------------------------------------------------------------------

    protected override void OnPaintSurface(SKPaintSurfaceEventArgs e)
    {
        var started = Stopwatch.GetTimestamp();
        var measurements = renderer?.Measurements ?? 0;
        EnsureLayout();
        if ((CurrentLayout is not { } current) || (renderer is not { } currentRenderer) || (Width <= 0) || (Height <= 0))
        {
            e.Surface.Canvas.Clear();
            return;
        }

        var canvas = e.Surface.Canvas;
        canvas.Save();
        int cells;
        try
        {
            canvas.Scale((float)(e.Info.Width / Width), (float)(e.Info.Height / Height));
            cells = currentRenderer.Render(canvas, current, Columns, Items, DataView, AllowRowDragging && (RowMover is not null));
            DrawResizeIndicator(canvas, current);
            DrawRowDragIndicator(canvas, current);
        }
        finally
        {
            canvas.Restore();
        }

        base.OnPaintSurface(e);
        FrameRendered?.Invoke(this, new GridFrameEventArgs(Stopwatch.GetElapsedTime(started).TotalMilliseconds, cells, currentRenderer.Measurements - measurements, current.VisibleRows, current.VisibleColumns, current.ScrollX, current.ScrollY));
    }

    //--------------------------------------------------------------------------------
    // Internal
    //--------------------------------------------------------------------------------

    internal bool ActivateCell(GridHit hit, Point point, bool longPress = false)
    {
        if (disposed || !IsEnabled || (hit.CellType == GridCellType.None) || (hit.RowIndex >= Items.Count) || (hit.ColumnIndex >= Columns.Count))
        {
            return false;
        }

        var item = (hit.RowIndex >= 0) && (hit.RowIndex < Items.Count) ? Items[hit.RowIndex] : null;
        if ((hit.CellType == GridCellType.Cell) && (item is not null) && ActivateBoolean(hit, item, longPress))
        {
            return true;
        }

        var key = (hit.ColumnIndex >= 0) && (hit.ColumnIndex < Columns.Count) ? Columns[hit.ColumnIndex].Key : null;
        var generation = inputGeneration;
        var args = new GridCellEventArgs(hit, point, item, key);
        if (longPress)
        {
            CellLongPressed?.Invoke(this, args);
        }
        else
        {
            CellTapped?.Invoke(this, args);
        }

        if (!args.Handled && (generation == inputGeneration))
        {
            var command = longPress ? CellLongPressedCommand : CellTappedCommand;
            if ((command?.CanExecute(args) ?? false) && (generation == inputGeneration) && !args.Handled)
            {
                args.Handled = true;
                command.Execute(args);
            }
        }

        if (!args.Handled && (generation == inputGeneration))
        {
            if (longPress)
            {
                ActivateLongPressDefault(args, generation);
            }
            else if ((hit.CellType == GridCellType.Cell) || (hit.CellType == GridCellType.RowHeader))
            {
                TryToggleSelection(hit.RowIndex, out _);
            }
            else if (hit.CellType == GridCellType.ColumnHeader)
            {
                SortByColumn(hit.ColumnIndex);
            }
        }

        return true;
    }

    private void ActivateLongPressDefault(GridCellEventArgs args, long generation)
    {
        if ((args.Hit.CellType == GridCellType.ColumnHeader) && AllowColumnConfiguration)
        {
            var request = new GridColumnConfigurationEventArgs(Array.AsReadOnly(columnCatalog), args.ColumnKey, Array.AsReadOnly(columnOrders));
            ColumnConfigurationRequested?.Invoke(this, request);
            if (!request.Handled && (generation == inputGeneration) && (ColumnConfigurationCommand is { } command) && command.CanExecute(request) && (generation == inputGeneration) && !request.Handled)
            {
                request.Handled = true;
                command.Execute(request);
            }
        }
        else if ((args.Hit.CellType is GridCellType.Cell or GridCellType.RowHeader) && (SelectionMode == GridSelectionMode.MultipleToggle))
        {
            var select = !IsSelected(args.Hit.RowIndex);
            if (SelectAllCommand is { } command)
            {
                if (command.CanExecute(select) && (generation == inputGeneration))
                {
                    command.Execute(select);
                }
            }
            else if (select)
            {
                SelectAll();
            }
            else
            {
                ClearSelection();
            }
        }
    }

    private void ScrollCore(double x, double y)
    {
        EnsureLayout();
        if (CurrentLayout?.ScrollTo(x, y) ?? false)
        {
            InvalidateSurface();
        }
    }

    private void SetDataSource(IEnumerable? source)
    {
        GridDataView<object>? owned = null;
        IGridDataView replacement;
        if (source is IGridDataView existing)
        {
            replacement = existing;
        }
        else
        {
            owned = new GridDataView<object>(source ?? Array.Empty<object>()) { SelectionMode = (GridSelectionMode)GetValue(SelectionModeProperty) };
            replacement = owned;
            if (Handler is null)
            {
                owned.Suspend();
            }
        }

        UnsubscribeData();
        ownedDataView?.Dispose();
        ownedDataView = owned;
        DataView = replacement;
        lastResetVersion = replacement.ResetVersion;
        SetValue(SelectionModeProperty, replacement.SelectionMode);
        if (Handler is not null)
        {
            SubscribeData();
        }

        CurrentLayout = null;
        CancelInput();
        renderer?.ClearCache();
        InvalidateLayout();
        ResetPlatform();
        NotifyDataProperties();
        OnPropertyChanged(nameof(DataView));
        SelectionChanged?.Invoke(this, EventArgs.Empty);
    }

    private static void OnItemsSourceChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var grid = (ClamGridView)bindable;
        grid.RequireUiThread();
        grid.SetDataSource((IEnumerable?)newValue);
    }

    private static void OnSelectionModeChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var grid = (ClamGridView)bindable;
        grid.RequireUiThread();
        if ((grid.DataView is { } view) && (view.SelectionMode != (GridSelectionMode)newValue))
        {
            view.SelectionMode = (GridSelectionMode)newValue;
        }
    }

    private static void OnStyleChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var grid = (ClamGridView)bindable;
        grid.RequireUiThread();
        grid.renderer?.Dispose();
        grid.renderer = null;
        grid.CancelInput();
        grid.InvalidateLayout();
    }

    private void SubscribeData()
    {
        if (!dataSubscribed && (DataView is { } view))
        {
            view.Changed += OnDataChanged;
            view.SortRequested += OnSortRequested;
            view.SortChanged += OnSortChanged;
            view.SortFailed += OnSortFailed;
            dataSubscribed = true;
        }
    }

    private void UnsubscribeData()
    {
        if (dataSubscribed && (DataView is { } view))
        {
            view.Changed -= OnDataChanged;
            view.SortRequested -= OnSortRequested;
            view.SortChanged -= OnSortChanged;
            view.SortFailed -= OnSortFailed;
        }

        dataSubscribed = false;
    }

    private void OnDataChanged(object? sender, GridDataChangedEventArgs e)
    {
        RequireUiThread();
        if (e.Kind == GridDataChangeKind.Reset)
        {
            lastResetVersion = DataView?.ResetVersion ?? 0;
            CurrentLayout = null;
            ResetPlatform();
        }

        if (e.Kind != GridDataChangeKind.Selection)
        {
            CancelInput();
            renderer?.ClearCache();
            InvalidateLayout();
        }

        SetValue(SelectionModeProperty, SelectionMode);
        NotifyDataProperties();
        InvalidateSurface();
        if (e.SelectionChanged)
        {
            SelectionChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    private void NotifyDataProperties()
    {
        OnPropertyChanged(nameof(RowCount));
        OnPropertyChanged(nameof(SelectedCount));
        OnPropertyChanged(nameof(SelectedItems));
        OnPropertyChanged(nameof(SelectionMode));
    }

    private void OnSortRequested(object? sender, GridSortRequestedEventArgs e) => SortRequested?.Invoke(this, e);

    private void OnSortChanged(object? sender, EventArgs e) => SortChanged?.Invoke(this, e);

    private void OnSortFailed(object? sender, GridSortFailedEventArgs e) => SortFailed?.Invoke(this, e);

    private void OnColumnsChanged(object? sender, EventArgs e)
    {
        RequireUiThread();
        SynchronizeColumnCatalog();
        CancelInput();
        renderer?.ClearCache();
        InvalidateLayout();
    }

    private void OnSizeChanged(object? sender, EventArgs e)
    {
        CancelInput();
        InvalidateLayout();
    }

    private void OnUnloaded(object? sender, EventArgs e) => CancelInput(true);

    private void InvalidateLayout()
    {
        layoutDirty = true;
        InvalidateSurface();
    }

    private void EnsureLayout()
    {
        if (disposed || (Width <= 0) || (Height <= 0) || (!layoutDirty && (CurrentLayout is not null)))
        {
            return;
        }

        renderer ??= new GridRenderer(GridStyle);
        var rowHeader = GridStyle.ShowRowHeaders ? GridStyle.RowHeaderWidth : 0;
        var widths = renderer.MeasureColumns(Columns, Items, Math.Max(0, Width - rowHeader), AutoMeasureRowLimit, DataView);
        if ((resizeColumn >= 0) && (resizeColumn < widths.Length))
        {
            widths[resizeColumn] = previewWidth;
        }

        var replacement = new GridLayout(widths, Items.Count, GridStyle.RowHeight ?? renderer.AutoRowHeight, GridStyle.ShowColumnHeaders ? GridStyle.HeaderHeight ?? renderer.AutoRowHeight : 0, rowHeader, Width, Height);
        replacement.ScrollTo(ScrollX, ScrollY);
        CurrentLayout = replacement;
        layoutDirty = false;
    }

    private void RequireUiThread()
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        if (Dispatcher.IsDispatchRequired)
        {
            throw new InvalidOperationException("Grid mutations must run on the UI thread.");
        }
    }
}
