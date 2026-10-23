# ClamGrid API reference

## 📖 ClamGridView

### 🧩 Properties

| Name | Type | Default | Bindable | Description |
|---|---|---|:-:|---|
| `ItemsSource` | `IEnumerable?` | `null` | ✓ | Rows; an `IGridDataView` is used as is, other sources are wrapped in an owned `GridDataView<object>` |
| `DataView` | `IGridDataView?` | | | Data view that owns rows, selection and sort state |
| `ColumnDefinitions` | `GridColumnCollection` | | | All definitions including hidden columns, declared as XAML content |
| `ValueAccessors` | `IGridValueAccessorProvider?` | `null` | ✓ | Resolves the accessor of columns declared without one by `Key` |
| `ColumnStates` | `IReadOnlyList<GridColumnState>` | `[]` | ✓ TwoWay | Visibility, order and saved width of all columns; a dragged width is saved here instead of the definition, the normalized value is written back and `null` restores the defaults including the declared widths |
| `FrozenColumnCount` | `int` | `0` | ✓ | Leading columns that stay in place while the others scroll |
| `SortOrders` | `IReadOnlyList<GridSortOrder>` | `[]` | ✓ TwoWay | Sort keys and directions; applied to the data view and written back after every sort, an empty list clears the sort and `null` leaves the view unchanged |
| `SortCycle` | `GridSortCycle` | `AscendingDescending` | ✓ | Direction sequence of repeated header taps; the `None` variants remove the key on the third tap |
| `GridStyle` | `GridStyle` | `new()` | ✓ | Font, sizes and colors |
| `EmptyText` | `string?` | `null` | ✓ | Text drawn in the center of the body when there is no row; the headers stay, and `null` or an empty string draws nothing |
| `SelectionMode` | `GridSelectionMode` | `MultipleToggle` | ✓ | Selection behavior |
| `RowCount` | `int` | | | Number of rows |
| `SelectedCount` | `int` | | | Number of selected rows |
| `SelectedItems` | `IReadOnlyList<object>` | | | Selected rows in display order |
| `SelectedKeys` | `IReadOnlyList<object>` | `[]` | ✓ TwoWay | Keys of the selected rows in display order; applied to the data view and written back after every selection change, keys without a row are dropped, and `null` leaves the selection unchanged |
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
| `CreateColumnEditSession()` | `GridColumnEditSession` | Creates an editable copy of the column settings; `GridColumnEditSession.Create(columns, orders)` does the same without the view |
| `IsSelected(int rowIndex)` | `bool` | Whether the row is selected |
| `SetSelected(int rowIndex, bool value)` | `bool` | Selects or clears the row |
| `TryToggleSelection(int rowIndex, out bool isSelected)` | `bool` | Toggles the row selection |
| `TryToggleSelectionByKey(object key, out bool isSelected)` | `bool` | Toggles the selection by row key and scrolls the row into view without moving horizontally |
| `SelectAll()` | `void` | Selects all rows |
| `ClearSelection()` | `void` | Clears the selection |
| `SortByColumn(string columnKey)` | `GridSortResult` | Sorts by the column following `SortCycle`; rejected when the column is hidden or does not allow sorting |
| `Refresh()` | `void` | Rebuilds the rows from the source |
| `RefreshRows(int startIndex, int count)` | `bool` | Redraws values and colors of the rows |
| `ScrollTo(double x, double y)` | `void` | Scrolls to the offset |
| `ScrollIntoView(int rowIndex, string? columnKey = null, ScrollToPosition position = MakeVisible)` | `bool` | Places the row at `Start`, `Center` or `End`, or moves it just into view with `MakeVisible`; the column is revealed with the smallest move, and `null` or a hidden column keeps the horizontal offset |
| `ScrollIntoViewByKey(object key, ScrollToPosition position = MakeVisible)` | `bool` | Scrolls only vertically to the row with the key; `false` when no shown row has it |
| `HitTest(double x, double y)` | `GridHit` | Resolves the cell at the position in DIP; the result carries `ColumnKey` as well as the indexes |
| `CancelInteraction()` | `void` | Stops the current gesture and inertia |
| `ApplyStyle()` | `void` | Recreates the renderer and the layout and redraws after the content of `GridStyle` was changed in place |
| `InvalidateSurface()` | `void` | Redraws the grid |
| `Dispose()` | `void` | Releases the data view subscriptions, timers and native resources |

Scroll requests made before the grid has a size are kept, the latest one is applied by the first layout and the `ScrollIntoView` methods return `true` for them.  

The grid is used on the UI thread; the members that change it throw `InvalidOperationException` when they are called from another thread and `ObjectDisposedException` after `Dispose()`.  

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
| `Key` | `string` | `""` | Unique identifier used by `ColumnStates`, `ValueAccessors` and sorting |
| `Header` | `string` | `""` | Header text; line breaks (`&#10;` in XAML) split it into lines that are centered and truncated one by one |
| `ValueAccessor` | `IGridValueAccessor` | unresolved | Typed getter and optional setter; resolved by `Key` from `ValueAccessors` when omitted |
| `Width` | `GridColumnWidth` | `Auto` | `Auto`, `Absolute(dip)` or `Star(weight)`; in XAML `Auto`, `85`, `*` or `2*` |
| `MinWidth` | `double` | `40` | Lower bound for resizing and star distribution |
| `Alignment` | `TextAlignment` | `Start` | Cell text alignment |
| `Format` | `string?` | `null` | Composite format applied to any value when it contains `{` (`{0:N0} pcs`, `{0} san`), otherwise a .NET format string applied to `IFormattable` values (`N0`, `D6`, `yyyy/MM/dd`); parsed when assigned, so an invalid composite format throws `FormatException` there |
| `Converter` | `IValueConverter?` | `null` | Runs on the cell value before `Format` when a text cell is painted; boolean cells, sorting and editing use the raw value |
| `HeaderBackground`, `HeaderTextColor`, `TextColor`, `Background` | `Color?` | `null` | Static colors; `null` falls back to `GridStyle` |
| `IsBoolean` | `bool` | `false` | Draws a check box and toggles the value on tap |
| `IsReadOnly` | `bool?` | `null` | Overrides `ClamGridView.IsReadOnly` for the column |
| `SortKey` | `string?` | `null` | Sort key registered on the data view when it differs from `Key` |
| `AllowSorting` | `bool` | `true` | A header tap sorts the column |
| `AllowResizing` | `bool` | `true` | The header boundary can be dragged |

## 🧭 GridColumnState

An entry of `ColumnStates`, also produced by `GridColumnEditSession.Export()`.  

| Name | Type | Description |
|---|---|---|
| `Key` | `string` | Column key |
| `IsVisible` | `bool` | Whether the column is shown |
| `Width` | `double?` | Absolute width in DIP saved by a drag on the header boundary, kept while the column is hidden; `null` uses the width of the definition and `GridColumnOption.Width` edits it in a settings screen |

Changing `ColumnDefinitions` or `ColumnStates` is folded into one arrangement, which runs when the grid is drawn or when `ColumnStates` is read. The visible columns in display order are `ColumnStates` where `IsVisible`, and their effective width is `Width` of the state or the declared width of the definition when it is `null`.  

## 🎨 GridStyle

A style can be a XAML resource.  
Treat it as immutable once assigned and replace it with a `with` expression to change it, or call `ApplyStyle()` after changing it in place.  
Invalid sizes throw `ArgumentException` when the style is assigned or `ApplyStyle()` is called, and `null` throws `ArgumentNullException`.  
The default colors follow the Material palette: Blue 700 header, Cyan 700 and Orange 700 for the sorted column, Blue 100 selection and Gray tints.  

| Name | Type | Default | Description |
|---|---|---|---|
| `FontFamily` | `string` | `monospace` | Primary font; missing characters are resolved through `FallbackFonts` of SkiaExtension |
| `FontSize` | `double` | `16` | Font size in DIP |
| `HorizontalPadding`, `VerticalPadding` | `double` | `8` | Cell padding in DIP |
| `RowHeight`, `HeaderHeight` | `double?` | `null` | `null` derives the height from the primary font and the fallback fonts resolved while measuring; the header also counts the most lines among all column headers, hidden ones included, and the corner text |
| `RowHeaderWidth` | `double` | `48` | Width of the row header |
| `ColumnHeadersVisible`, `RowHeadersVisible`, `VerticalLinesVisible` | `bool` | `true` | Visibility of the headers and vertical lines |
| `ScrollBarVisible`, `ScrollBarThickness`, `ScrollBarColor` | `bool`, `double`, `Color` | `true`, `4`, `#8C64748B` | Scroll bars drawn over the right and bottom edges while the content overflows; the corner radius is half the thickness and the bar keeps a gap of one DIP from the edge |
| `CornerText` | `string` | `#` | Text of the corner cell above the row headers; line breaks work as in column headers |
| `RowHeaderAlignment` | `TextAlignment` | `End` | Alignment of the row header text |
| `TextColor`, `Background`, `HeaderBackground`, `HeaderTextColor`, `RowHeaderBackground`, `GridLineColor`, `SelectedBackground`, `SelectedTextColor` | `Color` | | Base colors |
| `AlternatingRowBackground` | `Color?` | `null` | Background of every second row in display order; the selection, `RowBackground` and the column background win, row headers keep their own color |
| `FrozenLineColor` | `Color` | `#9E9E9E` | Separator on the right edge of the frozen columns |
| `EmptyTextColor` | `Color` | `#9E9E9E` | Color of `ClamGridView.EmptyText` |
| `AscendingHeaderBackground`, `DescendingHeaderBackground` | `Color` | | Header background of the primary sort key |
| `AscendingSortMark`, `DescendingSortMark` | `string` | `↑`, `↓` | Text drawn next to the header of a sorted column; kept when the header text is truncated |
| `SortMarkPosition` | `GridSortMarkPosition` | `Start` | `Start` draws the mark before the header text, `End` after it |
| `SortPriorityVisible` | `bool` | `false` | With several sort keys, marks the secondary keys too and appends the priority (`▲2`) |
| `RowBackground` | `Func<object, Color?>?` | `null` | Row background by item |
| `RowHeaderText` | `Func<GridRowHeaderTextContext, string?>?` | `null` | Row header text by item and selection state; `null` falls back to the row number |
| `CellColors`, `ColumnHeaderColors`, `RowHeaderColors` | callback | `null` | Per cell and per header colors with the item, column, sort state and default colors in the context |

## 🔤 Fonts

Per character font fallback comes from [SkiaExtension](https://github.com/usausa/skia-extension).  
Configure it with `FallbackFonts` in the `SkiaExtension` namespace at startup, before the first grid is shown.  
`Languages` defaults to the UI language of the device.  
The `Typefaces` also count toward the automatic row height.  

```csharp
FallbackFonts.Languages = ["ja", "en"];
FallbackFonts.Typefaces = [SKTypeface.FromStream(await FileSystem.OpenAppPackageFileAsync("NotoSansJP-Regular.ttf"))];
```

## 🗂 GridDataView&lt;T&gt;

| Member | Description |
|---|---|
| `GridDataView(IEnumerable source, Func<T, object?>? keySelector)` | Wraps the rows; the selection follows the key, so it survives sorting, refreshes and a reload of the rows, and `INotifyCollectionChanged` / `INotifyPropertyChanged` sources are tracked |
| `Count`, `this[int]`, `SourceCount`, `SelectedCount`, `SelectedItems`, `SelectedKeys`, `SortOrders`, `SelectionMode` | State for binding and logic; `Count` and the indexes cover the rows the filter keeps, `SourceCount` all rows |
| `Filter` | `Func<T, bool>?` that hides the rows it rejects; it runs again on source and row changes, hidden rows lose their selection, and it survives `SetSource` like the sort. Hidden rows keep their place in the sort, so sort callbacks receive them as well |
| `SetSelected`, `TryToggleSelection`, `SetSelectedKeys(keys)`, `SelectAll`, `ClearSelection`, `UpdateSelection(predicate)` | Selection API; `SetSelectedKeys` replaces the selection with the rows of the given keys, drops the keys without a row, and returns `false` without applying anything while a notification is running |
| `RegisterSort(key, selector, comparer)`, `RegisterComparer(key, comparison)`, `SetSortCallback(keys, callback)` | Sort key registration; the callback variant delegates the sorting itself, for example to a database query |
| `SortBy(key, cycle)`, `RestoreSortOrders(orders)` | Sort by a key following a `GridSortCycle` (ascending then descending when omitted) and restore a saved sort state |
| `SetSource(rows)`, `Refresh()`, `Suspend()` / `Resume()` | Replace the rows, rebuild them or batch changes; the selection keeps every row whose key is still there, so reloading the rows as new instances of the same keys keeps it |
| `Changed`, `SelectionChanged`, `SortRequested`, `SortChanged`, `SortFailed` | Events with the same meaning as on the view |

The data view belongs to the thread that created it, and that is where the bound collection has to be changed as well; the members that change it throw `InvalidOperationException` when they are called from another thread and `ObjectDisposedException` after `Dispose()`.  
A change made from inside one of its own events is refused as well: most members throw `InvalidOperationException`, while `SetSelectedKeys` returns `false` and `RestoreSortOrders` returns a `Rejected` result. Defer it until the notification returns.  
