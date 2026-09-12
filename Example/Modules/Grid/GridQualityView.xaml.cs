namespace Example.Modules.Grid;

[View(ViewId.GridQuality)]
public sealed partial class GridQualityView
{
    public GridQualityView()
    {
        InitializeComponent();

        Loaded += OnLoaded;
    }

    // ReSharper disable once AsyncVoidMethod
    private async void OnLoaded(object? sender, EventArgs e)
    {
        Loaded -= OnLoaded;

        if (BindingContext is GridQualityViewModel viewModel)
        {
            await viewModel.RunAsync(new QualityVerifier(Host));
        }
    }
}
