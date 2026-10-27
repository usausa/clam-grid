namespace Example.Modules.Grid;

public sealed class SparklineDrawing : IDisposable
{
    private const float LineThickness = 1.5f;

    private const float MarkerRadius = 2.5f;

    private const float Gap = 1;

    private const byte AreaAlpha = 0x33;

    private readonly SKPaint fill = new() { IsAntialias = true, Style = SKPaintStyle.Fill };

    private readonly SKPaint stroke = new()
    {
        IsAntialias = true,
        Style = SKPaintStyle.Stroke,
        StrokeWidth = LineThickness,
        StrokeCap = SKStrokeCap.Round,
        StrokeJoin = SKStrokeJoin.Round
    };

    private SKPoint[] points = [];

    public void Dispose()
    {
        fill.Dispose();
        stroke.Dispose();
    }

    public void DrawTrend(SKCanvas canvas, SKRect bounds, ReadOnlySpan<double> values, SparklineKind kind, SKColor color)
    {
        if (values.Length == 0)
        {
            return;
        }

        var (minimum, range) = GetRange(values);
        if (kind == SparklineKind.Bar)
        {
            DrawBars(canvas, bounds, values, minimum, range, color);
            return;
        }

        UpdatePoints(bounds, values, minimum, range);
        if (kind == SparklineKind.Area)
        {
            fill.Color = color.WithAlpha(AreaAlpha);
            canvas.DrawPath(CreateArea(bounds), fill);
        }

        stroke.Color = color;
        canvas.DrawPoints(SKPointMode.Polygon, points, stroke);
        fill.Color = color;
        canvas.DrawCircle(points[^1], MarkerRadius, fill);
    }

    public void DrawWinLoss(SKCanvas canvas, SKRect bounds, ReadOnlySpan<double> values, SKColor positive, SKColor negative)
    {
        if (values.Length == 0)
        {
            return;
        }

        var middle = bounds.MidY;
        var extent = (bounds.Height / 2) - (Gap / 2);
        var width = (bounds.Width / values.Length) - Gap;
        for (var i = 0; i < values.Length; i++)
        {
            var value = values[i];
            if ((value > 0) || (value < 0))
            {
                var left = bounds.Left + (bounds.Width * i / values.Length);
                fill.Color = value > 0 ? positive : negative;
                var top = value > 0 ? middle - extent : middle + (Gap / 2);
                var bottom = value > 0 ? middle - (Gap / 2) : middle + extent;
                canvas.DrawRect(new SKRect(left, top, left + width, bottom), fill);
            }
        }
    }

    public void DrawProgress(SKCanvas canvas, SKRect bounds, double ratio, SKColor track, SKColor color)
    {
        fill.Color = track;
        canvas.DrawRect(bounds, fill);
        fill.Color = color;
        canvas.DrawRect(new SKRect(bounds.Left, bounds.Top, bounds.Left + (float)(bounds.Width * Math.Clamp(ratio, 0, 1)), bounds.Bottom), fill);
    }

    private void DrawBars(SKCanvas canvas, SKRect bounds, ReadOnlySpan<double> values, double minimum, double range, SKColor color)
    {
        var width = (bounds.Width / values.Length) - Gap;
        fill.Color = color;
        for (var i = 0; i < values.Length; i++)
        {
            var left = bounds.Left + (bounds.Width * i / values.Length);
            var top = (float)(bounds.Bottom - (bounds.Height * (values[i] - minimum) / range));
            canvas.DrawRect(new SKRect(left, Math.Min(top, bounds.Bottom - 1), left + width, bounds.Bottom), fill);
        }
    }

    private SKPath CreateArea(SKRect bounds)
    {
        using var builder = new SKPathBuilder();
        builder.MoveTo(points[0].X, bounds.Bottom);
        foreach (var point in points)
        {
            builder.LineTo(point);
        }

        builder.LineTo(points[^1].X, bounds.Bottom);
        builder.Close();
        return builder.Detach();
    }

    private void UpdatePoints(SKRect bounds, ReadOnlySpan<double> values, double minimum, double range)
    {
        if (points.Length != values.Length)
        {
            points = new SKPoint[values.Length];
        }

        var step = values.Length > 1 ? bounds.Width / (values.Length - 1) : 0;
        for (var i = 0; i < values.Length; i++)
        {
            points[i] = new SKPoint(bounds.Left + (step * i), (float)(bounds.Bottom - (bounds.Height * (values[i] - minimum) / range)));
        }
    }

    private static (double Minimum, double Range) GetRange(ReadOnlySpan<double> values)
    {
        var minimum = Double.MaxValue;
        var maximum = Double.MinValue;
        foreach (var value in values)
        {
            minimum = Math.Min(minimum, value);
            maximum = Math.Max(maximum, value);
        }

        var range = maximum - minimum;
        return (minimum, range > 0 ? range : 1);
    }
}
