namespace ClamGrid.Input;

internal readonly record struct GridGestureResult(GridGestureAction Action, Point Delta = default, Point Velocity = default);
