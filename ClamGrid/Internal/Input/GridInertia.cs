namespace ClamGrid.Internal.Input;

using ClamGrid.Internal.Layout;

internal sealed class GridInertia
{
    private const double Friction = 5;
    private const double MinimumSpeed = 40;

    public Point Velocity { get; private set; }

    public bool IsRunning => Velocity != default;

    public void Start(Point velocity) => Velocity = new Point(Normalize(velocity.X), Normalize(velocity.Y));

    public void Stop() => Velocity = default;

    public Point Step(double seconds)
    {
        if (!Double.IsFinite(seconds) || (seconds <= 0))
        {
            return default;
        }

        if (seconds > 0.1)
        {
            Stop();
            return default;
        }

        var decay = Math.Exp(-Friction * seconds);
        var distance = (1 - decay) / Friction;
        var delta = new Point(Velocity.X * distance, Velocity.Y * distance);
        Velocity = new Point(Normalize(Velocity.X * decay), Normalize(Velocity.Y * decay));
        return delta;
    }

    public void StopAtBounds(GridLayout layout)
    {
        ArgumentNullException.ThrowIfNull(layout);
        var x = ((Velocity.X < 0) && (layout.ScrollX <= 0)) || ((Velocity.X > 0) && (layout.ScrollX >= layout.MaximumScrollX)) ? 0 : Velocity.X;
        var y = ((Velocity.Y < 0) && (layout.ScrollY <= 0)) || ((Velocity.Y > 0) && (layout.ScrollY >= layout.MaximumScrollY)) ? 0 : Velocity.Y;
        Velocity = new Point(x, y);
    }

    private static double Normalize(double value) => Double.IsFinite(value) && (Math.Abs(value) >= MinimumSpeed) ? Math.Clamp(value, -3500, 3500) : 0;
}
