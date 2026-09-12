namespace ClamGrid.Tests.Internal.Layout;

public sealed class GridRowDragLayoutTests
{
    [Fact]
    public void InsertionUsesMidpointsAndConvertsToPostRemovalIndex()
    {
        var layout = new GridLayout([100], 10, 40, 40, 40, 240, 240);
        Assert.Equal(0, GridRowDragLayout.GetInsertionIndex(layout, 59));
        Assert.Equal(1, GridRowDragLayout.GetInsertionIndex(layout, 60));
        Assert.Equal(2, GridRowDragLayout.GetTargetIndex(0, 3, 10));
        Assert.Equal(1, GridRowDragLayout.GetTargetIndex(5, 1, 10));
        Assert.Equal(5, GridRowDragLayout.GetTargetIndex(5, 6, 10));
        Assert.Equal(-1, GridRowDragLayout.GetTargetIndex(10, 3, 10));
        layout.ScrollTo(0, 160);
        Assert.Equal(4, GridRowDragLayout.GetInsertionIndex(layout, 40));
        Assert.Equal(10, GridRowDragLayout.GetInsertionIndex(layout, 1000));
    }

    [Fact]
    public void EdgeScrollOnlyRunsInsideTheGridAndApproachesTheEdge()
    {
        var layout = new GridLayout([100], 100, 40, 40, 40, 240, 240);
        Assert.Equal(0, GridRowDragLayout.GetScrollVelocity(layout, new Point(20, 140)));
        Assert.Equal(0, GridRowDragLayout.GetScrollVelocity(layout, new Point(-1, 239)));
        Assert.Equal(0, GridRowDragLayout.GetScrollVelocity(layout, new Point(20, 240)));
        Assert.True(GridRowDragLayout.GetScrollVelocity(layout, new Point(20, 41)) < -400);
        Assert.True(GridRowDragLayout.GetScrollVelocity(layout, new Point(20, 239)) > 400);
    }
}
