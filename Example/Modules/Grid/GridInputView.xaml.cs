namespace Example.Modules.Grid;

[View(ViewId.GridInput)]
public sealed partial class GridInputView
{
    public GridInputView()
    {
        InitializeComponent();

        Loaded += OnLoaded;
    }

    // ReSharper disable once AsyncVoidMethod
    private async void OnLoaded(object? sender, EventArgs e)
    {
        Loaded -= OnLoaded;

        if (BindingContext is GridInputViewModel viewModel)
        {
            await viewModel.RunAsync(new InputVerifier(TestGrid, Scroller));
        }
    }
}
