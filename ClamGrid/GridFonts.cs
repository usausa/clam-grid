namespace ClamGrid;

using SkiaSharp;

// Global font fallback settings, read when a grid creates its renderer, so configure them at startup
public static class GridFonts
{
    // BCP-47 language tags passed to the per character system font lookup in priority order
    public static IReadOnlyList<string> Languages { get; set; } = ["ja"];

    // Typefaces tried in order before the system lookup for characters the primary font lacks; the caller keeps ownership
    public static IReadOnlyList<SKTypeface> Fallbacks { get; set; } = [];
}
