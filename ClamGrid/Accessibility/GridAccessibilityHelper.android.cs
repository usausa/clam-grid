namespace ClamGrid.Accessibility;

using Android.OS;
using Android.Views;

using AndroidX.Core.View.Accessibility;
using AndroidX.CustomView.Widget;

using ClamGrid.Layout;
using ClamGrid.Rendering;

using AndroidRect = Android.Graphics.Rect;

internal sealed class GridAccessibilityHelper : ExploreByTouchHelper
{
    private const int MoveUpAction = 0x7e000001;
    private const int MoveDownAction = 0x7e000002;

    private readonly Dictionary<string, int> headerIds = [with(StringComparer.Ordinal)];
    private readonly View host;
    private readonly ClamGridView grid;
    private readonly GridCellIdentityMap identities;
    private int nextHeaderId = -1;

    public GridAccessibilityHelper(View host, ClamGridView grid)
        : base(host)
    {
        this.host = host;
        this.grid = grid;
        identities = new GridCellIdentityMap(grid.DataView?.RowKeyComparer ?? ReferenceEqualityComparer.Instance);
    }

    public void Prune()
    {
        var columns = grid.Columns.Select(static column => column.Key).ToHashSet(StringComparer.Ordinal);
        identities.Prune(key => (grid.DataView?.IndexOfKey(key) ?? -1) >= 0, columns);
        foreach (var key in headerIds.Keys.Where(key => !columns.Contains(key)).ToArray())
        {
            headerIds.Remove(key);
        }
    }

    public override bool PerformAccessibilityAction(View? hostView, int action, Bundle? args)
    {
        if (grid.GetCurrentLayout() is { } layout)
        {
            var x = grid.ScrollX;
            var y = grid.ScrollY;
            if ((action == AccessibilityNodeInfoCompat.ActionScrollForward) || (action == AccessibilityNodeInfoCompat.ActionScrollBackward))
            {
                grid.ScrollBy(0, (action == AccessibilityNodeInfoCompat.ActionScrollForward ? 1 : -1) * layout.BodyBounds.Height);
                return !y.Equals(grid.ScrollY);
            }

            if ((action == AccessibilityNodeInfoCompat.AccessibilityActionCompat.ActionScrollLeft?.Id) || (action == AccessibilityNodeInfoCompat.AccessibilityActionCompat.ActionScrollRight?.Id))
            {
                grid.ScrollBy((action == AccessibilityNodeInfoCompat.AccessibilityActionCompat.ActionScrollLeft?.Id ? -1 : 1) * layout.BodyBounds.Width, 0);
                return !x.Equals(grid.ScrollX);
            }
        }

        return base.PerformAccessibilityAction(hostView, action, args);
    }

    protected override int GetVirtualViewAt(float x, float y)
    {
        var scale = host.Width / grid.Width;
        var hit = grid.HitTest(x / scale, y / scale);
        return hit.CellType is GridCellType.Cell or GridCellType.ColumnHeader ? Encode(hit.RowIndex, hit.ColumnIndex) : InvalidId;
    }

    protected override void GetVisibleVirtualViews(IList<Java.Lang.Integer>? virtualViewIds)
    {
        if ((grid.GetCurrentLayout() is not { } layout) || (virtualViewIds is null))
        {
            return;
        }

        var rows = layout.VisibleRows;
        var columns = layout.VisibleColumns;
        if (layout.HeaderHeight > 0)
        {
            for (var column = columns.Start; column < columns.End; column++)
            {
                var clip = new GridRect(layout.RowHeaderWidth, 0, layout.BodyBounds.Width, layout.HeaderHeight);
                if (!layout.GetColumnHeaderBounds(column).Intersect(clip).IsEmpty)
                {
                    virtualViewIds.Add(Java.Lang.Integer.ValueOf(Encode(-1, column)));
                }
            }
        }

        for (var row = rows.Start; row < rows.End; row++)
        {
            for (var column = columns.Start; column < columns.End; column++)
            {
                if (!layout.GetCellBounds(row, column).Intersect(layout.BodyBounds).IsEmpty)
                {
                    virtualViewIds.Add(Java.Lang.Integer.ValueOf(Encode(row, column)));
                }
            }
        }
    }

    protected override void OnPopulateNodeForHost(AccessibilityNodeInfoCompat? node)
    {
        base.OnPopulateNodeForHost(node);
        if (node is not null)
        {
            node.ContentDescription = $"{grid.RowCount}行、{grid.Columns.Count}列の表";
            node.Scrollable = true;
            node.AddAction(AccessibilityNodeInfoCompat.ActionScrollForward);
            node.AddAction(AccessibilityNodeInfoCompat.ActionScrollBackward);
            node.AddAction(AccessibilityNodeInfoCompat.AccessibilityActionCompat.ActionScrollLeft);
            node.AddAction(AccessibilityNodeInfoCompat.AccessibilityActionCompat.ActionScrollRight);
        }
    }

    protected override void OnPopulateNodeForVirtualView(int virtualViewId, AccessibilityNodeInfoCompat? node)
    {
        if (node is null)
        {
            return;
        }

        var hit = Decode(virtualViewId);
        if ((grid.GetCurrentLayout() is not { } layout) || (hit.CellType == GridCellType.None) || (hit.RowIndex >= grid.RowCount) || (hit.ColumnIndex < 0) || (hit.ColumnIndex >= grid.Columns.Count))
        {
            node.ContentDescription = "利用できないセル";
            using var empty = new AndroidRect(0, 0, 1, 1);
            SetBoundsInScreenFromBoundsInParent(node, empty);
            return;
        }

        var column = grid.Columns[hit.ColumnIndex];
        if (hit.CellType == GridCellType.ColumnHeader)
        {
            var orders = grid.DataView?.SortOrders;
            var primary = orders is { Count: > 0 } ? orders[0] : null;
            var key = column.SortKey ?? column.Key;
            node.ContentDescription = $"列見出し、{column.Header}" + (primary?.Key == key ? primary.Descending ? "、降順" : "、昇順" : String.Empty);
            node.ClassName = "android.widget.TextView";
            node.Heading = true;
            node.Focusable = true;
            node.Clickable = column.AllowSorting && (grid.DataView?.CanSort(key) ?? false);
            node.LongClickable = grid.AllowColumnConfiguration;
            AddClickActions(node);
            SetNodeBounds(node, layout.GetColumnHeaderBounds(hit.ColumnIndex).Intersect(new GridRect(layout.RowHeaderWidth, 0, layout.BodyBounds.Width, layout.HeaderHeight)));
            return;
        }

        var value = column.ValueAccessor.GetValue(grid.GetItem(hit.RowIndex));
        node.ContentDescription = $"{hit.RowIndex + 1}行、{column.Header}、{GridRenderer.FormatValue(value, column.Format)}";
        node.ClassName = column.IsBoolean ? "android.widget.CheckBox" : "android.widget.TextView";
        node.Checkable = column.IsBoolean;
        node.Checked = value is true;
        node.Selected = grid.IsSelected(hit.RowIndex);
        node.Clickable = !column.IsBoolean || grid.CanEditBoolean(column);
        node.LongClickable = !column.IsBoolean;
        node.Focusable = true;
        AddClickActions(node);
        if (grid.AllowRowDragging && (grid.RowMover?.CanMove ?? false))
        {
            if (hit.RowIndex > 0)
            {
                using var action = new AccessibilityNodeInfoCompat.AccessibilityActionCompat(MoveUpAction, "上へ移動");
                node.AddAction(action);
            }

            if (hit.RowIndex < grid.RowCount - 1)
            {
                using var action = new AccessibilityNodeInfoCompat.AccessibilityActionCompat(MoveDownAction, "下へ移動");
                node.AddAction(action);
            }
        }

        SetNodeBounds(node, layout.GetCellBounds(hit.RowIndex, hit.ColumnIndex).Intersect(layout.BodyBounds));
    }

    protected override bool OnPerformActionForVirtualView(int virtualViewId, int action, Bundle? arguments)
    {
        if ((action != AccessibilityNodeInfoCompat.ActionClick) && (action != AccessibilityNodeInfoCompat.ActionLongClick) && (action != MoveUpAction) && (action != MoveDownAction))
        {
            return false;
        }

        var hit = Decode(virtualViewId);
        if ((grid.GetCurrentLayout() is not { } layout) || (hit.CellType == GridCellType.None) || (hit.RowIndex >= grid.RowCount) || (hit.ColumnIndex < 0) || (hit.ColumnIndex >= grid.Columns.Count))
        {
            return false;
        }

        if (action is MoveUpAction or MoveDownAction)
        {
            return hit.CellType == GridCellType.Cell && grid.MoveRowForAccessibility(hit.RowIndex, hit.RowIndex + (action == MoveUpAction ? -1 : 1));
        }

        var bounds = hit.CellType == GridCellType.ColumnHeader ? layout.GetColumnHeaderBounds(hit.ColumnIndex) : layout.GetCellBounds(hit.RowIndex, hit.ColumnIndex);
        grid.CancelInteraction();
        return grid.ActivateCell(hit, new Point(bounds.X + (bounds.Width / 2), bounds.Y + (bounds.Height / 2)), action == AccessibilityNodeInfoCompat.ActionLongClick);
    }

    private static void AddClickActions(AccessibilityNodeInfoCompat node)
    {
        if (node.Clickable)
        {
            node.AddAction(AccessibilityNodeInfoCompat.ActionClick);
        }

        if (node.LongClickable)
        {
            node.AddAction(AccessibilityNodeInfoCompat.ActionLongClick);
        }
    }

    private void SetNodeBounds(AccessibilityNodeInfoCompat node, GridRect rect)
    {
        var scale = host.Width / grid.Width;
        using var bounds = new AndroidRect((int)(rect.X * scale), (int)(rect.Y * scale), (int)Math.Ceiling(rect.Right * scale), (int)Math.Ceiling(rect.Bottom * scale));
        SetBoundsInScreenFromBoundsInParent(node, bounds);
    }

    private int Encode(int row, int column)
    {
        var key = grid.Columns[column].Key;
        if (row < 0)
        {
            if (!headerIds.TryGetValue(key, out var id))
            {
                id = checked(--nextHeaderId);
                headerIds.Add(key, id);
            }

            return id;
        }

        return grid.DataView is { } view ? identities.GetId(view.GetRowKey(row), key) : InvalidId;
    }

    private GridHit Decode(int id)
    {
        if (id < -1)
        {
            var key = headerIds.FirstOrDefault(pair => pair.Value == id).Key;
            for (var column = 0; column < grid.Columns.Count; column++)
            {
                if (grid.Columns[column].Key == key)
                {
                    return new GridHit(GridCellType.ColumnHeader, -1, column);
                }
            }

            return GridHit.None;
        }

        if ((identities.Find(id) is not { } cell) || (grid.DataView is not { } view))
        {
            return GridHit.None;
        }

        var row = view.IndexOfKey(cell.RowKey);
        if (row < 0)
        {
            return GridHit.None;
        }

        for (var column = 0; column < grid.Columns.Count; column++)
        {
            if (grid.Columns[column].Key == cell.ColumnKey)
            {
                return new GridHit(GridCellType.Cell, row, column);
            }
        }

        return GridHit.None;
    }
}
