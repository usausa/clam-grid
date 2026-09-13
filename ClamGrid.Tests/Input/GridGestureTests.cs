namespace ClamGrid.Tests.Input;

public sealed class GridGestureTests
{
    [Fact]
    public void SmallMovementRemainsATapButLeavingTheViewDoesNot()
    {
        // Arrange
        var input = new GridGestureController();
        input.Press(1, new Point(50, 50), 0);

        // Act & Assert
        Assert.Equal(GridGestureAction.None, input.Move(1, new Point(54, 55), 50).Action);
        Assert.Equal(GridGestureAction.Tap, input.Release(1, new Point(54, 55), 100, true).Action);

        // Arrange
        input.Press(1, new Point(2, 50), 200);

        // Act & Assert
        Assert.Equal(GridGestureAction.None, input.Release(1, new Point(-1, 50), 210, false).Action);
    }

    [Fact]
    public void LongPressFiresOnceAndNeverBecomesATapOrPan()
    {
        // Arrange
        var input = new GridGestureController();
        input.Press(1, new Point(50, 50), 0);

        // Act & Assert
        Assert.Equal(GridGestureAction.None, input.Tick(499).Action);
        Assert.Equal(GridGestureAction.LongPress, input.Tick(500).Action);
        Assert.Equal(GridGestureAction.None, input.Tick(600).Action);
        Assert.Equal(GridGestureAction.None, input.Move(1, new Point(50, 100), 610).Action);
        Assert.Equal(GridGestureAction.None, input.Release(1, new Point(50, 100), 650, true).Action);
        Assert.Equal(GridGestureState.Idle, input.State);
    }

    [Fact]
    public void PanLocksDominantDirectionAndDoesNotLongPressOrTap()
    {
        // Arrange
        var input = new GridGestureController();
        input.Press(1, new Point(50, 100), 0);

        // Act
        var move = input.Move(1, new Point(52, 80), 20);

        // Assert
        Assert.Equal(GridGestureAction.Pan, move.Action);
        Assert.Equal(new Point(0, 20), move.Delta);

        // Act & Assert
        Assert.Equal(new Point(0, 20), input.Move(1, new Point(80, 60), 40).Delta);
        Assert.Equal(GridGestureAction.None, input.Tick(600).Action);

        // Act
        var release = input.Release(1, new Point(80, 60), 60, true);

        // Assert
        Assert.Equal(GridGestureAction.PanCompleted, release.Action);
        Assert.Equal(0, release.Velocity.X);
        Assert.True(release.Velocity.Y > 0);
    }

    [Fact]
    public void DiagonalPanKeepsBothAxesAndHeadersCanConstrainToHorizontal()
    {
        // Arrange
        var input = new GridGestureController();
        input.Press(1, new Point(50, 50), 0);

        // Act & Assert
        Assert.Equal(new Point(10, 10), input.Move(1, new Point(40, 40), 20).Delta);

        // Arrange
        input.Cancel();
        input.Press(1, new Point(50, 50), 0, panAxis: GridPanAxis.Horizontal);

        // Act & Assert
        Assert.Equal(new Point(10, 0), input.Move(1, new Point(40, 10), 20).Delta);
    }

    [Fact]
    public void PauseBeforeReleaseDoesNotStartInertia()
    {
        // Arrange
        var input = new GridGestureController();
        input.Press(1, new Point(50, 100), 0);
        input.Move(1, new Point(50, 50), 50);

        // Act
        var release = input.Release(1, new Point(50, 50), 500, true);

        // Assert
        Assert.Equal(default, release.Velocity);
    }

    [Fact]
    public void BoundaryReservesTheWholeGestureAndResizeUsesTotalDisplacement()
    {
        // Arrange
        var input = new GridGestureController();
        input.Press(1, new Point(100, 20), 0, true);

        // Act & Assert
        Assert.Equal(GridGestureAction.None, input.Tick(600).Action);
        Assert.Equal(GridGestureAction.None, input.Release(1, new Point(100, 20), 610, true).Action);

        // Arrange
        input.Press(1, new Point(100, 20), 700, true);

        // Act & Assert
        Assert.Equal(new GridGestureResult(GridGestureAction.Resize, new Point(40, 2)), input.Move(1, new Point(140, 22), 750));
        Assert.Equal(new GridGestureResult(GridGestureAction.ResizeCompleted, new Point(50, 0)), input.Release(1, new Point(150, 22), 800, true));
    }

    [Fact]
    public void ResizeOutsideAndOsCancelDiscardTheOperation()
    {
        // Arrange
        var input = new GridGestureController();
        input.Press(1, new Point(100, 20), 0, true);
        input.Move(1, new Point(140, 20), 20);

        // Act & Assert
        Assert.Equal(GridGestureAction.None, input.Release(1, new Point(1000, 20), 30, false).Action);

        // Arrange
        input.Press(1, new Point(100, 20), 100);
        input.Cancel();

        // Act & Assert
        Assert.Equal(GridGestureAction.None, input.Release(1, new Point(100, 20), 120, true).Action);
        Assert.Equal(GridGestureAction.None, input.Tick(1000).Action);
    }

    [Fact]
    public void SecondPointerBlocksAllRemainingPointersUntilTheyLift()
    {
        // Arrange
        var input = new GridGestureController();
        input.Press(1, new Point(50, 50), 0);

        // Act & Assert
        Assert.Equal(GridGestureAction.Canceled, input.Press(2, new Point(60, 60), 10).Action);
        Assert.Equal(GridGestureState.Blocked, input.State);
        Assert.Equal(GridGestureAction.None, input.Release(1, new Point(50, 50), 20, true).Action);
        Assert.Equal(GridGestureAction.None, input.Tick(600).Action);
        Assert.Equal(GridGestureAction.None, input.Move(2, new Point(100, 100), 610).Action);
        Assert.Equal(GridGestureAction.None, input.Release(2, new Point(100, 100), 620, true).Action);

        // Arrange
        input.Press(3, new Point(50, 50), 700);

        // Act & Assert
        Assert.Equal(GridGestureAction.Tap, input.Release(3, new Point(50, 50), 750, true).Action);
    }

    [Fact]
    public void DataInvalidationBlocksUntilReleaseAndIgnoresUnrelatedPointer()
    {
        // Arrange
        var input = new GridGestureController();
        input.Press(1, new Point(50, 50), 0);

        // Act & Assert
        Assert.Equal(GridGestureAction.None, input.Release(99, new Point(50, 50), 10, true).Action);

        // Arrange
        input.Block();

        // Act & Assert
        Assert.Equal(GridGestureAction.None, input.Release(1, new Point(50, 50), 20, true).Action);
        Assert.Equal(GridGestureState.Idle, input.State);
    }

    [Fact]
    public void InertiaDecaysStopsAtEachBoundaryAndRejectsLongFrameGaps()
    {
        // Arrange
        var inertia = new GridInertia();
        inertia.Start(new Point(1000, 500));

        // Act
        var first = inertia.Step(0.016);

        // Assert
        Assert.InRange(first.X, 15, 16);
        Assert.InRange(inertia.Velocity.X, 900, 1000);

        // Arrange
        var layout = new GridLayout([100, 100], 100, 40, 40, 40, 140, 240);
        layout.ScrollTo(layout.MaximumScrollX, 0);

        // Act
        inertia.StopAtBounds(layout);

        // Assert
        Assert.Equal(0, inertia.Velocity.X);
        Assert.True(inertia.Velocity.Y > 0);

        // Act & Assert
        Assert.Equal(default, inertia.Step(0.2));
        Assert.False(inertia.IsRunning);

        // Act
        inertia.Start(new Point(10, 20));

        // Assert
        Assert.False(inertia.IsRunning);

        // Act
        inertia.Start(new Point(0, 1000));
        for (var i = 0; i < 100; i++)
        {
            inertia.Step(0.016);
        }

        // Assert
        Assert.False(inertia.IsRunning);
    }

    [Fact]
    public void BoundaryHitUsesVisibleHeaderEdgesAfterScrolling()
    {
        // Arrange
        var layout = new GridLayout([100, 80, 120], 10, 40, 32, 48, 260, 200);

        // Act & Assert
        Assert.Equal(0, layout.HitTestColumnBoundary(146, 20));
        Assert.Equal(-1, layout.HitTestColumnBoundary(146, 33));
        Assert.Equal(-1, layout.HitTestColumnBoundary(49, 20));

        // Arrange
        layout.ScrollTo(60, 0);

        // Act & Assert
        Assert.Equal(0, layout.HitTestColumnBoundary(88, 20));
        Assert.Equal(1, layout.HitTestColumnBoundary(168, 20));
        Assert.Equal(-1, layout.HitTestColumnBoundary(255, 20));
    }
}
