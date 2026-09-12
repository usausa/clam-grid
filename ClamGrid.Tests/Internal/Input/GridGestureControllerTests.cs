namespace ClamGrid.Tests.Internal.Input;

public sealed class GridGestureControllerTests
{
    [Fact]
    public void SmallMovementRemainsATapButLeavingTheViewDoesNot()
    {
        var input = new GridGestureController();
        input.Press(1, new Point(50, 50), 0);
        Assert.Equal(GridGestureAction.None, input.Move(1, new Point(54, 55), 50).Action);
        Assert.Equal(GridGestureAction.Tap, input.Release(1, new Point(54, 55), 100, true).Action);
        input.Press(1, new Point(2, 50), 200);
        Assert.Equal(GridGestureAction.None, input.Release(1, new Point(-1, 50), 210, false).Action);
    }

    [Fact]
    public void LongPressFiresOnceAndNeverBecomesATapOrPan()
    {
        var input = new GridGestureController();
        input.Press(1, new Point(50, 50), 0);
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
        var input = new GridGestureController();
        input.Press(1, new Point(50, 100), 0);
        var move = input.Move(1, new Point(52, 80), 20);
        Assert.Equal(GridGestureAction.Pan, move.Action);
        Assert.Equal(new Point(0, 20), move.Delta);
        Assert.Equal(new Point(0, 20), input.Move(1, new Point(80, 60), 40).Delta);
        Assert.Equal(GridGestureAction.None, input.Tick(600).Action);
        var release = input.Release(1, new Point(80, 60), 60, true);
        Assert.Equal(GridGestureAction.PanCompleted, release.Action);
        Assert.Equal(0, release.Velocity.X);
        Assert.True(release.Velocity.Y > 0);
    }

    [Fact]
    public void DiagonalPanKeepsBothAxesAndHeadersCanConstrainToHorizontal()
    {
        var input = new GridGestureController();
        input.Press(1, new Point(50, 50), 0);
        Assert.Equal(new Point(10, 10), input.Move(1, new Point(40, 40), 20).Delta);
        input.Cancel();
        input.Press(1, new Point(50, 50), 0, panAxis: GridPanAxis.Horizontal);
        Assert.Equal(new Point(10, 0), input.Move(1, new Point(40, 10), 20).Delta);
    }

    [Fact]
    public void PauseBeforeReleaseDoesNotStartInertia()
    {
        var input = new GridGestureController();
        input.Press(1, new Point(50, 100), 0);
        input.Move(1, new Point(50, 50), 50);
        Assert.Equal(default, input.Release(1, new Point(50, 50), 500, true).Velocity);
    }

    [Fact]
    public void BoundaryReservesTheWholeGestureAndResizeUsesTotalDisplacement()
    {
        var input = new GridGestureController();
        input.Press(1, new Point(100, 20), 0, true);
        Assert.Equal(GridGestureAction.None, input.Tick(600).Action);
        Assert.Equal(GridGestureAction.None, input.Release(1, new Point(100, 20), 610, true).Action);
        input.Press(1, new Point(100, 20), 700, true);
        Assert.Equal(new GridGestureResult(GridGestureAction.Resize, new Point(40, 2)), input.Move(1, new Point(140, 22), 750));
        Assert.Equal(new GridGestureResult(GridGestureAction.ResizeCompleted, new Point(50, 0)), input.Release(1, new Point(150, 22), 800, true));
    }

    [Fact]
    public void ResizeOutsideAndOsCancelDiscardTheOperation()
    {
        var input = new GridGestureController();
        input.Press(1, new Point(100, 20), 0, true);
        input.Move(1, new Point(140, 20), 20);
        Assert.Equal(GridGestureAction.None, input.Release(1, new Point(1000, 20), 30, false).Action);
        input.Press(1, new Point(100, 20), 100);
        input.Cancel();
        Assert.Equal(GridGestureAction.None, input.Release(1, new Point(100, 20), 120, true).Action);
        Assert.Equal(GridGestureAction.None, input.Tick(1000).Action);
    }

    [Fact]
    public void SecondPointerBlocksAllRemainingPointersUntilTheyLift()
    {
        var input = new GridGestureController();
        input.Press(1, new Point(50, 50), 0);
        Assert.Equal(GridGestureAction.Canceled, input.Press(2, new Point(60, 60), 10).Action);
        Assert.Equal(GridGestureState.Blocked, input.State);
        Assert.Equal(GridGestureAction.None, input.Release(1, new Point(50, 50), 20, true).Action);
        Assert.Equal(GridGestureAction.None, input.Tick(600).Action);
        Assert.Equal(GridGestureAction.None, input.Move(2, new Point(100, 100), 610).Action);
        Assert.Equal(GridGestureAction.None, input.Release(2, new Point(100, 100), 620, true).Action);
        input.Press(3, new Point(50, 50), 700);
        Assert.Equal(GridGestureAction.Tap, input.Release(3, new Point(50, 50), 750, true).Action);
    }

    [Fact]
    public void DataInvalidationBlocksUntilReleaseAndIgnoresUnrelatedPointer()
    {
        var input = new GridGestureController();
        input.Press(1, new Point(50, 50), 0);
        Assert.Equal(GridGestureAction.None, input.Release(99, new Point(50, 50), 10, true).Action);
        input.Block();
        Assert.Equal(GridGestureAction.None, input.Release(1, new Point(50, 50), 20, true).Action);
        Assert.Equal(GridGestureState.Idle, input.State);
    }

    [Fact]
    public void HandleReservesPressAndLongHoldAndOnlyMovementStartsDrag()
    {
        var gesture = new GridGestureController();
        gesture.Press(1, new Point(20, 60), 0, rowDragHandle: true);
        Assert.Equal(GridGestureAction.None, gesture.Tick(600).Action);
        Assert.Equal(GridGestureAction.None, gesture.Release(1, new Point(20, 60), 650, true).Action);
        gesture.Press(1, new Point(20, 60), 700, rowDragHandle: true);
        Assert.Equal(GridGestureAction.RowDrag, gesture.Move(1, new Point(20, 100), 750).Action);
        Assert.Equal(GridGestureState.RowDragging, gesture.State);
        Assert.Equal(GridGestureAction.RowDragCompleted, gesture.Release(1, new Point(20, 140), 800, true).Action);
        Assert.Equal(GridGestureState.Idle, gesture.State);
    }

    [Fact]
    public void OutsideReleaseMultiplePointersAndInvalidationNeverCommitDrag()
    {
        var gesture = new GridGestureController();
        gesture.Press(1, new Point(20, 60), 0, rowDragHandle: true);
        gesture.Move(1, new Point(20, 100), 50);
        Assert.Equal(GridGestureAction.None, gesture.Release(1, new Point(-20, 100), 100, false).Action);
        gesture.Press(1, new Point(20, 60), 200, rowDragHandle: true);
        gesture.Move(1, new Point(20, 100), 250);
        gesture.Press(2, new Point(30, 100), 260);
        Assert.Equal(GridGestureAction.None, gesture.Release(1, new Point(20, 100), 300, true).Action);
        gesture.Cancel();
        gesture.Press(1, new Point(20, 60), 400, rowDragHandle: true);
        gesture.Move(1, new Point(20, 100), 450);
        gesture.Block();
        Assert.Equal(GridGestureAction.None, gesture.Release(1, new Point(20, 100), 500, true).Action);
    }
}
