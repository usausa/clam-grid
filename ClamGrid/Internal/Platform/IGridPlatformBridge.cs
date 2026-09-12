namespace ClamGrid.Internal.Platform;

internal interface IGridPlatformBridge : IDisposable
{
    void InvalidateAccessibility();

    void PruneAccessibility();

    void SetParentIntercept(bool allow);
}
