namespace ClamGrid.Internal.Rendering;

using System.Globalization;
using System.Text;

using ClamGrid.Internal.Layout;

using SkiaSharp;
using SkiaSharp.Views.Maui;

internal sealed class GridRenderer : IDisposable
{
    private readonly GridStyle style;
    private readonly FontSet primary;
    private readonly FontSet japanese;
    private readonly Dictionary<string, FontSet> fallbackByFamily = [with(StringComparer.Ordinal)];
    private readonly Dictionary<int, FontSet> fallbackByCodepoint = [];
    private readonly SKPaint paint = new() { IsAntialias = true };
    private readonly Dictionary<(string Text, float Width, bool Header), TextRun> textCache = [];

    public int Measurements { get; private set; }

    public double AutoRowHeight { get; }

    public GridRenderer(GridStyle style)
    {
        style.Validate();
        this.style = style;
        primary = new FontSet(SKTypeface.FromFamilyName(style.FontFamily), style.FontSize);
        japanese = new FontSet(SKFontManager.Default.MatchCharacter(style.FontFamily, SKFontStyle.Normal, ["ja"], '日') ?? SKTypeface.FromFamilyName("sans-serif"), style.FontSize);
        AutoRowHeight = Math.Ceiling(Math.Max(primary.Font.Metrics.Descent - primary.Font.Metrics.Ascent, japanese.Font.Metrics.Descent - japanese.Font.Metrics.Ascent) + (style.VerticalPadding * 2));
    }

    public static string FormatValue(object? value) => Convert.ToString(value, CultureInfo.CurrentCulture) ?? String.Empty;

    public double[] MeasureColumns(IReadOnlyList<GridColumn> columns, IReadOnlyList<object> items, double availableWidth, int sampleSize, IGridDataView? dataView)
    {
        var specs = new GridColumnWidthSpec[columns.Count];
        for (var columnIndex = 0; columnIndex < columns.Count; columnIndex++)
        {
            var column = columns[columnIndex];
            var width = Measure(GetHeaderText(column, dataView), true);
            if (column.Width.Unit == GridColumnWidthUnit.Auto)
            {
                for (var row = 0; row < Math.Min(sampleSize, items.Count); row++)
                {
                    width = Math.Max(width, column.IsBoolean ? 24 : Measure(FormatValue(column.ValueAccessor.GetValue(items[row])), false));
                }
            }

            specs[columnIndex] = new GridColumnWidthSpec(column.Width, column.MinWidth, width + (style.HorizontalPadding * 2));
        }

        return GridColumnSizer.Resolve(specs, availableWidth);
    }

    public int Render(SKCanvas canvas, GridLayout layout, IReadOnlyList<GridColumn> columns, IReadOnlyList<object> items, IGridDataView? dataView, bool showRowHandles)
    {
        var rows = layout.VisibleRows;
        var visibleColumns = layout.VisibleColumns;
        var rendered = 0;
        canvas.Clear(style.Background.ToSKColor());
        canvas.Save();
        canvas.ClipRect(ToSkRect(layout.BodyBounds));
        for (var row = rows.Start; row < rows.End; row++)
        {
            var selected = dataView?.IsSelected(row) ?? false;
            var rowBackground = selected ? style.SelectedBackground : style.RowBackground?.Invoke(items[row]);
            for (var column = visibleColumns.Start; column < visibleColumns.End; column++)
            {
                var rect = layout.GetCellBounds(row, column);
                if (rect.IsEmpty)
                {
                    continue;
                }

                var definition = columns[column];
                var value = definition.ValueAccessor.GetValue(items[row]);
                var colors = new GridColors(selected ? style.SelectedTextColor : definition.TextColor ?? style.TextColor, rowBackground ?? definition.Background ?? style.Background);
                colors = colors.Apply(style.CellColors?.Invoke(new GridCellColorContext(items[row], row, definition, column, value, selected, colors)) ?? default);
                Fill(canvas, rect, colors.Background!);
                if (definition.IsBoolean)
                {
                    DrawBoolean(canvas, rect, value is true, colors.TextColor!, colors.Background!);
                }
                else
                {
                    DrawText(canvas, rect, FormatValue(value), definition.Alignment, false, colors.TextColor!);
                }

                DrawLines(canvas, rect);
                rendered++;
            }
        }

        canvas.Restore();
        canvas.Save();
        canvas.ClipRect(ToSkRect(layout.ColumnHeaderArea));
        for (var column = visibleColumns.Start; column < visibleColumns.End; column++)
        {
            var rect = layout.GetColumnHeaderBounds(column);
            if (rect.IsEmpty)
            {
                continue;
            }

            var definition = columns[column];
            var priority = GetSortPriority(definition, dataView);
            var order = priority >= 0 ? dataView!.SortOrders[priority] : null;
            var background = priority == 0 ? order!.Descending ? style.DescendingHeaderBackground : style.AscendingHeaderBackground : definition.HeaderBackground ?? style.HeaderBackground;
            var colors = new GridColors(definition.HeaderTextColor ?? style.HeaderTextColor, background);
            colors = colors.Apply(style.ColumnHeaderColors?.Invoke(new GridColumnHeaderColorContext(definition, column, order, priority, colors)) ?? default);
            Fill(canvas, rect, colors.Background!);
            DrawText(canvas, rect, GetHeaderText(definition, dataView), TextAlignment.Center, true, colors.TextColor!);
            DrawLines(canvas, rect);
        }

        canvas.Restore();
        canvas.Save();
        canvas.ClipRect(ToSkRect(layout.RowHeaderArea));
        for (var row = rows.Start; row < rows.End; row++)
        {
            var rect = layout.GetRowHeaderBounds(row);
            if (rect.IsEmpty)
            {
                continue;
            }

            var selected = dataView?.IsSelected(row) ?? false;
            var colors = new GridColors(selected ? style.SelectedTextColor : style.TextColor, selected ? style.SelectedBackground : style.RowHeaderBackground);
            colors = colors.Apply(style.RowHeaderColors?.Invoke(new GridRowHeaderColorContext(items[row], row, selected, colors)) ?? default);
            Fill(canvas, rect, colors.Background!);
            DrawText(canvas, rect, (row + 1).ToString(CultureInfo.InvariantCulture), TextAlignment.End, false, colors.TextColor!);
            if (showRowHandles)
            {
                paint.Color = colors.TextColor!.ToSKColor();
                paint.StrokeWidth = 2;
                for (var line = -1; line <= 1; line++)
                {
                    var y = (float)(rect.Y + (rect.Height / 2) + (line * 5));
                    canvas.DrawLine(5, y, 17, y, paint);
                }
            }

            DrawLines(canvas, rect);
        }

        canvas.Restore();
        var corner = new GridRect(0, 0, layout.RowHeaderWidth, layout.HeaderHeight);
        Fill(canvas, corner, style.RowHeaderBackground);
        DrawText(canvas, corner, "#", TextAlignment.Center, true, style.TextColor);
        DrawLines(canvas, corner);
        DrawScrollbars(canvas, layout);
        return rendered;
    }

    public void ClearCache() => textCache.Clear();

    public void Dispose()
    {
        paint.Dispose();
        primary.Dispose();
        japanese.Dispose();
        foreach (var fonts in fallbackByFamily.Values)
        {
            fonts.Dispose();
        }

        fallbackByFamily.Clear();
        fallbackByCodepoint.Clear();
    }

    private static GridSortOrder? GetPrimarySort(GridColumn column, IGridDataView? view)
    {
        var primary = view is { SortOrders.Count: > 0 } ? view.SortOrders[0] : null;
        return column.AllowSorting && (primary?.Key == (column.SortKey ?? column.Key)) ? primary : null;
    }

    private static int GetSortPriority(GridColumn column, IGridDataView? view)
    {
        if (column.AllowSorting && (view is not null))
        {
            for (var index = 0; index < view.SortOrders.Count; index++)
            {
                if (view.SortOrders[index].Key == (column.SortKey ?? column.Key))
                {
                    return index;
                }
            }
        }

        return -1;
    }

    private static string GetHeaderText(GridColumn column, IGridDataView? view)
    {
        var order = GetPrimarySort(column, view);
        return order is null ? column.Header : (order.Descending ? "↓ " : "↑ ") + column.Header;
    }

    private static SKRect ToSkRect(GridRect rect) => new((float)rect.X, (float)rect.Y, (float)rect.Right, (float)rect.Bottom);

    private FontSet ResolveFonts(int codepoint)
    {
        if (codepoint <= 127)
        {
            return primary;
        }

        if (japanese.Font.ContainsGlyph(codepoint))
        {
            return japanese;
        }

        // 絵文字などプライマリ・日本語フォントに無い文字は、システムのフォントから文字単位で探す
        if (!fallbackByCodepoint.TryGetValue(codepoint, out var fonts))
        {
            fonts = japanese;
            var typeface = SKFontManager.Default.MatchCharacter(style.FontFamily, SKFontStyle.Normal, null, codepoint);
            if (typeface is not null)
            {
                if (fallbackByFamily.TryGetValue(typeface.FamilyName, out var existing))
                {
                    typeface.Dispose();
                    fonts = existing;
                }
                else
                {
                    fonts = new FontSet(typeface, style.FontSize);
                    fallbackByFamily[typeface.FamilyName] = fonts;
                }
            }

            fallbackByCodepoint[codepoint] = fonts;
        }

        return fonts;
    }

    // 文字要素（結合文字・サロゲートペアを含む）ごとにフォントを決め、同じフォントが続く範囲をまとめる
    private List<TextSegment> Segment(string text, bool header)
    {
        var segments = new List<TextSegment>();
        var boundaries = StringInfo.ParseCombiningCharacters(text);
        FontSet? current = null;
        var start = 0;
        foreach (var index in boundaries)
        {
            var fonts = Rune.TryGetRuneAt(text, index, out var rune) ? ResolveFonts(rune.Value) : japanese;
            if (ReferenceEquals(fonts, current))
            {
                continue;
            }

            if (current is not null)
            {
                segments.Add(new TextSegment(text[start..index], header ? current.HeaderFont : current.Font));
            }

            current = fonts;
            start = index;
        }

        if (current is not null)
        {
            segments.Add(new TextSegment(text[start..], header ? current.HeaderFont : current.Font));
        }

        return segments;
    }

    private float Measure(string text, bool header)
    {
        var width = 0f;
        foreach (var segment in Segment(text, header))
        {
            Measurements++;
            width += segment.Font.MeasureText(segment.Text);
        }

        return width;
    }

    private TextRun GetText(string text, float availableWidth, bool header)
    {
        var key = (text, availableWidth, header);
        if (textCache.TryGetValue(key, out var cached))
        {
            return cached;
        }

        var segments = Segment(text, header);
        var widths = new float[segments.Count];
        var width = 0f;
        for (var i = 0; i < segments.Count; i++)
        {
            Measurements++;
            widths[i] = segments[i].Font.MeasureText(segments[i].Text);
            width += widths[i];
        }

        var runs = new List<TextRun.Part>(segments.Count + 1);
        if (width <= availableWidth)
        {
            for (var i = 0; i < segments.Count; i++)
            {
                runs.Add(new TextRun.Part(segments[i].Text, widths[i], segments[i].Font));
            }
        }
        else
        {
            // 省略記号を含めて収まる最長の先頭部分を、文字要素の境界で二分探索する
            var ellipsisFont = header ? primary.HeaderFont : primary.Font;
            Measurements++;
            var ellipsisWidth = ellipsisFont.MeasureText("…");
            width = 0f;
            var fits = ellipsisWidth <= availableWidth;
            for (var i = 0; fits && (i < segments.Count); i++)
            {
                if (width + widths[i] + ellipsisWidth <= availableWidth)
                {
                    runs.Add(new TextRun.Part(segments[i].Text, widths[i], segments[i].Font));
                    width += widths[i];
                    continue;
                }

                var segmentText = segments[i].Text;
                var boundaries = StringInfo.ParseCombiningCharacters(segmentText);
                var low = 0;
                var high = boundaries.Length;
                var lowWidth = 0f;
                while (low < high)
                {
                    var middle = low + ((high - low + 1) / 2);
                    var end = middle == boundaries.Length ? segmentText.Length : boundaries[middle];
                    Measurements++;
                    var prefixWidth = segments[i].Font.MeasureText(segmentText.AsSpan(0, end));
                    if (width + prefixWidth + ellipsisWidth <= availableWidth)
                    {
                        low = middle;
                        lowWidth = prefixWidth;
                    }
                    else
                    {
                        high = middle - 1;
                    }
                }

                if (low > 0)
                {
                    var end = low == boundaries.Length ? segmentText.Length : boundaries[low];
                    runs.Add(new TextRun.Part(segmentText[..end], lowWidth, segments[i].Font));
                    width += lowWidth;
                }

                break;
            }

            if (fits)
            {
                runs.Add(new TextRun.Part("…", ellipsisWidth, ellipsisFont));
                width += ellipsisWidth;
            }
            else
            {
                runs.Clear();
                width = 0f;
            }
        }

        if (textCache.Count >= 2048)
        {
            textCache.Clear();
        }

        var result = new TextRun(runs, width);
        textCache[key] = result;
        return result;
    }

    private void DrawText(SKCanvas canvas, GridRect rect, string text, TextAlignment alignment, bool header, Color color)
    {
        var available = (float)rect.Width - (style.HorizontalPadding * 2);
        if (rect.IsEmpty || (available <= 0) || (text.Length == 0))
        {
            return;
        }

        var run = GetText(text.Replace('\r', ' ').Replace('\n', ' '), available, header);
        if (run.Parts.Count == 0)
        {
            return;
        }

        var x = alignment switch
        {
            TextAlignment.Center => rect.X + ((rect.Width - run.Width) / 2),
            TextAlignment.End => rect.Right - style.HorizontalPadding - run.Width,
            _ => rect.X + style.HorizontalPadding
        };
        var metrics = (header ? primary.HeaderFont : primary.Font).Metrics;
        var baseline = rect.Y + ((rect.Height - (metrics.Descent - metrics.Ascent)) / 2) - metrics.Ascent;
        canvas.Save();
        canvas.ClipRect(ToSkRect(rect));
        paint.Style = SKPaintStyle.Fill;
        paint.Color = color.ToSKColor();
        foreach (var part in run.Parts)
        {
            canvas.DrawText(part.Text, (float)x, (float)baseline, SKTextAlign.Left, part.Font, paint);
            x += part.Width;
        }

        canvas.Restore();
    }

    private void Fill(SKCanvas canvas, GridRect rect, Color color)
    {
        paint.Style = SKPaintStyle.Fill;
        paint.Color = color.ToSKColor();
        canvas.DrawRect(ToSkRect(rect), paint);
    }

    private void DrawLines(SKCanvas canvas, GridRect rect)
    {
        paint.Style = SKPaintStyle.Stroke;
        paint.StrokeWidth = 0.5f;
        paint.Color = style.GridLineColor.ToSKColor();
        canvas.DrawLine((float)rect.X, (float)rect.Bottom, (float)rect.Right, (float)rect.Bottom, paint);
        if (style.ShowVerticalLines)
        {
            canvas.DrawLine((float)rect.Right, (float)rect.Y, (float)rect.Right, (float)rect.Bottom, paint);
        }
    }

    private void DrawBoolean(SKCanvas canvas, GridRect rect, bool value, Color foreground, Color background)
    {
        var size = (float)Math.Min(20, Math.Min(rect.Width, rect.Height) - 8);
        if (size <= 0)
        {
            return;
        }

        var box = SKRect.Create((float)(rect.X + ((rect.Width - size) / 2)), (float)(rect.Y + ((rect.Height - size) / 2)), size, size);
        paint.Color = foreground.ToSKColor();
        paint.Style = value ? SKPaintStyle.Fill : SKPaintStyle.Stroke;
        paint.StrokeWidth = 1.5f;
        canvas.DrawRoundRect(box, 3, 3, paint);
        if (value)
        {
            paint.Style = SKPaintStyle.Stroke;
            paint.Color = background.ToSKColor();
            canvas.DrawLine(box.Left + 4, box.MidY, box.MidX - 1, box.Bottom - 5, paint);
            canvas.DrawLine(box.MidX - 1, box.Bottom - 5, box.Right - 3, box.Top + 5, paint);
        }
    }

    private void DrawScrollbars(SKCanvas canvas, GridLayout layout)
    {
        var body = layout.BodyBounds;
        paint.Style = SKPaintStyle.Fill;
        paint.Color = new SKColor(0x64, 0x74, 0x8B, 140);
        if ((layout.MaximumScrollY > 0) && !body.IsEmpty)
        {
            var height = Math.Min(body.Height, Math.Max(24, body.Height * body.Height / layout.ContentHeight));
            var y = body.Y + ((body.Height - height) * layout.ScrollY / layout.MaximumScrollY);
            canvas.DrawRoundRect(SKRect.Create((float)body.Right - 5, (float)y, 4, (float)height), 2, 2, paint);
        }

        if ((layout.MaximumScrollX > 0) && !body.IsEmpty)
        {
            var width = Math.Min(body.Width, Math.Max(24, body.Width * body.Width / layout.ContentWidth));
            var x = body.X + ((body.Width - width) * layout.ScrollX / layout.MaximumScrollX);
            canvas.DrawRoundRect(SKRect.Create((float)x, (float)body.Bottom - 5, (float)width, 4), 2, 2, paint);
        }
    }

    private sealed record TextSegment(string Text, SKFont Font);

    private sealed record TextRun(IReadOnlyList<TextRun.Part> Parts, float Width)
    {
        public sealed record Part(string Text, float Width, SKFont Font);
    }

    private sealed class FontSet : IDisposable
    {
        public SKTypeface Typeface { get; }

        public SKFont Font { get; }

        public SKFont HeaderFont { get; }

        public FontSet(SKTypeface typeface, float size)
        {
            Typeface = typeface;
            Font = new SKFont(typeface, size);
            HeaderFont = new SKFont(typeface, size) { Embolden = true };
        }

        public void Dispose()
        {
            HeaderFont.Dispose();
            Font.Dispose();
            Typeface.Dispose();
        }
    }
}
