namespace ClamGrid;

using System.Diagnostics.CodeAnalysis;

public partial class ClamGridView
{
    public static readonly BindableProperty SelectedKeysProperty = BindableProperty.Create(nameof(SelectedKeys), typeof(IReadOnlyList<object>), typeof(ClamGridView), defaultBindingMode: BindingMode.TwoWay, propertyChanged: OnSelectedKeysChanged);

    private object[]? requestedSelectedKeys;
    private bool applyingSelectedKeys;

    [AllowNull]
    public IReadOnlyList<object> SelectedKeys
    {
        get => (IReadOnlyList<object>?)GetValue(SelectedKeysProperty) ?? DataView?.SelectedKeys ?? Array.Empty<object>();
        set => SetValue(SelectedKeysProperty, value);
    }

    private static void OnSelectedKeysChanged(BindableObject bindable, object? oldValue, object? newValue)
    {
        var grid = (ClamGridView)bindable;
        if (grid.applyingSelectedKeys)
        {
            return;
        }

        grid.requestedSelectedKeys = (newValue as IEnumerable<object>)?.ToArray();
        grid.ApplyRequestedSelectedKeys();
    }

    private void ApplyRequestedSelectedKeys()
    {
        if ((requestedSelectedKeys is not { } keys) || (DataView is not { } view))
        {
            return;
        }

        if (!keys.SequenceEqual(view.SelectedKeys, view.RowKeyComparer))
        {
            view.SetSelectedKeys(keys);
        }

        PublishSelectedKeys();
    }

    private void PublishSelectedKeys()
    {
        if (DataView is not { } view)
        {
            return;
        }

        requestedSelectedKeys = view.SelectedKeys.ToArray();
        if ((GetValue(SelectedKeysProperty) is IReadOnlyList<object> current) && current.SequenceEqual(view.SelectedKeys, view.RowKeyComparer))
        {
            return;
        }

        applyingSelectedKeys = true;
        try
        {
            SetValueFromRenderer(SelectedKeysProperty, view.SelectedKeys);
        }
        finally
        {
            applyingSelectedKeys = false;
        }
    }
}
