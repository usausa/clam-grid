namespace ClamGrid.Platform;

internal interface IGridPlatformBridge : IDisposable
{
    void SetParentIntercept(bool allow);
}
