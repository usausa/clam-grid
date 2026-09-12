namespace ClamGrid;

using ClamGrid.Internal.Platform;

public partial class ClamGridView
{
    partial void AttachPlatform()
    {
        if (!disposed && (Handler?.PlatformView is Android.Views.View view))
        {
            platform = new GridPlatformBridge(view, this);
        }
    }
}
