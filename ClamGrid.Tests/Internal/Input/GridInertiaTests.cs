namespace ClamGrid.Tests.Internal.Input;

public sealed class GridInertiaTests
{
    [Fact]
    public void InertiaDecaysStopsAtEachBoundaryAndRejectsLongFrameGaps()
    {
        var inertia = new GridInertia();
        inertia.Start(new Point(1000, 500));
        var first = inertia.Step(0.016);
        Assert.InRange(first.X, 15, 16);
        Assert.InRange(inertia.Velocity.X, 900, 1000);
        var layout = new GridLayout([100, 100], 100, 40, 40, 40, 140, 240);
        layout.ScrollTo(layout.MaximumScrollX, 0);
        inertia.StopAtBounds(layout);
        Assert.Equal(0, inertia.Velocity.X);
        Assert.True(inertia.Velocity.Y > 0);
        Assert.Equal(default, inertia.Step(0.2));
        Assert.False(inertia.IsRunning);
        inertia.Start(new Point(10, 20));
        Assert.False(inertia.IsRunning);
        inertia.Start(new Point(0, 1000));
        for (var i = 0; i < 100; i++)
        {
            inertia.Step(0.016);
        }

        Assert.False(inertia.IsRunning);
    }
}
