# ClamGrid - MAUI grid view rendered with SkiaSharp

[![NuGet](https://img.shields.io/nuget/v/ClamGrid.svg)](https://www.nuget.org/packages/ClamGrid/)

## What is this?

A .NET MAUI grid view for Android that draws cells with SkiaSharp.
Layout, sorting, selection and touch handling are implemented in C#, so the grid stays responsive with tens of thousands of rows.

| Selection, sorting and boolean editing | Conditional colors | Columns declared in XAML |
|:-:|:-:|:-:|
| <img src="Document/list.png" width="240" /> | <img src="Document/color.png" width="240" /> | <img src="Document/ticket.png" width="240" /> |

## Quick Start

Call `UseSkiaSharp()` when building the application.

```csharp
var builder = MauiApp.CreateBuilder()
    .UseMauiApp<App>()
    .UseSkiaSharp();
```

Define columns with typed value accessors and connect a data view.

```csharp
var grid = new ClamGridView();
grid.ConfigureColumns(
[
    new GridColumn("name", "Name", new GridValueAccessor<Customer, string>(static x => x.Name)) { Width = GridColumnWidth.Star(), MinWidth = 120 },
    new GridColumn("number", "Number", new GridValueAccessor<Customer, string>(static x => x.Number)) { Width = GridColumnWidth.Absolute(160), Alignment = TextAlignment.End },
    new GridColumn("done", "Done", new GridValueAccessor<Customer, bool>(static x => x.IsDone, static (x, value) => x.IsDone = value)) { IsBoolean = true, IsReadOnly = false }
]);

// Customer.Id is a unique and immutable key
var view = new GridDataView<Customer>(customers, static x => x.Id);
view.RegisterSort("name", static x => x.Name, StringComparer.CurrentCulture);
view.RegisterSort("number", static x => x.Number, StringComparer.Ordinal);

grid.ItemsSource = view;
grid.SelectionMode = GridSelectionMode.MultipleToggle;
grid.CellTapped += (_, e) => Debug.WriteLine(e.Hit);
```

Columns can also be declared in XAML. Columns without a `ValueAccessor` are resolved by `Key` from `ValueAccessors`, so the value access stays typed and trimming safe.

```xml
<clam:ClamGridView ItemsSource="{Binding Customers}"
                   ValueAccessors="{x:Static local:Customer.Accessors}"
                   ColumnOrders="{Binding ColumnOrders}"
                   SelectionMode="MultipleToggle">
    <clam:GridColumn Key="name" Header="Name" Width="*" MinWidth="120" />
    <clam:GridColumn Key="number" Header="Number" Width="160" Alignment="End" />
    <clam:GridColumn Key="done" Header="Done" IsBoolean="True" IsReadOnly="False" />
</clam:ClamGridView>
```

```csharp
// Customer.Accessors
public static GridValueAccessorCollection<Customer> Accessors { get; } = new()
{
    { "name", static x => x.Name },
    { "number", static x => x.Number },
    { "done", static x => x.IsDone, static (x, value) => x.IsDone = value }
};
```

`GridStyle` can be a XAML resource as well (`<clam:GridStyle x:Key="ListStyle" FontFamily="monospace" ShowRowHeaders="False" />`). Treat a style as immutable once assigned and replace it with a `with` expression to change it.

The grid scrolls by itself, so place it where it receives a definite size (for example a `*` row of a `Grid`).
Tapping a column header sorts by the registered key, tapping a row toggles the selection, and a long press selects or clears all rows.

## Supported features

| Category | Detail |
|---|---|
| **Columns** | Auto / Absolute / Star width, minimum width, alignment, static header and cell colors |
| **Column settings** | Visibility and order, edit session for a settings screen, drag to resize |
| **Data** | `INotifyCollectionChanged` / `INotifyPropertyChanged` tracking, stable row keys |
| **Sorting** | Multi-key sort with history, direction aware comparers, sort callback |
| **Selection** | None / single / multiple toggle, select all, selection by key |
| **Editing** | Boolean cell toggle |
| **Row dragging** | Reorder rows by dragging the row header |
| **Input** | Tap, long press, pan with inertia, column resize, commands for MVVM |
| **Styling** | `GridStyle` with font, padding, colors and conditional color callbacks. Characters missing from the font (emoji and so on) fall back to system fonts per character |
| **Accessibility** | Android virtual views for headers and cells |

## ClamGridView API

### Properties

| Name | Type | Default | Description |
|---|---|---|---|
| `ItemsSource` | `IEnumerable?` | `null` | Rows. An `IGridDataView` is used as is, other sources are wrapped in an owned `GridDataView<object>`. Bindable. |
| `DataView` | `IGridDataView?` | | Data view that owns the rows, selection and sort state. |
| `Columns` | `GridColumnCollection` | | Visible columns in display order. |
| `ColumnDefinitions` | `GridColumnCollection` | | All column definitions including hidden columns. Content property, so columns can be declared as XAML children. Changes rebuild `Columns` with the current `ColumnOrders`. |
| `ValueAccessors` | `IGridValueAccessorProvider?` | `null` | Resolves the value accessor of columns declared without one by `Key`. `GridValueAccessorCollection<T>` is the typed implementation. Bindable. |
| `ColumnOrders` | `IReadOnlyList<GridColumnOrder>` | `[]` | Visibility and order of all columns. Set `null` to restore the default. The value is normalized and written back, so a `TwoWay` binding (the default) receives the normalized orders. Bindable. |
| `GridStyle` | `GridStyle` | `new()` | Font, padding, sizes and colors. Bindable. |
| `SelectionMode` | `GridSelectionMode` | `MultipleToggle` | Selection behavior. Bindable. |
| `RowCount` | `int` | | Number of rows. |
| `SelectedCount` | `int` | | Number of selected rows. |
| `SelectedItems` | `IReadOnlyList<object>` | | Selected rows in display order. |
| `IsReadOnly` | `bool` | `true` | Default for boolean cell editing. `GridColumn.IsReadOnly` takes precedence. Bindable. |
| `AllowColumnResizing` | `bool` | `true` | Enables dragging the header boundary. Bindable. |
| `AllowColumnConfiguration` | `bool` | `true` | Enables the column configuration request by a long press on a header. Bindable. |
| `AllowRowDragging` | `bool` | `false` | Enables row reordering by dragging the row header. Bindable. |
| `RowMover` | `IGridRowMover?` | `null` | Applies row moves to the source collection. Bindable. |
| `CellTappedCommand` | `ICommand?` | `null` | Executed after `CellTapped` with `GridCellEventArgs`. Bindable. |
| `CellLongPressedCommand` | `ICommand?` | `null` | Executed after `CellLongPressed` with `GridCellEventArgs`. Bindable. |
| `SelectAllCommand` | `ICommand?` | `null` | Replaces the default bulk selection. Parameter is `bool` (select or clear). Bindable. |
| `ColumnConfigurationCommand` | `ICommand?` | `null` | Executed after `ColumnConfigurationRequested` with `GridColumnConfigurationEventArgs`. `CreateEditSession()` on the argument creates the settings copy. Bindable. |
| `CellValueChangedCommand` | `ICommand?` | `null` | Executed after `CellValueChanged` with `GridCellValueEventArgs`. Bindable. |
| `ColumnWidthChangedCommand` | `ICommand?` | `null` | Executed after `ColumnWidthChanged` with `GridColumnWidthEventArgs`. Bindable. |
| `RowMovedCommand` | `ICommand?` | `null` | Executed after `RowMoved` with `GridRowMoveEventArgs`. Bindable. |
| `AutoMeasureRowLimit` | `int` | `32` | Number of rows measured for Auto width columns. |
| `ScrollX` | `double` | | Horizontal scroll offset in DIP. |
| `ScrollY` | `double` | | Vertical scroll offset in DIP. |
| `InputState` | `GridGestureState` | | Current gesture state. |
| `IsInertiaRunning` | `bool` | | Whether inertial scrolling is in progress. |

### Methods

| Name | Returns | Description |
|---|---|---|
| `ConfigureColumns(IEnumerable<GridColumn> definitions, IEnumerable<GridColumnOrder>? orders = null)` | `void` | Replaces all column definitions and applies the saved visibility and order. |
| `ApplyColumnOrders(IEnumerable<GridColumnOrder>? orders)` | `void` | Applies visibility and order to the current definitions. `null` restores the default. |
| `CreateColumnEditSession()` | `GridColumnEditSession` | Creates an editable copy of the column settings. `GridColumnEditSession.Create(columns, orders)` does the same without the view. |
| `IsSelected(int rowIndex)` | `bool` | Whether the row is selected. |
| `SetSelected(int rowIndex, bool value)` | `bool` | Selects or clears the row. |
| `TryToggleSelection(int rowIndex, out bool isSelected)` | `bool` | Toggles the row selection. |
| `TryToggleSelectionByKey(object key, out bool isSelected)` | `bool` | Toggles the selection by row key and scrolls the row into view. |
| `SelectAll()` | `void` | Selects all rows. |
| `ClearSelection()` | `void` | Clears the selection. |
| `SortByColumn(int columnIndex)` | `GridSortResult` | Sorts by the column key, toggling the direction of the primary key. |
| `Refresh()` | `void` | Rebuilds the rows from the source. |
| `RefreshRows(int startIndex, int count)` | `bool` | Redraws values and colors of the rows. |
| `ScrollTo(double x, double y)` | `void` | Scrolls to the offset. |
| `ScrollBy(double x, double y)` | `void` | Scrolls by the delta. |
| `ScrollIntoView(int rowIndex, int columnIndex)` | `bool` | Scrolls until the cell is visible. |
| `HitTest(double x, double y)` | `GridHit` | Resolves the cell at the position in DIP. |
| `CancelInteraction()` | `void` | Stops the current gesture and inertia. |
| `InvalidateSurface()` | `void` | Redraws the grid. |
| `Dispose()` | `void` | Releases the data view subscriptions, timers and native resources. |

### Events

| Name | EventArgs | Description |
|---|---|---|
| `CellTapped` | `GridCellEventArgs` | Cell, header or corner tapped. Set `Handled` to suppress the command and the default action. |
| `CellLongPressed` | `GridCellEventArgs` | Cell or header long pressed. |
| `CellValueChanging` | `GridCellValueEventArgs` | Boolean cell is about to change. Cancelable. |
| `CellValueChanged` | `GridCellValueEventArgs` | Boolean cell changed. |
| `SelectionChanged` | `EventArgs` | Selection changed. |
| `SortRequested` | `GridSortRequestedEventArgs` | Sort is about to run. Cancelable. |
| `SortChanged` | `EventArgs` | Sort orders changed. |
| `SortFailed` | `GridSortFailedEventArgs` | Sort failed and the previous rows are kept. |
| `ColumnWidthChanging` | `GridColumnWidthEventArgs` | Column resize is about to be applied. Cancelable. |
| `ColumnWidthChanged` | `GridColumnWidthEventArgs` | Column width changed. |
| `ColumnConfigurationRequested` | `GridColumnConfigurationEventArgs` | Column configuration requested by a long press on a header. |
| `RowMoveRequested` | `GridRowMoveEventArgs` | Row drag is about to be applied. Cancelable. |
| `RowMoved` | `GridRowMoveEventArgs` | Row moved. |
| `FrameRendered` | `GridFrameEventArgs` | Render statistics of a frame. |

## Dependencies

- [SkiaSharp](https://github.com/mono/SkiaSharp)
