namespace ClamGrid;

/// <summary>文字色と背景色。nullの色は既定色を引き継ぐ。</summary>
public readonly record struct GridColors(Color? TextColor = null, Color? Background = null)
{
    internal GridColors Apply(GridColors overrides) => new(overrides.TextColor ?? TextColor, overrides.Background ?? Background);
}
