namespace ClamGrid;

[System.ComponentModel.TypeConverter(typeof(GridColumnWidthTypeConverter))]
public readonly record struct GridColumnWidth
{
    public static GridColumnWidth Auto => default;

    public double Value { get; }

    public GridColumnWidthUnit Unit { get; }

    private GridColumnWidth(double value, GridColumnWidthUnit unit)
    {
        Value = value;
        Unit = unit;
    }

    public static GridColumnWidth Absolute(double value)
    {
        if (!Double.IsFinite(value) || (value < 0))
        {
            throw new ArgumentOutOfRangeException(nameof(value), value, "The width must be finite and non-negative.");
        }

        return new GridColumnWidth(value, GridColumnWidthUnit.Absolute);
    }

    public static GridColumnWidth Star(double weight = 1)
    {
        if (!Double.IsFinite(weight) || (weight <= 0))
        {
            throw new ArgumentOutOfRangeException(nameof(weight), weight, "The weight must be finite and positive.");
        }

        return new GridColumnWidth(weight, GridColumnWidthUnit.Star);
    }
}
