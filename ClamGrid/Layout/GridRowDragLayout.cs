namespace ClamGrid.Layout;

internal static class GridRowDragLayout
{
    public static int GetInsertionIndex(GridLayout layout, double y)
    {
        ArgumentNullException.ThrowIfNull(layout);
        if (!Double.IsFinite(y))
        {
            return -1;
        }

        return (int)Math.Clamp(Math.Floor(((y - layout.HeaderHeight + layout.ScrollY) / layout.RowHeight) + 0.5), 0, layout.RowCount);
    }

    public static int GetTargetIndex(int from, int insertion, int count) => (from < 0) || (from >= count) || (insertion < 0) || (insertion > count)
        ? -1 : insertion > from ? insertion - 1 : insertion;

    public static double GetScrollVelocity(GridLayout layout, Point point)
    {
        ArgumentNullException.ThrowIfNull(layout);
        if ((point.X < 0) || (point.X >= layout.ViewportWidth) || (point.Y < layout.HeaderHeight) || (point.Y >= layout.ViewportHeight))
        {
            return 0;
        }

        var margin = Math.Min(32, layout.BodyBounds.Height / 2);
        if (margin <= 0)
        {
            return 0;
        }

        if (point.Y < layout.HeaderHeight + margin)
        {
            return -480 * (1 - ((point.Y - layout.HeaderHeight) / margin));
        }

        return point.Y > layout.ViewportHeight - margin ? 480 * (1 - ((layout.ViewportHeight - point.Y) / margin)) : 0;
    }
}
