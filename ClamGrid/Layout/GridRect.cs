namespace ClamGrid.Layout;

internal readonly record struct GridRect(double X, double Y, double Width, double Height)
{
    public double Right => X + Width;

    public double Bottom => Y + Height;

    public bool IsEmpty => (Width <= 0) || (Height <= 0);

    public bool Contains(double x, double y) => !IsEmpty && (x >= X) && (x < Right) && (y >= Y) && (y < Bottom);

    public GridRect Intersect(GridRect other)
    {
        var left = Math.Max(X, other.X);
        var top = Math.Max(Y, other.Y);
        return new GridRect(left, top, Math.Max(0, Math.Min(Right, other.Right) - left), Math.Max(0, Math.Min(Bottom, other.Bottom) - top));
    }
}
