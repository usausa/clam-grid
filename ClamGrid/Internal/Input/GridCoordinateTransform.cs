namespace ClamGrid.Internal.Input;

using ClamGrid.Internal.Layout;

internal readonly record struct GridCoordinateTransform
{
    public double ScaleX { get; }

    public double ScaleY { get; }

    public GridCoordinateTransform(double scaleX, double scaleY)
    {
        if (!Double.IsFinite(scaleX) || (scaleX <= 0))
        {
            throw new ArgumentOutOfRangeException(nameof(scaleX));
        }

        if (!Double.IsFinite(scaleY) || (scaleY <= 0))
        {
            throw new ArgumentOutOfRangeException(nameof(scaleY));
        }

        ScaleX = scaleX;
        ScaleY = scaleY;
    }

    public Point ToDip(double x, double y) => new(x / ScaleX, y / ScaleY);

    public GridRect ToPixels(GridRect rect) => new(rect.X * ScaleX, rect.Y * ScaleY, rect.Width * ScaleX, rect.Height * ScaleY);
}
