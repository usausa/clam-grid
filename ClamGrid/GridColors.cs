namespace ClamGrid;

// Text and background colors; a null color inherits the default
public readonly record struct GridColors(Color? TextColor = null, Color? Background = null)
{
    internal GridColors Apply(GridColors overrides) => new(overrides.TextColor ?? TextColor, overrides.Background ?? Background);
}
