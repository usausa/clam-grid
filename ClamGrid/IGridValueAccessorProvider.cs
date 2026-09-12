namespace ClamGrid;

public interface IGridValueAccessorProvider
{
    IGridValueAccessor? GetValueAccessor(string key);
}
