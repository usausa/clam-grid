namespace ClamGrid.Tests;

public sealed class GridRowDraggingTests
{
    [Fact]
    public void HandleReservesPressAndLongHoldAndOnlyMovementStartsDrag()
    {
        // Arrange
        var gesture = new GridGestureController();
        gesture.Press(1, new Point(20, 60), 0, rowDragHandle: true);

        // Act & Assert
        Assert.Equal(GridGestureAction.None, gesture.Tick(600).Action);
        Assert.Equal(GridGestureAction.None, gesture.Release(1, new Point(20, 60), 650, true).Action);

        // Arrange
        gesture.Press(1, new Point(20, 60), 700, rowDragHandle: true);

        // Act & Assert
        Assert.Equal(GridGestureAction.RowDrag, gesture.Move(1, new Point(20, 100), 750).Action);
        Assert.Equal(GridGestureState.RowDragging, gesture.State);
        Assert.Equal(GridGestureAction.RowDragCompleted, gesture.Release(1, new Point(20, 140), 800, true).Action);
        Assert.Equal(GridGestureState.Idle, gesture.State);
    }

    [Fact]
    public void OutsideReleaseMultiplePointersAndInvalidationNeverCommitDrag()
    {
        // Arrange
        var gesture = new GridGestureController();
        gesture.Press(1, new Point(20, 60), 0, rowDragHandle: true);
        gesture.Move(1, new Point(20, 100), 50);

        // Act & Assert
        Assert.Equal(GridGestureAction.None, gesture.Release(1, new Point(-20, 100), 100, false).Action);

        // Arrange
        gesture.Press(1, new Point(20, 60), 200, rowDragHandle: true);
        gesture.Move(1, new Point(20, 100), 250);
        gesture.Press(2, new Point(30, 100), 260);

        // Act & Assert
        Assert.Equal(GridGestureAction.None, gesture.Release(1, new Point(20, 100), 300, true).Action);

        // Arrange
        gesture.Cancel();
        gesture.Press(1, new Point(20, 60), 400, rowDragHandle: true);
        gesture.Move(1, new Point(20, 100), 450);
        gesture.Block();

        // Act & Assert
        Assert.Equal(GridGestureAction.None, gesture.Release(1, new Point(20, 100), 500, true).Action);
    }

    [Fact]
    public void InsertionUsesMidpointsAndConvertsToPostRemovalIndex()
    {
        // Arrange
        var layout = new GridLayout([100], 10, 40, 40, 40, 240, 240);

        // Act & Assert
        Assert.Equal(0, GridRowDragLayout.GetInsertionIndex(layout, 59));
        Assert.Equal(1, GridRowDragLayout.GetInsertionIndex(layout, 60));
        Assert.Equal(2, GridRowDragLayout.GetTargetIndex(0, 3, 10));
        Assert.Equal(1, GridRowDragLayout.GetTargetIndex(5, 1, 10));
        Assert.Equal(5, GridRowDragLayout.GetTargetIndex(5, 6, 10));
        Assert.Equal(-1, GridRowDragLayout.GetTargetIndex(10, 3, 10));

        // Arrange
        layout.ScrollTo(0, 160);

        // Act & Assert
        Assert.Equal(4, GridRowDragLayout.GetInsertionIndex(layout, 40));
        Assert.Equal(10, GridRowDragLayout.GetInsertionIndex(layout, 1000));
    }

    [Fact]
    public void EdgeScrollOnlyRunsInsideTheGridAndApproachesTheEdge()
    {
        // Arrange
        var layout = new GridLayout([100], 100, 40, 40, 40, 240, 240);

        // Act & Assert
        Assert.Equal(0, GridRowDragLayout.GetScrollVelocity(layout, new Point(20, 140)));
        Assert.Equal(0, GridRowDragLayout.GetScrollVelocity(layout, new Point(-1, 239)));
        Assert.Equal(0, GridRowDragLayout.GetScrollVelocity(layout, new Point(20, 240)));
        Assert.True(GridRowDragLayout.GetScrollVelocity(layout, new Point(20, 41)) < -400);
        Assert.True(GridRowDragLayout.GetScrollVelocity(layout, new Point(20, 239)) > 400);
    }
}
