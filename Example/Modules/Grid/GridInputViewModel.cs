namespace Example.Modules.Grid;

public sealed partial class GridInputViewModel : AppViewModelBase
{
    private InputVerifier? verifier;

    [ObservableProperty]
    public partial string Status { get; set; } = "準備中";

    [ObservableProperty]
    public partial bool IsRunning { get; set; }

    //--------------------------------------------------------------------------------
    // Verification
    //--------------------------------------------------------------------------------

    public async Task RunAsync(InputVerifier target)
    {
        verifier?.Dispose();
        verifier = target;
        IsRunning = true;
        try
        {
            await target.RunAsync(x => Status = x).ConfigureAwait(true);
            Status = $"PASS: {target.Passed}項目 / Androidの入力経路を確認";
            global::Android.Util.Log.Info(InputVerifier.Tag, Status);
        }
        catch (InvalidOperationException error)
        {
            Status = $"FAIL ({target.Passed}項目通過): {error.Message}";
            global::Android.Util.Log.Error(InputVerifier.Tag, error.ToString());
        }
        finally
        {
            IsRunning = false;
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            verifier?.Dispose();
            verifier = null;
        }

        base.Dispose(disposing);
    }

    //--------------------------------------------------------------------------------
    // Navigation
    //--------------------------------------------------------------------------------

    protected override Task OnNotifyBackAsync() => IsRunning ? Task.CompletedTask : Navigator.ForwardAsync(ViewId.GridMenu);

    protected override Task OnNotifyFunction1() => OnNotifyBackAsync();
}
