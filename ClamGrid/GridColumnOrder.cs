namespace ClamGrid;

public sealed record GridColumnOrder(string Key, bool IsVisible)
{
    // Absolute width in DIP saved for the column; null uses the width of the definition
    public double? Width { get; init; }
}
