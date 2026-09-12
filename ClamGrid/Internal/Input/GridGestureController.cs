namespace ClamGrid.Internal.Input;

internal sealed class GridGestureController
{
    public const double MovementThreshold = 6;
    public const double LongPressMilliseconds = 500;

    private readonly HashSet<long> pointers = [];
    private readonly Queue<(Point Point, double Time)> samples = [];
    private long primary;
    private Point origin;
    private Point previous;
    private double pressedAt;
    private GridPanAxis axis;
    private bool resize;
    private bool rowHandle;

    public GridGestureState State { get; private set; }

    public GridGestureResult Press(long id, Point point, double milliseconds, bool resizeBoundary = false, GridPanAxis panAxis = GridPanAxis.Both, bool rowDragHandle = false)
    {
        if (!pointers.Add(id) || (pointers.Count > 1))
        {
            Block();
            return new GridGestureResult(GridGestureAction.Canceled);
        }

        primary = id;
        origin = point;
        previous = point;
        pressedAt = milliseconds;
        resize = resizeBoundary;
        rowHandle = rowDragHandle;
        axis = panAxis;
        samples.Clear();
        samples.Enqueue((point, milliseconds));
        State = GridGestureState.Pressed;
        return default;
    }

    public GridGestureResult Move(long id, Point point, double milliseconds)
    {
        if ((id != primary) || !pointers.Contains(id) || (State is GridGestureState.Blocked or GridGestureState.LongPressed))
        {
            return default;
        }

        var total = new Point(point.X - origin.X, point.Y - origin.Y);
        if (State == GridGestureState.Pressed)
        {
            if ((Math.Abs(total.X) < MovementThreshold) && (Math.Abs(total.Y) < MovementThreshold))
            {
                return default;
            }

            State = resize ? GridGestureState.Resizing : rowHandle ? GridGestureState.RowDragging : GridGestureState.Panning;
            if (!resize && !rowHandle && (axis == GridPanAxis.Both))
            {
                if (Math.Abs(total.X) > Math.Abs(total.Y) * 1.5)
                {
                    axis = GridPanAxis.Horizontal;
                }
                else if (Math.Abs(total.Y) > Math.Abs(total.X) * 1.5)
                {
                    axis = GridPanAxis.Vertical;
                }
            }
        }

        var delta = Restrict(new Point(previous.X - point.X, previous.Y - point.Y));
        previous = point;
        AddSample(point, milliseconds);
        return State switch
        {
            GridGestureState.Resizing => new GridGestureResult(GridGestureAction.Resize, total),
            GridGestureState.RowDragging => new GridGestureResult(GridGestureAction.RowDrag, total),
            _ => new GridGestureResult(GridGestureAction.Pan, delta)
        };
    }

    public GridGestureResult Tick(double milliseconds)
    {
        if ((State != GridGestureState.Pressed) || resize || rowHandle || ((milliseconds - pressedAt) < LongPressMilliseconds))
        {
            return default;
        }

        State = GridGestureState.LongPressed;
        return new GridGestureResult(GridGestureAction.LongPress);
    }

    public GridGestureResult Release(long id, Point point, double milliseconds, bool inside)
    {
        if (!pointers.Contains(id))
        {
            return default;
        }

        var result = default(GridGestureResult);
        if ((id == primary) && (State != GridGestureState.Blocked))
        {
            Move(id, point, milliseconds);
            result = State switch
            {
                GridGestureState.Pressed when inside && !resize && !rowHandle => new GridGestureResult(GridGestureAction.Tap),
                GridGestureState.Panning => new GridGestureResult(GridGestureAction.PanCompleted, Velocity: GetVelocity(milliseconds)),
                GridGestureState.Resizing when inside => new GridGestureResult(GridGestureAction.ResizeCompleted, new Point(point.X - origin.X, 0)),
                GridGestureState.RowDragging when inside => new GridGestureResult(GridGestureAction.RowDragCompleted),
                _ => default
            };
        }

        pointers.Remove(id);
        if (pointers.Count == 0)
        {
            Cancel();
        }

        return result;
    }

    public void Block()
    {
        samples.Clear();
        State = pointers.Count > 0 ? GridGestureState.Blocked : GridGestureState.Idle;
    }

    public void Cancel()
    {
        pointers.Clear();
        samples.Clear();
        State = GridGestureState.Idle;
    }

    private Point Restrict(Point value) => axis switch
    {
        GridPanAxis.Horizontal => new Point(value.X, 0),
        GridPanAxis.Vertical => new Point(0, value.Y),
        _ => value
    };

    private void AddSample(Point point, double milliseconds)
    {
        if ((samples.Count > 0) && (samples.Last().Point == point))
        {
            return;
        }

        samples.Enqueue((point, milliseconds));
        while ((samples.Count > 2) && ((milliseconds - samples.Peek().Time) > 100))
        {
            samples.Dequeue();
        }
    }

    private Point GetVelocity(double milliseconds)
    {
        if (samples.Count < 2)
        {
            return default;
        }

        var (firstPoint, firstTime) = samples.Peek();
        var (lastPoint, lastTime) = samples.Last();
        var elapsed = (lastTime - firstTime) / 1000;
        if ((elapsed <= 0) || ((milliseconds - lastTime) > 100))
        {
            return default;
        }

        return Restrict(new Point(Math.Clamp((firstPoint.X - lastPoint.X) / elapsed, -3500, 3500), Math.Clamp((firstPoint.Y - lastPoint.Y) / elapsed, -3500, 3500)));
    }
}
