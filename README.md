# ClamGrid - Grid view for .NET MAUI Android

[![NuGet](https://img.shields.io/nuget/v/ClamGrid.svg)](https://www.nuget.org/packages/ClamGrid/)

## 🐚 What is this?

A .NET MAUI grid view for Android that draws cells with SkiaSharp.  
Layout, sorting, selection and touch handling run in C#, so the grid stays responsive with tens of thousands of rows.  

| 🖱 Selection, sorting and boolean editing | 🎨 Conditional colors | 🧾 Columns declared in XAML |
|:-:|:-:|:-:|
| <img src="Document/list.png" width="240" /> | <img src="Document/color.png" width="240" /> | <img src="Document/ticket.png" width="240" /> |

## 🚀 Quick Start

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
    new GridColumn("number", "Number", new GridValueAccessor<Customer, int>(static x => x.Number)) { Width = GridColumnWidth.Absolute(160), Alignment = TextAlignment.End, Format = "N0" },
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

Columns can also be declared in XAML.  
A column without `ValueAccessor` is resolved by `Key` from `ValueAccessors`, so the value access stays typed.  

```xml
<clam:ClamGridView ItemsSource="{Binding Customers}"
                   ValueAccessors="{x:Static local:Customer.Accessors}"
                   ColumnOrders="{Binding ColumnOrders}"
                   SelectionMode="MultipleToggle">
    <clam:GridColumn Key="name" Header="Name" Width="*" MinWidth="120" />
    <clam:GridColumn Key="number" Header="Number" Width="160" Alignment="End" Format="N0" />
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

The grid scrolls by itself, so give it a definite size such as a `*` row of a `Grid`.  
Tap a header to sort, tap a row to select it and long press to select or clear all rows.  

## 🔗 Binding and MVVM

Every setting a view model needs is a bindable property or a command.  
`Example/Modules/Ticket` is a complete screen built this way.  

```xml
<clam:ClamGridView ItemsSource="{Binding Items}"
                   ValueAccessors="{x:Static models:TicketRowAccessors.Ticket}"
                   ColumnOrders="{Binding ColumnOrders}"
                   SortOrders="{Binding SortOrders}"
                   FrozenColumnCount="1"
                   GridStyle="{StaticResource ListGridStyle}"
                   SelectionMode="MultipleToggle"
                   SelectAllCommand="{Binding SelectCommand}"
                   ColumnConfigurationCommand="{Binding ColumnEditCommand}">
    <clam:GridColumn Key="StatusMark" Header="Status" Width="45" AllowSorting="False" />
    <clam:GridColumn Key="ReceiptOrder" Header="Order" Width="95" Format="D6" />
    <clam:GridColumn Key="StartedDate" Header="Started" Width="80" Format="MM/dd" />
</clam:ClamGridView>
```

```csharp
public sealed class TicketListViewModel : ObservableObject
{
    private readonly IColumnSettingsStore store;

    // Rows, selection and sort state in one object
    public GridDataView<TicketRow> Items { get; } = new(Array.Empty<TicketRow>(), static x => x.Id);

    // TwoWay bound: the grid writes the normalized orders back, so persisting them in the setter is enough
    public IReadOnlyList<GridColumnOrder>? ColumnOrders
    {
        get;
        set
        {
            field = value;
            OnPropertyChanged();
            if (value is not null)
            {
                store.Save("columns", value);
            }
        }
    }

    // TwoWay bound as well: the grid writes the applied sort back after a header tap
    public IReadOnlyList<GridSortOrder>? SortOrders
    {
        get;
        set
        {
            field = value;
            OnPropertyChanged();
        }
    }

    public ICommand SelectCommand { get; }

    public ICommand ColumnEditCommand { get; }

    public TicketListViewModel(IColumnSettingsStore store)
    {
        this.store = store;
        ColumnOrders = store.Load("columns");
        SortOrders = [new GridSortOrder("ReceiptOrder")];
        Items.RegisterSort("ReceiptOrder", static x => x.ReceiptOrder);
        // Long press on a row: true selects, false clears
        SelectCommand = new Command<bool>(select => Items.UpdateSelection(x => select && !x.IsCompleted));
        // Long press on a header: open a settings page with an editable copy, assign session.Export() to ColumnOrders on return
        ColumnEditCommand = new Command<GridColumnConfigurationEventArgs>(e => OpenColumnSettings(e.CreateEditSession()));
    }
}
```

| Task | Binding |
|---|---|
| Rows | `ItemsSource` bound to a `GridDataView<T>`, which owns rows, selection and sort state |
| Columns | `GridColumn` children in XAML and `ValueAccessors` bound to a static `GridValueAccessorCollection<T>` |
| Column settings | `ColumnOrders` (TwoWay) and `ColumnConfigurationCommand`, whose argument creates the edit session for a settings page |
| Sort state | `SortOrders` (TwoWay) with the keys registered on the data view, and `SortCycle` for the header tap sequence |
| Selection | `SelectionMode`, `SelectAllCommand` and the selection methods of the data view |
| Editing | `IsReadOnly` and `CellValueChangedCommand` for boolean cells |
| Colors | `GridStyle` as a resource with `RowBackground` and the color callbacks |
| Other input | `CellTappedCommand`, `CellLongPressedCommand`, `ColumnWidthChangedCommand` and `RowMovedCommand` |

Messaging, navigation and screen controllers belong to the application.  

## ✅ Supported features

| Category | Detail |
|---|---|
| **Columns** | Auto / Absolute / Star width, minimum width, alignment, format string and value converter, frozen leading columns, static header and cell colors |
| **Column settings** | Visibility and order, edit session for a settings screen, drag to resize |
| **Data** | `INotifyCollectionChanged` / `INotifyPropertyChanged` tracking, stable row keys |
| **Sorting** | Multi-key sort with history, direction aware comparers, sort callback, configurable tap cycle |
| **Selection** | None / single / multiple toggle, select all, selection by key |
| **Editing** | Boolean cell toggle |
| **Row dragging** | Reorder rows by dragging the row header |
| **Input** | Tap, long press, pan with inertia, column resize, commands for MVVM |
| **Styling** | Font, padding, colors, sort marks, row header text and conditional color callbacks |
| **Fonts** | Per character fallback to `GridFonts` typefaces and system fonts, emoji sequences included |

## 📖 API

See the [API reference](Document/API.md).  
