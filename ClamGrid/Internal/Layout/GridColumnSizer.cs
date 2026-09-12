namespace ClamGrid.Internal.Layout;

internal static class GridColumnSizer
{
    public static double[] Resolve(IReadOnlyList<GridColumnWidthSpec> columns, double availableWidth)
    {
        ArgumentNullException.ThrowIfNull(columns);
        if (!Double.IsFinite(availableWidth) || (availableWidth < 0))
        {
            throw new ArgumentOutOfRangeException(nameof(availableWidth));
        }

        var widths = new double[columns.Count];
        var stars = new List<int>();
        var remaining = availableWidth;
        for (var i = 0; i < columns.Count; i++)
        {
            var column = columns[i];
            if (!Double.IsFinite(column.Minimum) || (column.Minimum < 0) || !Double.IsFinite(column.Measured) || (column.Measured < 0))
            {
                throw new ArgumentException("Column measurements must be finite and non-negative.", nameof(columns));
            }

            if (column.Width.Unit == GridColumnWidthUnit.Star)
            {
                stars.Add(i);
            }
            else
            {
                widths[i] = Math.Max(column.Minimum, column.Width.Unit == GridColumnWidthUnit.Auto ? column.Measured : column.Width.Value);
                remaining -= widths[i];
            }
        }

        while (stars.Count > 0)
        {
            var maximumWeight = stars.Max(index => columns[index].Width.Value);
            var totalWeight = stars.Sum(index => columns[index].Width.Value / maximumWeight);
            var distributable = Math.Max(0, remaining);
            var pinned = false;
            for (var i = stars.Count - 1; i >= 0; i--)
            {
                var index = stars[i];
                var share = distributable * (columns[index].Width.Value / maximumWeight / totalWeight);
                if (share < columns[index].Minimum)
                {
                    widths[index] = columns[index].Minimum;
                    remaining -= widths[index];
                    stars.RemoveAt(i);
                    pinned = true;
                }
            }

            if (!pinned)
            {
                foreach (var index in stars)
                {
                    widths[index] = Math.Max(0, remaining) * (columns[index].Width.Value / maximumWeight / totalWeight);
                }

                break;
            }
        }

        return widths;
    }
}
