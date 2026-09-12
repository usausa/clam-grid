namespace ClamGrid;

public interface IGridRowMover
{
    bool CanMove { get; }

    bool Move(object rowKey, int targetIndex);
}
