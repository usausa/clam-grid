namespace Example.Modules.Grid;

internal static class SampleColumns
{
    public static readonly Color DiscontinuedBackground = Color.FromArgb("#FEF9C3");

    public static readonly Color AlternatingBackground = Color.FromArgb("#F1F5F9");

    public static GridColumn[] CreateList() =>
    [
        new("visible", "発注", new GridValueAccessor<SampleRow, bool>(static x => x.IsChecked, static (x, value) => x.IsChecked = value)) { Width = GridColumnWidth.Absolute(64), IsBoolean = true, AllowSorting = false },
        new("discontinued", "廃番", new GridValueAccessor<SampleRow, bool>(static x => x.IsDiscontinued, static (x, value) => x.IsDiscontinued = value)) { Width = GridColumnWidth.Absolute(64), IsBoolean = true },
        new("status", "状態", new GridValueAccessor<SampleRow, string>(static x => x.Status)) { Width = GridColumnWidth.Absolute(64), Alignment = TextAlignment.Center },
        new("code", "コード", new GridValueAccessor<SampleRow, string>(static x => x.Code)) { Width = GridColumnWidth.Absolute(120) },
        new("name", "顧客名", new GridValueAccessor<SampleRow, string>(static x => x.Name)) { Width = GridColumnWidth.Absolute(200) },
        new("category", "カテゴリ", new GridValueAccessor<SampleRow, string>(static x => x.Category)) { Width = GridColumnWidth.Auto },
        new("warehouse", "倉庫", new GridValueAccessor<SampleRow, int>(static x => x.Warehouse)) { Width = GridColumnWidth.Absolute(72), Alignment = TextAlignment.End },
        new("supplier", "仕入先", new GridValueAccessor<SampleRow, string>(static x => x.Supplier)) { Width = GridColumnWidth.Auto },
        new("difference", "在庫差異", new GridValueAccessor<SampleRow, int>(static x => x.Difference)) { Width = GridColumnWidth.Absolute(96), Alignment = TextAlignment.End, Format = "+#,0;-#,0;0" },
        new("description", "説明", new GridValueAccessor<SampleRow, string>(static x => x.Description)) { Width = GridColumnWidth.Absolute(240), LineCount = 2, VerticalAlignment = TextAlignment.Start },
        new("location", "棚番", new GridValueAccessor<SampleRow, string>(static x => x.Location)) { Width = GridColumnWidth.Absolute(200) },
        new("unit", "単位", new GridValueAccessor<SampleRow, string>(static x => x.Unit)) { Width = GridColumnWidth.Absolute(140) },
        new("arrival", "入荷日", new GridValueAccessor<SampleRow, DateTime>(static x => x.ArrivalDate)) { Width = GridColumnWidth.Absolute(80), Alignment = TextAlignment.End, Format = "MM/dd" },
        new("updated", "更新日時", new GridValueAccessor<SampleRow, DateTime>(static x => x.UpdatedAt)) { Width = GridColumnWidth.Absolute(200), Format = "MM/dd HH:mm" }
    ];

    public static GridColumn[] CreateColor() =>
    [
        new("discontinued", "廃番", new GridValueAccessor<SampleRow, bool>(static x => x.IsDiscontinued, static (x, value) => x.IsDiscontinued = value)) { Width = GridColumnWidth.Absolute(64), IsBoolean = true, AllowSorting = false },
        new("name", "顧客名", new GridValueAccessor<SampleRow, string>(static x => x.Name)) { Width = GridColumnWidth.Star(), MinWidth = 160 },
        new("difference", "在庫差異", new GridValueAccessor<SampleRow, int>(static x => x.Difference)) { Width = GridColumnWidth.Absolute(96), Alignment = TextAlignment.End, HeaderTextColor = Colors.White, HeaderBackground = Color.FromArgb("#4F46E5") }
    ];

    public static GridColumn[] CreateColumnSettings() =>
    [
        new("visible", "表示", new GridValueAccessor<GridColumnOption, bool>(static x => x.IsVisible, static (x, value) => x.IsVisible = value)) { Width = GridColumnWidth.Auto, IsBoolean = true, IsReadOnly = false, AllowSorting = false, AllowResizing = false, Alignment = TextAlignment.Center },
        new("header", "項目名", new GridValueAccessor<GridColumnOption, string>(static x => x.Header)) { Width = GridColumnWidth.Star(), MinWidth = 120, AllowSorting = false, AllowResizing = false },
        // Saved width in DIP; empty when the declared width is used
        new("width", "幅", new GridValueAccessor<GridColumnOption, double?>(static x => x.Width)) { Width = GridColumnWidth.Absolute(80), Alignment = TextAlignment.End, Format = "N0", AllowSorting = false, AllowResizing = false }
    ];

    public static void RegisterSorts(GridDataView<SampleRow> rows)
    {
        rows.RegisterSort("status", static x => x.IsDiscontinued);
        rows.RegisterSort("code", static x => x.Id);
        rows.RegisterSort("name", static x => x.Name, StringComparer.CurrentCulture);
        rows.RegisterSort("category", static x => x.Category, StringComparer.CurrentCulture);
        rows.RegisterSort("warehouse", static x => x.Warehouse);
        rows.RegisterSort("supplier", static x => x.Supplier, StringComparer.Ordinal);
        rows.RegisterSort("difference", static x => x.Difference);
        rows.RegisterSort("description", static x => x.DescriptionNumber);
        rows.RegisterSort("location", static x => x.Location, StringComparer.CurrentCulture);
        rows.RegisterSort("unit", static x => x.Unit, StringComparer.Ordinal);
        rows.RegisterSort("arrival", static x => x.ArrivalDate);
        rows.RegisterSort("updated", static x => x.UpdatedAt);
        rows.RegisterSort("discontinued", static x => x.IsDiscontinued);
    }
}
