# ClamGrid API reference

## 📖 ClamGridView

### 🧩 Properties

| Name | Type | Default | Bindable | Description |
|---|---|---|:-:|---|
| `ItemsSource` | `IEnumerable?` | `null` | ✓ | Rows; an `IGridDataView` is used as is, other sources are wrapped in an owned `GridDataView<object>` |
| `DataView` | `IGridDataView?` | | | Data view that owns rows, selection and sort state |
| `Columns` | `GridColumnCollection` | | | Visible columns in display order |
| `ColumnDefinitions` | `GridColumnCollection` | | | All definitions including hidden columns, declared as XAML content |
| `ValueAccessors` | `IGridValueAccessorProvider?` | `null` | ✓ | Resolves the accessor of columns declared without one by `Key` |
| `ColumnOrders` | `IReadOnlyList<GridColumnOrder>` | `[]` | ✓ TwoWay | Visibility and order of all columns; `null` restores the default and the normalized value is written back |
| `FrozenColumnCount` | `int` | `0` | ✓ | Leading columns that stay in place while the others scroll |
| `SortOrders` | `IReadOnlyList<GridSortOrder>` | `[]` | ✓ TwoWay | Sort keys and directions; applied to the data view and written back after every sort, an empty list clears the sort and `null` leaves the view unchanged |
| `SortCycle` | `GridSortCycle` | `AscendingDescending` | ✓ | Direction sequence of repeated header taps; the `None` variants remove the key on the third tap |
| `GridStyle` | `GridStyle` | `new()` | ✓ | Font, sizes and colors |
| `SelectionMode` | `GridSelectionMode` | `MultipleToggle` | ✓ | Selection behavior |
| `RowCount` | `int` | | | Number of rows |
| `SelectedCount` | `int` | | | Number of selected rows |
| `SelectedItems` | `IReadOnlyList<object>` | | | Selected rows in display order |
| `IsReadOnly` | `bool` | `true` | ✓ | Default for boolean cell editing; `GridColumn.IsReadOnly` wins |
| `AllowColumnResizing` | `bool` | `true` | ✓ | Drag the header boundary to resize |
| `AllowColumnConfiguration` | `bool` | `true` | ✓ | Long press on a header requests the column configuration |
| `AllowRowDragging` | `bool` | `false` | ✓ | Drag the row header to reorder rows |
| `RowMover` | `IGridRowMover?` | `null` | ✓ | Applies row moves to the source collection |
| `CellTappedCommand` | `ICommand?` | `null` | ✓ | Runs after `CellTapped` with `GridCellEventArgs` |
| `CellLongPressedCommand` | `ICommand?` | `null` | ✓ | Runs after `CellLongPressed` with `GridCellEventArgs` |
| `SelectAllCommand` | `ICommand?` | `null` | ✓ | Replaces the bulk selection; the parameter is `bool` (select or clear) |
| `ColumnConfigurationCommand` | `ICommand?` | `null` | ✓ | Runs after `ColumnConfigurationRequested` with `GridColumnConfigurationEventArgs` |
| `CellValueChangedCommand` | `ICommand?` | `null` | ✓ | Runs after `CellValueChanged` with `GridCellValueEventArgs` |
| `ColumnWidthChangedCommand` | `ICommand?` | `null` | ✓ | Runs after `ColumnWidthChanged` with `GridColumnWidthEventArgs` |
| `RowMovedCommand` | `ICommand?` | `null` | ✓ | Runs after `RowMoved` with `GridRowMoveEventArgs` |
| `AutoMeasureRowLimit` | `int` | `32` | | Rows measured for `Auto` width columns |
| `ScrollX`, `ScrollY` | `double` | | | Scroll offsets in DIP |
| `InputState` | `GridGestureState` | | | Current gesture state |
| `IsInertiaRunning` | `bool` | | | Inertial scrolling in progress |

### 🛠 Methods

| Name | Returns | Description |
|---|---|---|
| `ConfigureColumns(IEnumerable<GridColumn> definitions, IEnumerable<GridColumnOrder>? orders = null)` | `void` | Replaces all column definitions and applies the saved visibility and order |
| `ApplyColumnOrders(IEnumerable<GridColumnOrder>? orders)` | `void` | Applies visibility and order to the current definitions; `null` restores the default |
| `CreateColumnEditSession()` | `GridColumnEditSession` | Creates an editable copy of the column settings; `GridColumnEditSession.Create(columns, orders)` does the same without the view |
| `IsSelected(int rowIndex)` | `bool` | Whether the row is selected |
| `SetSelected(int rowIndex, bool value)` | `bool` | Selects or clears the row |
| `TryToggleSelection(int rowIndex, out bool isSelected)` | `bool` | Toggles the row selection |
| `TryToggleSelectionByKey(object key, out bool isSelected)` | `bool` | Toggles the selection by row key and scrolls the row into view |
| `SelectAll()` | `void` | Selects all rows |
| `ClearSelection()` | `void` | Clears the selection |
| `SortByColumn(int columnIndex)` | `GridSortResult` | Sorts by the column key following `SortCycle` |
| `Refresh()` | `void` | Rebuilds the rows from the source |
| `RefreshRows(int startIndex, int count)` | `bool` | Redraws values and colors of the rows |
| `ScrollTo(double x, double y)` | `void` | Scrolls to the offset |
| `ScrollBy(double x, double y)` | `void` | Scrolls by the delta |
| `ScrollIntoView(int rowIndex, int columnIndex)` | `bool` | Scrolls until the cell is visible |
| `HitTest(double x, double y)` | `GridHit` | Resolves the cell at the position in DIP |
| `CancelInteraction()` | `void` | Stops the current gesture and inertia |
| `InvalidateSurface()` | `void` | Redraws the grid |
| `Dispose()` | `void` | Releases the data view subscriptions, timers and native resources |

### 📣 Events

| Name | EventArgs | Description |
|---|---|---|
| `CellTapped` | `GridCellEventArgs` | Cell, header or corner tapped; `Handled` suppresses the command and the default action |
| `CellLongPressed` | `GridCellEventArgs` | Cell or header long pressed |
| `CellValueChanging` | `GridCellValueEventArgs` | Boolean cell is about to change (cancelable) |
| `CellValueChanged` | `GridCellValueEventArgs` | Boolean cell changed |
| `SelectionChanged` | `EventArgs` | Selection changed |
| `SortRequested` | `GridSortRequestedEventArgs` | Sort is about to run (cancelable) |
| `SortChanged` | `EventArgs` | Sort orders changed |
| `SortFailed` | `GridSortFailedEventArgs` | Sort failed and the previous rows are kept |
| `ColumnWidthChanging` | `GridColumnWidthEventArgs` | Column resize is about to be applied (cancelable) |
| `ColumnWidthChanged` | `GridColumnWidthEventArgs` | Column width changed |
| `ColumnConfigurationRequested` | `GridColumnConfigurationEventArgs` | Column configuration requested by a long press on a header |
| `RowMoveRequested` | `GridRowMoveEventArgs` | Row drag is about to be applied (cancelable) |
| `RowMoved` | `GridRowMoveEventArgs` | Row moved |
| `FrameRendered` | `GridFrameEventArgs` | Render statistics of a frame |

## 📐 GridColumn

| Name | Type | Default | Description |
|---|---|---|---|
| `Key` | `string` | `""` | Unique identifier used by `ColumnOrders`, `ValueAccessors` and sorting |
| `Header` | `string` | `""` | Header text |
| `ValueAccessor` | `IGridValueAccessor` | unresolved | Typed getter and optional setter; resolved by `Key` from `ValueAccessors` when omitted |
| `Width` | `GridColumnWidth` | `Auto` | `Auto`, `Absolute(dip)` or `Star(weight)`; in XAML `Auto`, `85`, `*` or `2*` |
| `MinWidth` | `double` | `40` | Lower bound for resizing and star distribution |
| `Alignment` | `TextAlignment` | `Start` | Cell text alignment |
| `Format` | `string?` | `null` | .NET format string applied to `IFormattable` values (`N0`, `D6`, `yyyy/MM/dd`) |
| `Converter` | `IValueConverter?` | `null` | Runs on the cell value before `Format` when a text cell is painted; boolean cells, sorting and editing use the raw value |
| `HeaderBackground`, `HeaderTextColor`, `TextColor`, `Background` | `Color?` | `null` | Static colors; `null` falls back to `GridStyle` |
| `IsBoolean` | `bool` | `false` | Draws a check box and toggles the value on tap |
| `IsReadOnly` | `bool?` | `null` | Overrides `ClamGridView.IsReadOnly` for the column |
| `SortKey` | `string?` | `null` | Sort key registered on the data view when it differs from `Key` |
| `AllowSorting` | `bool` | `true` | A header tap sorts the column |
| `AllowResizing` | `bool` | `true` | The header boundary can be dragged |

## 🎨 GridStyle

A style can be a XAML resource.  
Treat it as immutable once assigned and replace it with a `with` expression to change it.  
The default colors follow the Material palette: Blue 700 header, Cyan 700 and Orange 700 for the sorted column, Blue 100 selection and Gray tints.  

| Name | Type | Default | Description |
|---|---|---|---|
| `FontFamily` | `string` | `monospace` | Primary font; missing characters are resolved through `GridFonts` |
| `FontSize` | `float` | `16` | Font size in DIP |
| `HorizontalPadding`, `VerticalPadding` | `float` | `8` | Cell padding in DIP |
| `RowHeight`, `HeaderHeight` | `double?` | `null` | `null` derives the height from the primary font and the fallback fonts resolved while measuring |
| `RowHeaderWidth` | `double` | `48` | Width of the row header |
| `ShowColumnHeaders`, `ShowRowHeaders`, `ShowVerticalLines` | `bool` | `true` | Visibility of the headers and vertical lines |
| `CornerText` | `string` | `#` | Text of the corner cell above the row headers |
| `RowHeaderAlignment` | `TextAlignment` | `End` | Alignment of the row header text |
| `TextColor`, `Background`, `HeaderBackground`, `HeaderTextColor`, `RowHeaderBackground`, `GridLineColor`, `SelectedBackground`, `SelectedTextColor` | `Color` | | Base colors |
| `FrozenLineColor` | `Color` | `#9E9E9E` | Separator on the right edge of the frozen columns |
| `AscendingHeaderBackground`, `DescendingHeaderBackground` | `Color` | | Header background of the primary sort key |
| `AscendingSortMark`, `DescendingSortMark` | `string` | `↑`, `↓` | Text drawn next to the header of a sorted column; kept when the header text is truncated |
| `SortMarkPosition` | `GridSortMarkPosition` | `Start` | `Start` draws the mark before the header text, `End` after it |
| `ShowSortPriority` | `bool` | `false` | With several sort keys, marks the secondary keys too and appends the priority (`▲2`) |
| `RowBackground` | `Func<object, Color?>?` | `null` | Row background by item |
| `RowHeaderText` | `Func<GridRowHeaderTextContext, string?>?` | `null` | Row header text by item and selection state; `null` falls back to the row number |
| `CellColors`, `ColumnHeaderColors`, `RowHeaderColors` | callback | `null` | Per cell and per header colors with the item, column, sort state and default colors in the context |

## 🔤 GridFonts

Global font fallback settings, read when a grid creates its renderer.  
Set them at startup, before the first grid is shown.  
Emoji presentation sequences (a character followed by U+FE0F) are matched against the emoji font first.  
Variation selectors and zero width joiners only steer the font choice and are not drawn.  

| Name | Type | Default | Description |
|---|---|---|---|
| `Languages` | `IReadOnlyList<string>` | `["ja"]` | BCP-47 tags for the per character system font lookup; the tag selects the glyph variant of shared CJK ideographs |
| `Fallbacks` | `IReadOnlyList<SKTypeface>` | `[]` | Typefaces tried before the system lookup, for example a bundled font; they also count toward the automatic row height and stay owned by the caller |

```csharp
GridFonts.Languages = ["ja", "en"];
GridFonts.Fallbacks = [SKTypeface.FromStream(await FileSystem.OpenAppPackageFileAsync("NotoSansJP-Regular.ttf"))];
```

## 🗂 GridDataView&lt;T&gt;

| Member | Description |
|---|---|
| `GridDataView(IEnumerable source, Func<T, object?>? keySelector)` | Wraps the rows; the key keeps the selection stable across sorting and refreshes, and `INotifyCollectionChanged` / `INotifyPropertyChanged` sources are tracked |
| `Count`, `this[int]`, `SelectedCount`, `SelectedItems`, `SortOrders`, `SelectionMode` | State for binding and logic |
| `SetSelected`, `TryToggleSelection`, `SelectAll`, `ClearSelection`, `UpdateSelection(predicate)` | Selection API |
| `RegisterSort(key, selector, comparer)`, `RegisterComparer(key, comparison)`, `SetSortCallback(keys, callback)` | Sort key registration; the callback variant delegates the sorting itself, for example to a database query |
| `SortBy(key, cycle)`, `RestoreSortOrders(orders)`, `SaveSortOrders()` | Sort by a key following a `GridSortCycle` (ascending then descending when omitted) and persist the sort state |
| `SetSource(rows)`, `Refresh()`, `Suspend()` / `Resume()` | Replace the rows, rebuild them or batch changes |
| `Changed`, `SelectionChanged`, `SortRequested`, `SortChanged`, `SortFailed` | Events with the same meaning as on the view |
