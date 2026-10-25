namespace ClamGrid.Rendering;

using System.Globalization;

using ClamGrid.Layout;

using SkiaExtension;

using SkiaSharp;
using SkiaSharp.Views.Maui;

internal sealed class GridRenderer : IDisposable
{
    private const int RowNumberCacheLimit = 2048;
    private const int CellLineCacheLimit = 2048;

    private readonly GridStyle style;
    private readonly FontResolver fonts;
    private readonly float fontSize;
    private readonly SKPaint paint = new() { IsAntialias = true };
    private readonly Dictionary<string, string[]> headerLines = [with(StringComparer.Ordinal)];
    private readonly Dictionary<string, string[]> cellLines = [with(StringComparer.Ordinal)];
    private readonly Dictionary<(bool Descending, int Number), string> headerMarks = [];
    private readonly Dictionary<int, string> rowNumbers = [];
    private RowState[] rowStates = [];

    public int Measurements => fonts.Measurements;

    // Includes every font resolved so far, so fonts found while measuring the headers and sample rows widen the automatic height
    public double AutoRowHeight => GetAutoHeight(1);

    private float TextLineHeight => fonts.GetMaxLineHeight(fontSize, false);

    public GridRenderer(GridStyle style)
    {
        style.Validate();
        this.style = style;
        fonts = new FontResolver(style.FontFamily);
        fontSize = (float)style.FontSize;
    }

    public static string GetCellText(GridColumn column, object? value) =>
        column.TextFormat.Format(column.Converter is { } converter ? converter.Convert(value, typeof(string), null, CultureInfo.CurrentCulture) : value, CultureInfo.CurrentCulture);

    public double[] MeasureColumns(IReadOnlyList<GridColumn> columns, IReadOnlyList<object> items, double availableWidth, int sampleSize, IGridDataView? dataView)
    {
        var specs = new GridColumnWidthSpec[columns.Count];
        for (var columnIndex = 0; columnIndex < columns.Count; columnIndex++)
        {
            var column = columns[columnIndex];
            var mark = GetHeaderMark(column, dataView);
            var width = GetTextLines(column.Header).Max(line => Measure(style.SortMarkPosition == GridSortMarkPosition.End ? line + mark : mark + line, true));
            if ((column.Width.Unit == GridColumnWidthUnit.Auto) && (column.CellPainter is null))
            {
                for (var row = 0; row < Math.Min(sampleSize, items.Count); row++)
                {
                    width = Math.Max(width, column.IsBoolean ? 24 : MeasureCell(column, column.ValueAccessor.GetValue(items[row])));
                }
            }

            specs[columnIndex] = new GridColumnWidthSpec(column.Width, column.MinWidth, width + (style.HorizontalPadding * 2));
        }

        return GridColumnSizer.Resolve(specs, availableWidth);
    }

    // Text lines with the vertical padding; a single line gives the automatic row height
    public double GetAutoHeight(int lines) => Math.Ceiling((TextLineHeight * Math.Max(1, lines)) + (style.VerticalPadding * 2));

    public int Render(SKCanvas canvas, GridLayout layout, IReadOnlyList<GridColumn> columns, IReadOnlyList<object> items, IGridDataView? dataView, bool showRowHandles)
    {
        var rows = layout.VisibleRows;
        if (rowStates.Length < rows.Count)
        {
            rowStates = new RowState[rows.Count];
        }

        var states = rowStates;
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
            DrawText(canvas, rect, GetRowHeaderText(items[row], row, selected), style.RowHeaderAlignment, colors.TextColor!);
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
        DrawHeaderText(canvas, corner, style.CornerText, String.Empty, style.HeaderTextColor);
        DrawLines(canvas, corner);
        DrawFrozenLine(canvas, layout);
        DrawScrollBars(canvas, layout);
        return rendered;
    }

    public void DrawEmptyText(SKCanvas canvas, string value, GridLayout layout) => DrawText(canvas, layout.BodyBounds, value, TextAlignment.Center, style.EmptyTextColor);

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
                var colors = new GridColors(state.Selected ? style.SelectedTextColor : definition.TextColor ?? style.TextColor, GetCellBackground(style, definition, row, state.Background));
                colors = colors.Apply(style.CellColors?.Invoke(new GridCellColorContext(items[row], row, definition, column, value, state.Selected, colors)) ?? default);
                Fill(canvas, rect, colors.Background!);
                if (definition.CellPainter is { } painter)
                {
                    PaintCell(canvas, painter, new GridCellPaintContext(ToSkRect(rect), items[row], row, definition, column, value, state.Selected, colors));
                }
                else if (definition.IsBoolean)
                {
                    DrawBoolean(canvas, rect, value is true, colors.TextColor!, colors.Background!);
                }
                else
                {
                    DrawCellText(canvas, rect, GetCellText(definition, value), definition, colors.TextColor!);
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
            DrawHeaderText(canvas, rect, definition.Header, GetHeaderMark(definition, dataView), colors.TextColor!);
            DrawLines(canvas, rect);
        }

        canvas.Restore();
    }

    public void ClearCache()
    {
        fonts.ClearCache();
        headerLines.Clear();
        cellLines.Clear();
    }

    public void Dispose()
    {
        paint.Dispose();
        fonts.Dispose();
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

    // Selection and row callback colors win over the column color, which wins over the alternating and base backgrounds
    internal static Color GetCellBackground(GridStyle style, GridColumn column, int rowIndex, Color? rowBackground) =>
        rowBackground ?? column.Background ?? ((rowIndex % 2) == 1 ? style.AlternatingRowBackground : null) ?? style.Background;

    // Headers and corner texts use every line; cells use the first LineCount of them
    internal static string[] GetTextLines(string text) => text.ReplaceLineEndings("\n").Split('\n');

    internal string GetRowHeaderText(object item, int rowIndex, bool selected) =>
        style.RowHeaderText?.Invoke(new GridRowHeaderTextContext(item, rowIndex, selected)) ?? GetRowNumber(rowIndex);

    // The sort mark with the space that separates it from the header text, or an empty string
    internal string GetHeaderMark(GridColumn column, IGridDataView? view)
    {
        if (GetSortMarkKey(style, column, view) is not { } key)
        {
            return String.Empty;
        }

        if (!headerMarks.TryGetValue(key, out var mark))
        {
            mark = FormatHeaderMark(style, key.Descending, key.Number);
            headerMarks[key] = mark;
        }

        return mark;
    }

    // The primary key always gets a mark; secondary keys get a mark with their priority only with SortPriorityVisible
    private static (bool Descending, int Number)? GetSortMarkKey(GridStyle style, GridColumn column, IGridDataView? view)
    {
        var priority = GetSortPriority(column, view);
        if ((priority < 0) || ((priority > 0) && !style.SortPriorityVisible))
        {
            return null;
        }

        return (view!.SortOrders[priority].Descending, style.SortPriorityVisible && (view.SortOrders.Count > 1) ? priority + 1 : 0);
    }

    private static string FormatHeaderMark(GridStyle style, bool descending, int number)
    {
        var mark = descending ? style.DescendingSortMark : style.AscendingSortMark;
        if (number > 0)
        {
            mark += number.ToString(CultureInfo.InvariantCulture);
        }

        if (String.IsNullOrEmpty(mark))
        {
            return String.Empty;
        }

        return style.SortMarkPosition == GridSortMarkPosition.End ? " " + mark : mark + " ";
    }

    private static SKRect ToSkRect(GridRect rect) => new((float)rect.X, (float)rect.Y, (float)rect.Right, (float)rect.Bottom);

    private static void PaintCell(SKCanvas canvas, Action<SKCanvas, GridCellPaintContext> painter, in GridCellPaintContext context)
    {
        var count = canvas.Save();
        canvas.ClipRect(context.Bounds);
        painter(canvas, context);
        canvas.RestoreToCount(count);
    }

    private float Measure(string text, bool header) => fonts.CreateText(text, fontSize, header).Width;

    private float MeasureCell(GridColumn column, object? value)
    {
        var text = GetCellText(column, value);
        if (column.LineCount == 1)
        {
            return Measure(text, false);
        }

        var width = 0f;
        var lines = GetCachedCellLines(text);
        var count = column.LineCount > 0 ? Math.Min(lines.Length, column.LineCount) : lines.Length;
        for (var i = 0; i < count; i++)
        {
            width = Math.Max(width, Measure(lines[i], false));
        }

        return width;
    }

    private (double X, SKTextAlign Align) GetTextPosition(GridRect rect, TextAlignment alignment) => alignment switch
    {
        TextAlignment.Center => (rect.CenterX, SKTextAlign.Center),
        TextAlignment.End => (rect.Right - style.HorizontalPadding, SKTextAlign.Right),
        _ => (rect.X + style.HorizontalPadding, SKTextAlign.Left)
    };

    private void DrawText(SKCanvas canvas, GridRect rect, string text, TextAlignment alignment, Color color)
    {
        var available = (float)(rect.Width - (style.HorizontalPadding * 2));
        if (rect.IsEmpty || (available <= 0) || (text.Length == 0))
        {
            return;
        }

        var drawableText = fonts.CreateText(text, fontSize, false, available);
        if (drawableText.Segments.Count == 0)
        {
            return;
        }

        var (x, textAlign) = GetTextPosition(rect, alignment);
        var baseline = fonts.GetMetrics(fontSize, false).GetCenterBaseline((float)rect.CenterY);
        paint.Style = SKPaintStyle.Fill;
        paint.Color = color.ToSKColor();
        canvas.Save();
        canvas.ClipRect(ToSkRect(rect));
        canvas.DrawText(drawableText, (float)x, baseline, textAlign, paint);
        canvas.Restore();
    }

    // Lines are stacked from the vertical alignment and truncated on their own; LineCount caps them and 0 fits as many as the row height allows
    private void DrawCellText(SKCanvas canvas, GridRect rect, string text, GridColumn column, Color color)
    {
        var available = (float)(rect.Width - (style.HorizontalPadding * 2));
        if (rect.IsEmpty || (available <= 0) || (text.Length == 0))
        {
            return;
        }

        var lineHeight = (double)TextLineHeight;
        var allowed = column.LineCount > 0 ? column.LineCount : Math.Max(1, (int)((rect.Height - (style.VerticalPadding * 2)) / lineHeight));
        var lines = allowed > 1 ? GetCachedCellLines(text) : null;
        var count = lines is null ? 1 : Math.Min(lines.Length, allowed);
        var top = column.VerticalAlignment switch
        {
            TextAlignment.Start => rect.Y + style.VerticalPadding,
            TextAlignment.End => rect.Bottom - style.VerticalPadding - (lineHeight * count),
            _ => rect.Y + ((rect.Height - (lineHeight * count)) / 2)
        };
        var (x, textAlign) = GetTextPosition(rect, column.Alignment);
        var metrics = fonts.GetMetrics(fontSize, false);
        paint.Style = SKPaintStyle.Fill;
        paint.Color = color.ToSKColor();
        canvas.Save();
        canvas.ClipRect(ToSkRect(rect));
        for (var i = 0; i < count; i++)
        {
            canvas.DrawText(fonts.CreateText(lines is null ? text : lines[i], fontSize, false, available), (float)x, metrics.GetCenterBaseline((float)(top + (lineHeight * (i + 0.5)))), textAlign, paint);
        }

        canvas.Restore();
    }

    // Lines are centered one under another and truncated on their own; the sort mark stays whole beside them at the vertical center
    private void DrawHeaderText(SKCanvas canvas, GridRect rect, string text, string mark, Color color)
    {
        var available = (float)(rect.Width - (style.HorizontalPadding * 2));
        if (rect.IsEmpty || (available <= 0))
        {
            return;
        }

        var markText = fonts.CreateText(mark, fontSize, true, available);
        var lines = markText.Width < available ? GetCachedHeaderLines(text) : [];
        var lineWidth = available - markText.Width;
        var textWidth = 0f;
        foreach (var line in lines)
        {
            textWidth = Math.Max(textWidth, fonts.CreateText(line, fontSize, true, lineWidth).Width);
        }

        var end = style.SortMarkPosition == GridSortMarkPosition.End;
        var left = rect.X + ((rect.Width - markText.Width - textWidth) / 2);
        var textLeft = end ? left : left + markText.Width;
        var lineHeight = (double)TextLineHeight;
        var top = rect.Y + ((rect.Height - (lineHeight * lines.Length)) / 2);
        var metrics = fonts.GetMetrics(fontSize, true);
        paint.Style = SKPaintStyle.Fill;
        paint.Color = color.ToSKColor();
        canvas.Save();
        canvas.ClipRect(ToSkRect(rect));
        canvas.DrawText(markText, (float)(end ? left + textWidth : left), metrics.GetCenterBaseline((float)rect.CenterY), SKTextAlign.Left, paint);
        for (var i = 0; i < lines.Length; i++)
        {
            canvas.DrawText(fonts.CreateText(lines[i], fontSize, true, lineWidth), (float)(textLeft + (textWidth / 2)), metrics.GetCenterBaseline((float)(top + (lineHeight * (i + 0.5)))), SKTextAlign.Center, paint);
        }

        canvas.Restore();
    }

    private string[] GetCachedHeaderLines(string text)
    {
        if (!headerLines.TryGetValue(text, out var lines))
        {
            lines = GetTextLines(text);
            headerLines[text] = lines;
        }

        return lines;
    }

    private string[] GetCachedCellLines(string text)
    {
        if (!cellLines.TryGetValue(text, out var lines))
        {
            if (cellLines.Count >= CellLineCacheLimit)
            {
                cellLines.Clear();
            }

            lines = GetTextLines(text);
            cellLines[text] = lines;
        }

        return lines;
    }

    private string GetRowNumber(int rowIndex)
    {
        if (!rowNumbers.TryGetValue(rowIndex, out var number))
        {
            if (rowNumbers.Count >= RowNumberCacheLimit)
            {
                rowNumbers.Clear();
            }

            number = (rowIndex + 1).ToString(CultureInfo.InvariantCulture);
            rowNumbers[rowIndex] = number;
        }

        return number;
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
        paint.StrokeWidth = (float)style.GridLineWidth;
        paint.Color = style.GridLineColor.ToSKColor();
        if (style.HorizontalLinesVisible)
        {
            canvas.DrawLine((float)rect.X, (float)rect.Bottom, (float)rect.Right, (float)rect.Bottom, paint);
        }

        if (style.VerticalLinesVisible)
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

    private void DrawScrollBars(SKCanvas canvas, GridLayout layout)
    {
        if (!style.ScrollBarVisible)
        {
            return;
        }

        var thickness = (float)style.ScrollBarThickness;
        var radius = thickness / 2;
        var offset = thickness + 1;
        var body = layout.BodyBounds;
        paint.Style = SKPaintStyle.Fill;
        paint.Color = style.ScrollBarColor.ToSKColor();
        if ((layout.MaximumScrollY > 0) && !body.IsEmpty)
        {
            var height = Math.Min(body.Height, Math.Max(24, body.Height * body.Height / layout.ContentHeight));
            var y = body.Y + ((body.Height - height) * layout.ScrollY / layout.MaximumScrollY);
            canvas.DrawRoundRect(SKRect.Create((float)body.Right - offset, (float)y, thickness, (float)height), radius, radius, paint);
        }

        var area = layout.ScrollArea;
        if ((layout.MaximumScrollX > 0) && !area.IsEmpty)
        {
            var width = Math.Min(area.Width, Math.Max(24, area.Width * area.Width / (layout.ContentWidth - layout.FrozenWidth)));
            var x = area.X + ((area.Width - width) * layout.ScrollX / layout.MaximumScrollX);
            canvas.DrawRoundRect(SKRect.Create((float)x, (float)area.Bottom - offset, (float)width, thickness), radius, radius, paint);
        }
    }

    private readonly record struct RowState(bool Selected, Color? Background);
}
