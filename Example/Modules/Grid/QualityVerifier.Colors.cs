namespace Example.Modules.Grid;

using SkiaSharp.Views.Maui;

public sealed partial class QualityVerifier
{
    private async Task VerifyColorsAsync()
    {
        report("セル・見出しの条件色と描画画素の検証");
        host.Content = null;
        grid.Dispose();
        grid.Handler?.DisconnectHandler();
        grid.Handler = null;
        data?.Dispose();
        data = null;
        var rows = SampleData.CreateRows(1000).ToArray();
        using var view = new GridDataView<SampleRow>(rows, static row => row.Id);
        view.RegisterSort("nameKey", static row => row.Name, StringComparer.Ordinal);
        view.RegisterSort("id", static row => row.Id);
        var cells = new List<GridCellColorContext>();
        var headers = new List<GridColumnHeaderColorContext>();
        var rowHeaders = new List<GridRowHeaderColorContext>();
        var overrideHeader = true;
        var backgroundOnly = false;
        var probe = new ColorProbeGrid { AutomationId = "QualityGrid" };
        grid = probe;
        GridColumn[] columns = [
            new("name", "AAA", new GridValueAccessor<SampleRow, string>(static row => row.Name)) { Width = GridColumnWidth.Absolute(100), SortKey = "nameKey", TextColor = Colors.Blue, Background = Colors.LightYellow, HeaderTextColor = Colors.Yellow, HeaderBackground = Colors.DarkGreen },
            new("id", "BBB", new GridValueAccessor<SampleRow, int>(static row => row.Id)) { Width = GridColumnWidth.Absolute(100) },
            new("done", "CCC", new GridValueAccessor<SampleRow, bool>(static row => row.IsChecked)) { Width = GridColumnWidth.Absolute(100), IsBoolean = true, AllowSorting = false }
        ];
        await NextFrameAsync(() =>
        {
            probe.Columns.ReplaceAll(columns);
            probe.GridStyle = new GridStyle
            {
                RowHeight = 40,
                HeaderHeight = 40,
                RowHeaderWidth = 48,
                TextColor = Colors.Black,
                Background = Colors.White,
                RowHeaderBackground = Colors.Beige,
                HeaderBackground = Colors.Gray,
                HeaderTextColor = Colors.White,
                SelectedTextColor = Colors.White,
                SelectedBackground = Colors.Blue,
                AscendingHeaderBackground = Colors.LightBlue,
                DescendingHeaderBackground = Colors.Orange,
                RowBackground = static item => ((SampleRow)item).Id == 1 ? Colors.LightCyan : null,
                CellColors = context =>
                {
                    cells.Add(context);
                    if ((context.Column.Key == "name") && (context.Item is SampleRow { Id: 0, IsDiscontinued: true }))
                    {
                        return backgroundOnly ? new(Background: Colors.Pink) : new(Colors.Red, Colors.Pink);
                    }

                    return context.Column.IsBoolean ? new(Colors.Green) : default;
                },
                ColumnHeaderColors = context =>
                {
                    headers.Add(context);
                    return overrideHeader && (context.Column.Key == "id") ? new(Colors.Yellow, Colors.Purple) : default;
                },
                RowHeaderColors = context =>
                {
                    rowHeaders.Add(context);
                    return context.Item is SampleRow { Id: 0, IsDiscontinued: true } ? new(Colors.Yellow, Colors.Purple) : default;
                }
            };
            probe.ItemsSource = view;
            host.Content = probe;
        }).ConfigureAwait(true);

        using (var bitmap = RenderBitmap())
        {
            CheckColors(bitmap, new Rect(48, 40, 100, 40), Colors.Pink, Colors.Red, "cell callback");
            CheckColors(bitmap, new Rect(48, 80, 100, 40), Colors.LightCyan, Colors.Blue, "row background / column text");
            CheckColors(bitmap, new Rect(48, 120, 100, 40), Colors.LightYellow, Colors.Blue, "column defaults");
            CheckColors(bitmap, new Rect(48, 0, 100, 40), Colors.DarkGreen, Colors.Yellow, "column header defaults");
            CheckColors(bitmap, new Rect(148, 0, 100, 40), Colors.Purple, Colors.Yellow, "header callback");
            CheckColors(bitmap, new Rect(0, 40, 48, 40), Colors.Purple, Colors.Yellow, "row header callback");
            CheckColors(bitmap, new Rect(248, 80, 100, 40), Colors.LightCyan, Colors.Green, "boolean foreground");
            Check("colors: complete callback context", cells.All(context => ReferenceEquals(context.Item, view[context.RowIndex]) && ReferenceEquals(context.Column, columns[context.ColumnIndex]) && Equals(context.Value, context.Column.ValueAccessor.GetValue(context.Item))));
            Check("colors: callbacks only for visible cells", cells.Count == probe.LastFrame!.RenderedCells && cells.All(context => context.RowIndex >= probe.LastFrame.Rows.Start && context.RowIndex < probe.LastFrame.Rows.End && context.ColumnIndex >= probe.LastFrame.Columns.Start && context.ColumnIndex < probe.LastFrame.Columns.End));
            Check("colors: no unregistered sort state", headers.All(static context => context.SortOrder is null && context.SortPriority == -1));
        }

        await NextFrameAsync(() => backgroundOnly = true).ConfigureAwait(true);
        using (var bitmap = RenderBitmap())
        {
            CheckColors(bitmap, new Rect(48, 40, 100, 40), Colors.Pink, Colors.Blue, "background-only override inherits text");
        }

        backgroundOnly = false;
        await NextFrameAsync(() =>
        {
            view.SetSelected(0, true);
            view.SetSelected(1, true);
        }).ConfigureAwait(true);
        using (var bitmap = RenderBitmap())
        {
            CheckColors(bitmap, new Rect(48, 40, 100, 40), Colors.Pink, Colors.Red, "callback overrides selection");
            CheckColors(bitmap, new Rect(48, 80, 100, 40), Colors.Blue, Colors.White, "default inherits selection");
            CheckColors(bitmap, new Rect(0, 80, 48, 40), Colors.Blue, Colors.White, "row header selection");
            var context = cells.First(static context => context.RowIndex == 0);
            Check("colors: selection state and default colors", context.IsSelected && Equals(context.DefaultColors.Background, Colors.Blue) && Equals(context.DefaultColors.TextColor, Colors.White));
        }

        await NextFrameAsync(() =>
        {
            view.ClearSelection();
            rows[0].IsDiscontinued = false;
        }).ConfigureAwait(true);
        using (var bitmap = RenderBitmap())
        {
            CheckColors(bitmap, new Rect(48, 40, 100, 40), Colors.LightYellow, Colors.Blue, "property change refreshes color");
            CheckColors(bitmap, new Rect(0, 40, 48, 40), Colors.Beige, Colors.Black, "row header property change");
        }

        await NextFrameAsync(() => view.RestoreSortOrders([new("nameKey", true), new("id")])).ConfigureAwait(true);
        using (var bitmap = RenderBitmap())
        {
            CheckColors(bitmap, new Rect(48, 0, 100, 40), Colors.Orange, Colors.Yellow, "primary sort default");
            CheckColors(bitmap, new Rect(148, 0, 100, 40), Colors.Purple, Colors.Yellow, "secondary sort callback");
            Check("colors: sort member and priority", headers[0].SortOrder == new GridSortOrder("nameKey", true) && headers[0].SortPriority == 0 && headers[1].SortPriority == 1 && headers[2].SortPriority == -1);
            Check("colors: sorted item identity", cells.All(context => ReferenceEquals(context.Item, view[context.RowIndex])) && rowHeaders.All(context => ReferenceEquals(context.Item, view[context.RowIndex])));
        }

        await NextFrameAsync(() => view.SortBy("id")).ConfigureAwait(true);
        using (var bitmap = RenderBitmap())
        {
            CheckColors(bitmap, new Rect(148, 0, 100, 40), Colors.Purple, Colors.Yellow, "callback overrides primary sort");
            Check("colors: header receives sorted defaults", headers[1].SortPriority == 0 && Equals(headers[1].DefaultColors.Background, Colors.LightBlue));
        }

        await NextFrameAsync(() =>
        {
            overrideHeader = false;
            grid.InvalidateSurface();
        }).ConfigureAwait(true);
        using (var bitmap = RenderBitmap())
        {
            CheckColors(bitmap, new Rect(148, 0, 100, 40), Colors.LightBlue, Colors.White, "external state invalidation");
        }

        await NextFrameAsync(() =>
        {
            probe.Columns.ReplaceAll([columns[1], columns[0]]);
            probe.ScrollIntoView(800, 1);
        }).ConfigureAwait(true);
        using (RenderBitmap())
        {
            Check("colors: reordered columns and scrolled rows", cells.Count > 0 && cells.All(context => ReferenceEquals(context.Column, probe.Columns[context.ColumnIndex]) && ReferenceEquals(context.Item, view[context.RowIndex])));
            Check("colors: hidden column not evaluated", cells.All(static context => context.Column.Key != "done") && headers.All(static context => context.Column.Key != "done"));
            Check("colors: offscreen rows not evaluated", cells.Count == probe.LastFrame!.RenderedCells && cells.Min(static context => context.RowIndex) > 0 && rowHeaders.Count == probe.LastFrame.Rows.Count);
        }

        await NextFrameAsync(() => probe.GridStyle = probe.GridStyle with { ShowColumnHeaders = false, ShowRowHeaders = false }).ConfigureAwait(true);
        using (RenderBitmap())
        {
            Check("colors: hidden headers not evaluated", headers.Count == 0 && rowHeaders.Count == 0 && cells.Count > 0);
        }

        await NextFrameAsync(() =>
        {
            probe.GridStyle = probe.GridStyle with { ShowColumnHeaders = true, ShowRowHeaders = true };
            view.SetSource(Array.Empty<SampleRow>());
        }).ConfigureAwait(true);
        using (RenderBitmap())
        {
            Check("colors: empty data still paints headers", cells.Count == 0 && rowHeaders.Count == 0 && headers.Count == probe.Columns.Count);
        }

        grid.ItemsSource = null;
        return;

        SKBitmap RenderBitmap()
        {
            cells.Clear();
            headers.Clear();
            rowHeaders.Clear();
            return probe.RenderBitmap();
        }
    }

    private void CheckColors(SKBitmap bitmap, Rect rect, Color background, Color foreground, string name)
    {
        var scaleX = bitmap.Width / grid.Width;
        var scaleY = bitmap.Height / grid.Height;
        rect = new Rect(rect.X * scaleX, rect.Y * scaleY, rect.Width * scaleX, rect.Height * scaleY);
        Check($"colors: {name} background pixels", bitmap.GetPixel((int)rect.X + 2, (int)rect.Y + 2) == background.ToSKColor());
        var target = foreground.ToSKColor();
        var matches = 0;
        for (var y = (int)rect.Y + 7; y < Math.Min(bitmap.Height, rect.Bottom - 7); y++)
        {
            for (var x = (int)rect.X + 4; x < Math.Min(bitmap.Width, rect.Right - 4); x++)
            {
                var pixel = bitmap.GetPixel(x, y);
                // Tolerates anti-aliased edge blending while still detecting a wrong text color
                if ((Math.Abs(pixel.Red - target.Red) <= 24) && (Math.Abs(pixel.Green - target.Green) <= 24) && (Math.Abs(pixel.Blue - target.Blue) <= 24))
                {
                    matches++;
                }
            }
        }

        Check($"colors: {name} foreground pixels", matches >= 3);
    }

    private sealed class ColorProbeGrid : ClamGridView
    {
        public GridFrameEventArgs? LastFrame { get; private set; }

        public ColorProbeGrid() => FrameRendered += (_, args) => LastFrame = args;

        public SKBitmap RenderBitmap()
        {
            var native = (global::Android.Views.View)Handler!.PlatformView!;
            var info = new SKImageInfo(native.Width, native.Height);
            using var surface = SKSurface.Create(info);
            OnPaintSurface(new SKPaintSurfaceEventArgs(surface, info));
            using var image = surface.Snapshot();
            return SKBitmap.FromImage(image);
        }
    }
}
