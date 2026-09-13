namespace Example.Modules.Grid;

using Android.Views;
using Android.Views.Accessibility;

using AndroidX.Core.View.Accessibility;

public sealed partial class QualityVerifier
{
    private async Task VerifyAccessibilityAsync()
    {
        report("アクセシビリティ操作の自動検証");
        host.Content = null;
        grid.Dispose();
        grid.Handler?.DisconnectHandler();
        grid.Handler = null;
        data?.Dispose();
        data = null;
        var rows = new ObservableCollection<SampleRow>(SampleData.CreateRows(10));
        using var view = new GridDataView<SampleRow>(rows, static row => row.Id);
        view.RegisterSort("id", static row => row.Id);
        var boolean = new GridColumn("visible", "発注", new GridValueAccessor<SampleRow, bool>(static row => row.IsChecked, static (row, value) => row.IsChecked = value)) { IsBoolean = true, IsReadOnly = false, AllowSorting = false, Width = GridColumnWidth.Absolute(100) };
        var number = new GridColumn("id", "番号", new GridValueAccessor<SampleRow, int>(static row => row.Id + 1)) { Width = GridColumnWidth.Absolute(130) };
        grid = new ClamGridView { AutomationId = "QualityGrid", GridStyle = new GridStyle { RowHeight = 40, HeaderHeight = 40 } };
        await NextFrameAsync(() =>
        {
            grid.Columns.ReplaceAll([boolean, number]);
            grid.ItemsSource = view;
            host.Content = grid;
        }).ConfigureAwait(true);
        var native = (View)grid.Handler!.PlatformView!;
        var provider = native.AccessibilityNodeProvider ?? throw new InvalidOperationException("Accessibility provider missing");
        var headerId = FindVirtualId(provider, "Column header, 番号");
        var booleanId = FindVirtualId(provider, "Row 1, 発注, ");
        var numberId = FindVirtualId(provider, "Row 1, 番号, 1");
        using (var node = provider.CreateAccessibilityNodeInfo(booleanId)!)
        {
            using var compatible = AccessibilityNodeInfoCompat.Wrap(node)!;
            Check("accessibility: checkbox semantics", node.Checkable && !compatible.Checked && node.Clickable);
        }

        Check("accessibility: checkbox action", provider.PerformAction(booleanId, Android.Views.Accessibility.Action.Click, null) && rows[0].IsChecked);
        Check("accessibility: row selection action", provider.PerformAction(numberId, Android.Views.Accessibility.Action.Click, null) && grid.SelectedCount == 1);
        using (var node = provider.CreateAccessibilityNodeInfo(numberId)!)
        {
            Check("accessibility: selected state", node.Selected);
        }

        Check("accessibility: header sort action", provider.PerformAction(headerId, Android.Views.Accessibility.Action.Click, null) && view.SortOrders[0].Key == "id");
        Check("accessibility: reverse sort action", provider.PerformAction(headerId, Android.Views.Accessibility.Action.Click, null) && view.SortOrders[0].Descending);
        using (var node = provider.CreateAccessibilityNodeInfo(headerId)!)
        {
            Check("accessibility: direction and heading", node.Heading && node.ContentDescription!.Contains("descending", StringComparison.Ordinal));
        }

        var requests = 0;
        grid.ColumnConfigurationRequested += (_, _) => Interlocked.Increment(ref requests);
        Check("accessibility: header settings action", provider.PerformAction(headerId, Android.Views.Accessibility.Action.LongClick, null) && Volatile.Read(ref requests) == 1);
        await NextFrameAsync(() => view.RestoreSortOrders([])).ConfigureAwait(true);
        grid.AllowRowDragging = true;
        grid.RowMover = new GridRowMover<SampleRow>(rows, view);
        using (var node = provider.CreateAccessibilityNodeInfo(numberId)!)
        {
            var action = node.ActionList!.Single(static item => item.Label?.ToString() == "Move down");
            Check("accessibility: move row down", provider.PerformAction(numberId, (Android.Views.Accessibility.Action)action.Id, null) && rows[1].Id == 0 && ReferenceEquals(rows[1], grid.SelectedItems.Single()));
        }

        using (var node = provider.CreateAccessibilityNodeInfo(numberId)!)
        {
            var action = node.ActionList!.Single(static item => item.Label?.ToString() == "Move up");
            static void CancelMove(object? sender, GridRowMoveEventArgs args) => args.Cancel = true;
            grid.RowMoveRequested += CancelMove;
            Check("accessibility: move cancellation", !provider.PerformAction(numberId, (Android.Views.Accessibility.Action)action.Id, null) && rows[1].Id == 0);
            grid.RowMoveRequested -= CancelMove;
            Check("accessibility: move row up", provider.PerformAction(numberId, (Android.Views.Accessibility.Action)action.Id, null) && rows[0].Id == 0);
        }

        await NextFrameAsync(() => grid.Columns.ReplaceAll([boolean with { IsReadOnly = true }, number])).ConfigureAwait(true);
        using (var node = provider.CreateAccessibilityNodeInfo(booleanId)!)
        {
            using var compatible = AccessibilityNodeInfoCompat.Wrap(node)!;
            Check("accessibility: readonly checkbox", node.Checkable && compatible.Checked && !node.Clickable);
        }

        await NextFrameAsync(() => grid.Columns[1] = number with { Format = "D3" }).ConfigureAwait(true);
        Check("accessibility: cell description uses the column format", FindVirtualId(provider, "Row 1, 番号, 001") == numberId);
        grid.ItemsSource = null;
        grid.RowMover = null;
        grid.AllowRowDragging = false;
    }

    private static int FindVirtualId(AccessibilityNodeProvider provider, string description)
    {
        using var root = provider.CreateAccessibilityNodeInfo(-1);
        // Scans the ids issued by the current helper and resolves the target from the actual node description
        foreach (var id in Enumerable.Range(-12, 11).Concat(Enumerable.Range(1, 100)))
        {
            using var node = provider.CreateAccessibilityNodeInfo(id);
            if (node?.ContentDescription?.Contains(description, StringComparison.Ordinal) ?? false)
            {
                return id;
            }
        }

        throw new InvalidOperationException($"Accessibility node missing: {description}");
    }
}
