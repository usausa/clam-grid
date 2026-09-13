namespace ClamGrid.Rendering;

using System.Buffers;
using System.Globalization;
using System.Text;

using ClamGrid.Layout;

using SkiaSharp;
using SkiaSharp.Views.Maui;

internal sealed class GridRenderer : IDisposable
{
    private const int EmojiFlag = 1 << 24;

    private static readonly string[] EmojiLanguages = ["und-Zsye"];
    private static readonly SearchValues<char> Selectors = SearchValues.Create("\uFE0E\uFE0F\u200D");
    private static readonly TextRun EmptyRun = new(Array.Empty<TextRun.Part>(), 0);

    private readonly GridStyle style;
    private readonly FontSet primary;
    private readonly string[]? languages;
    private readonly FontSet[] fallbacks;
    private readonly Dictionary<string, FontSet> fallbackByFamily = [with(StringComparer.Ordinal)];
    private readonly Dictionary<int, FontSet> fallbackByCodepoint = [];
    private readonly SKPaint paint = new() { IsAntialias = true };
    private readonly Dictionary<(string Text, float Width, bool Header), TextRun> textCache = [];

    public int Measurements { get; private set; }

    // Includes every font resolved so far, so fonts found while measuring the headers and sample rows widen the automatic height
    public double AutoRowHeight
    {
        get
        {
            var height = LineHeight(primary);
            foreach (var fonts in fallbacks.Concat(fallbackByFamily.Values))
            {
                height = Math.Max(height, LineHeight(fonts));
            }

            return Math.Ceiling(height + (style.VerticalPadding * 2));
        }
    }

    public GridRenderer(GridStyle style)
    {
        style.Validate();
        this.style = style;
        primary = new FontSet(SKTypeface.FromFamilyName(style.FontFamily), style.FontSize);
        languages = GridFonts.Languages.Count > 0 ? GridFonts.Languages.ToArray() : null;
        fallbacks = GridFonts.Fallbacks.Select(typeface => new FontSet(typeface, style.FontSize, false)).ToArray();
    }

    public static string FormatValue(object? value, string? format) =>
        !String.IsNullOrEmpty(format) && (value is IFormattable formattable)
            ? formattable.ToString(format, CultureInfo.CurrentCulture)
            : Convert.ToString(value, CultureInfo.CurrentCulture) ?? String.Empty;

    public double[] MeasureColumns(IReadOnlyList<GridColumn> columns, IReadOnlyList<object> items, double availableWidth, int sampleSize, IGridDataView? dataView)
    {
        var specs = new GridColumnWidthSpec[columns.Count];
        for (var columnIndex = 0; columnIndex < columns.Count; columnIndex++)
        {
            var column = columns[columnIndex];
            var width = Measure(GetHeaderText(style, column, dataView), true);
            if (column.Width.Unit == GridColumnWidthUnit.Auto)
            {
                for (var row = 0; row < Math.Min(sampleSize, items.Count); row++)
                {
                    width = Math.Max(width, column.IsBoolean ? 24 : Measure(FormatValue(column.ValueAccessor.GetValue(items[row]), column.Format), false));
                }
            }

            specs[columnIndex] = new GridColumnWidthSpec(column.Width, column.MinWidth, width + (style.HorizontalPadding * 2));
        }

        return GridColumnSizer.Resolve(specs, availableWidth);
    }

    public int Render(SKCanvas canvas, GridLayout layout, IReadOnlyList<GridColumn> columns, IReadOnlyList<object> items, IGridDataView? dataView, bool showRowHandles)
    {
        var rows = layout.VisibleRows;
        var states = new RowState[rows.Count];
        for (var row = rows.Start; row < rows.End; row++)
        {
            var selected = dataView?.IsSelected(row) ?? false;
            states[row - rows.Start] = new RowState(selected, selected ? style.SelectedBackground : style.RowBackground?.Invoke(items[row]));
        }

        canvas.Clear(style.Background.ToSKColor());
        var headerArea = layout.ColumnHeaderArea;
        var scrollArea = layout.ScrollArea;
        var rendered = RenderCells(canvas, layout, scrollArea, layout.VisibleColumns, columns, items, rows, states);
        rendered += RenderCells(canvas, layout, layout.FrozenArea, layout.FrozenColumns, columns, items, rows, states);
        RenderHeaders(canvas, layout, new GridRect(scrollArea.X, 0, scrollArea.Width, headerArea.Height), layout.VisibleColumns, columns, dataView);
        RenderHeaders(canvas, layout, new GridRect(headerArea.X, 0, layout.FrozenWidth, headerArea.Height), layout.FrozenColumns, columns, dataView);
        canvas.Save();
        canvas.ClipRect(ToSkRect(layout.RowHeaderArea));
        for (var row = rows.Start; row < rows.End; row++)
        {
            var rect = layout.GetRowHeaderBounds(row);
            if (rect.IsEmpty)
            {
                continue;
            }

            var selected = states[row - rows.Start].Selected;
            var colors = new GridColors(selected ? style.SelectedTextColor : style.TextColor, selected ? style.SelectedBackground : style.RowHeaderBackground);
            colors = colors.Apply(style.RowHeaderColors?.Invoke(new GridRowHeaderColorContext(items[row], row, selected, colors)) ?? default);
            Fill(canvas, rect, colors.Background!);
            DrawText(canvas, rect, GetRowHeaderText(style, items[row], row, selected), style.RowHeaderAlignment, false, colors.TextColor!);
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
        Fill(canvas, corner, style.HeaderBackground);
        DrawText(canvas, corner, style.CornerText, TextAlignment.Center, true, style.HeaderTextColor);
        DrawLines(canvas, corner);
        DrawFrozenLine(canvas, layout);
        DrawScrollbars(canvas, layout);
        return rendered;
    }

    // Scrolling and frozen columns are painted in separate passes with their own clip so frozen cells cover scrolled ones
    private int RenderCells(SKCanvas canvas, GridLayout layout, GridRect clip, GridIndexRange range, IReadOnlyList<GridColumn> columns, IReadOnlyList<object> items, GridIndexRange rows, RowState[] states)
    {
        if (clip.IsEmpty || (range.Count == 0))
        {
            return 0;
        }

        var rendered = 0;
        canvas.Save();
        canvas.ClipRect(ToSkRect(clip));
        for (var row = rows.Start; row < rows.End; row++)
        {
            var state = states[row - rows.Start];
            for (var column = range.Start; column < range.End; column++)
            {
                var rect = layout.GetCellBounds(row, column);
                if (rect.IsEmpty)
                {
                    continue;
                }

                var definition = columns[column];
                var value = definition.ValueAccessor.GetValue(items[row]);
                var colors = new GridColors(state.Selected ? style.SelectedTextColor : definition.TextColor ?? style.TextColor, state.Background ?? definition.Background ?? style.Background);
                colors = colors.Apply(style.CellColors?.Invoke(new GridCellColorContext(items[row], row, definition, column, value, state.Selected, colors)) ?? default);
                Fill(canvas, rect, colors.Background!);
                if (definition.IsBoolean)
                {
                    DrawBoolean(canvas, rect, value is true, colors.TextColor!, colors.Background!);
                }
                else
                {
                    DrawText(canvas, rect, FormatValue(value, definition.Format), definition.Alignment, false, colors.TextColor!);
                }

                DrawLines(canvas, rect);
                rendered++;
            }
        }

        canvas.Restore();
        return rendered;
    }

    private void RenderHeaders(SKCanvas canvas, GridLayout layout, GridRect clip, GridIndexRange range, IReadOnlyList<GridColumn> columns, IGridDataView? dataView)
    {
        if (clip.IsEmpty || (range.Count == 0))
        {
            return;
        }

        canvas.Save();
        canvas.ClipRect(ToSkRect(clip));
        for (var column = range.Start; column < range.End; column++)
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
            DrawHeader(canvas, rect, definition, dataView, colors.TextColor!);
            DrawLines(canvas, rect);
        }

        canvas.Restore();
    }

    public void ClearCache() => textCache.Clear();

    public void Dispose()
    {
        paint.Dispose();
        primary.Dispose();
        foreach (var fonts in fallbacks.Concat(fallbackByFamily.Values))
        {
            fonts.Dispose();
        }

        fallbackByFamily.Clear();
        fallbackByCodepoint.Clear();
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

    internal static string GetRowHeaderText(GridStyle style, object item, int rowIndex, bool selected) =>
        style.RowHeaderText?.Invoke(new GridRowHeaderTextContext(item, rowIndex, selected)) ?? (rowIndex + 1).ToString(CultureInfo.InvariantCulture);

    internal static string GetHeaderText(GridStyle style, GridColumn column, IGridDataView? view)
    {
        var mark = GetSortMark(style, column, view);
        if (String.IsNullOrEmpty(mark))
        {
            return column.Header;
        }

        return style.SortMarkPosition == GridSortMarkPosition.End ? $"{column.Header} {mark}" : $"{mark} {column.Header}";
    }

    // The primary key always gets a mark; secondary keys get a mark with their priority only with ShowSortPriority
    private static string GetSortMark(GridStyle style, GridColumn column, IGridDataView? view)
    {
        var priority = GetSortPriority(column, view);
        if ((priority < 0) || ((priority > 0) && !style.ShowSortPriority))
        {
            return String.Empty;
        }

        var mark = view!.SortOrders[priority].Descending ? style.DescendingSortMark : style.AscendingSortMark;
        return style.ShowSortPriority && (view.SortOrders.Count > 1) ? mark + (priority + 1).ToString(CultureInfo.InvariantCulture) : mark;
    }

    private static SKRect ToSkRect(GridRect rect) => new((float)rect.X, (float)rect.Y, (float)rect.Right, (float)rect.Bottom);

    private static string Sanitize(string text) => text.Replace('\r', ' ').Replace('\n', ' ');

    private static float LineHeight(FontSet fonts) => fonts.Font.Metrics.Descent - fonts.Font.Metrics.Ascent;

    // Variation selectors and joiners have no glyph without shaping, so they only steer the font choice and are not drawn
    private static string StripSelectors(string text) => text.AsSpan().IndexOfAny(Selectors) < 0 ? text : text.Replace("\uFE0F", String.Empty, StringComparison.Ordinal).Replace("\uFE0E", String.Empty, StringComparison.Ordinal).Replace("\u200D", String.Empty, StringComparison.Ordinal);

    private FontSet ResolveFonts(int codepoint, bool emoji)
    {
        if ((codepoint <= 127) && !emoji)
        {
            return primary;
        }

        var key = emoji ? codepoint | EmojiFlag : codepoint;
        if (fallbackByCodepoint.TryGetValue(key, out var fonts))
        {
            return fonts;
        }

        // An emoji presentation sequence asks the system for the emoji font before the primary font is considered
        fonts = emoji ? MatchSystemFont(codepoint, EmojiLanguages) : null;
        if (fonts is null)
        {
            fonts = primary;
            if (!primary.Font.ContainsGlyph(codepoint))
            {
                // Configured fallback fonts come first, then the system fonts are searched per character with the language hints
                fonts = Array.Find(fallbacks, fallback => fallback.Font.ContainsGlyph(codepoint)) ?? MatchSystemFont(codepoint, languages) ?? primary;
            }
        }

        fallbackByCodepoint[key] = fonts;
        return fonts;
    }

    private FontSet? MatchSystemFont(int codepoint, string[]? hints)
    {
        var typeface = SKFontManager.Default.MatchCharacter(style.FontFamily, SKFontStyle.Normal, hints, codepoint);
        if (typeface is null)
        {
            return null;
        }

        if (fallbackByFamily.TryGetValue(typeface.FamilyName, out var existing))
        {
            typeface.Dispose();
            return existing;
        }

        var fonts = new FontSet(typeface, style.FontSize);
        fallbackByFamily[typeface.FamilyName] = fonts;
        return fonts;
    }

    // Picks a font per text element (combining marks and surrogate pairs included) and merges runs that share a font
    private List<TextSegment> Segment(string text, bool header)
    {
        var segments = new List<TextSegment>();
        var boundaries = StringInfo.ParseCombiningCharacters(text);
        FontSet? current = null;
        var start = 0;
        for (var i = 0; i < boundaries.Length; i++)
        {
            var index = boundaries[i];
            var end = i + 1 < boundaries.Length ? boundaries[i + 1] : text.Length;
            var emoji = text.AsSpan(index, end - index).Contains('\uFE0F');
            var fonts = Rune.TryGetRuneAt(text, index, out var rune) ? ResolveFonts(rune.Value, emoji) : primary;
            if (ReferenceEquals(fonts, current))
            {
                continue;
            }

            if (current is not null)
            {
                segments.Add(new TextSegment(StripSelectors(text[start..index]), header ? current.HeaderFont : current.Font));
            }

            current = fonts;
            start = index;
        }

        if (current is not null)
        {
            segments.Add(new TextSegment(StripSelectors(text[start..]), header ? current.HeaderFont : current.Font));
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
            // Binary search on text element boundaries for the longest prefix that fits together with the ellipsis
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

        var run = GetText(Sanitize(text), available, header);
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
        canvas.Save();
        canvas.ClipRect(ToSkRect(rect));
        DrawRun(canvas, rect, run, x, header, color);
        canvas.Restore();
    }

    // The sort mark is kept and only the header text is truncated when the header does not fit
    private void DrawHeader(SKCanvas canvas, GridRect rect, GridColumn column, IGridDataView? view, Color color)
    {
        var mark = GetSortMark(style, column, view);
        if (String.IsNullOrEmpty(mark))
        {
            DrawText(canvas, rect, column.Header, TextAlignment.Center, true, color);
            return;
        }

        var available = (float)rect.Width - (style.HorizontalPadding * 2);
        if (rect.IsEmpty || (available <= 0))
        {
            return;
        }

        var end = style.SortMarkPosition == GridSortMarkPosition.End;
        var markRun = GetText(end ? " " + mark : mark + " ", available, true);
        var textRun = (markRun.Width < available) && (column.Header.Length > 0) ? GetText(Sanitize(column.Header), available - markRun.Width, true) : EmptyRun;
        var first = end ? textRun : markRun;
        var second = end ? markRun : textRun;
        canvas.Save();
        canvas.ClipRect(ToSkRect(rect));
        var x = DrawRun(canvas, rect, first, rect.X + ((rect.Width - first.Width - second.Width) / 2), true, color);
        DrawRun(canvas, rect, second, x, true, color);
        canvas.Restore();
    }

    private double DrawRun(SKCanvas canvas, GridRect rect, TextRun run, double x, bool header, Color color)
    {
        var metrics = (header ? primary.HeaderFont : primary.Font).Metrics;
        var baseline = rect.Y + ((rect.Height - (metrics.Descent - metrics.Ascent)) / 2) - metrics.Ascent;
        paint.Style = SKPaintStyle.Fill;
        paint.Color = color.ToSKColor();
        foreach (var part in run.Parts)
        {
            canvas.DrawText(part.Text, (float)x, (float)baseline, SKTextAlign.Left, part.Font, paint);
            x += part.Width;
        }

        return x;
    }

    private void Fill(SKCanvas canvas, GridRect rect, Color color)
    {
        paint.Style = SKPaintStyle.Fill;
        paint.Color = color.ToSKColor();
        canvas.DrawRect(ToSkRect(rect), paint);
    }

    // Marks the right edge of the frozen columns so the boundary to the scrolling columns stays visible
    private void DrawFrozenLine(SKCanvas canvas, GridLayout layout)
    {
        if ((layout.FrozenColumnCount == 0) || (layout.FrozenWidth <= 0))
        {
            return;
        }

        paint.Style = SKPaintStyle.Stroke;
        paint.StrokeWidth = 2;
        paint.Color = style.FrozenLineColor.ToSKColor();
        var x = (float)(layout.RowHeaderWidth + layout.FrozenWidth) - 1;
        canvas.DrawLine(x, 0, x, (float)layout.ViewportHeight, paint);
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

        var area = layout.ScrollArea;
        if ((layout.MaximumScrollX > 0) && !area.IsEmpty)
        {
            var width = Math.Min(area.Width, Math.Max(24, area.Width * area.Width / (layout.ContentWidth - layout.FrozenWidth)));
            var x = area.X + ((area.Width - width) * layout.ScrollX / layout.MaximumScrollX);
            canvas.DrawRoundRect(SKRect.Create((float)x, (float)area.Bottom - 5, (float)width, 4), 2, 2, paint);
        }
    }

    private readonly record struct RowState(bool Selected, Color? Background);

    private sealed record TextSegment(string Text, SKFont Font);

    private sealed record TextRun(IReadOnlyList<TextRun.Part> Parts, float Width)
    {
        public sealed record Part(string Text, float Width, SKFont Font);
    }

    private sealed class FontSet : IDisposable
    {
        private readonly bool ownsTypeface;

        public SKTypeface Typeface { get; }

        public SKFont Font { get; }

        public SKFont HeaderFont { get; }

        public FontSet(SKTypeface typeface, float size, bool ownsTypeface = true)
        {
            this.ownsTypeface = ownsTypeface;
            Typeface = typeface;
            Font = new SKFont(typeface, size);
            HeaderFont = new SKFont(typeface, size) { Embolden = true };
        }

        public void Dispose()
        {
            HeaderFont.Dispose();
            Font.Dispose();
            if (ownsTypeface)
            {
                Typeface.Dispose();
            }
        }
    }
}
