namespace Example.Modules.Grid;

public sealed partial class GridQualityViewModel : AppViewModelBase
{
    private QualityVerifier? verifier;

    [ObservableProperty]
    public partial string Status { get; set; } = "準備中";

    [ObservableProperty]
    public partial bool IsRunning { get; set; }

    //--------------------------------------------------------------------------------
    // Verification
    //--------------------------------------------------------------------------------

    [SuppressMessage("Design", "CA1031", Justification = "自動検証の失敗をログと画面へ記録するため、検証の最上位で例外を捕捉する。")]
    public async Task RunAsync(QualityVerifier target)
    {
        verifier?.Dispose();
        verifier = target;
        IsRunning = true;
        try
        {
            await target.RunAsync(x => Status = x).ConfigureAwait(true);
        }
        catch (OperationCanceledException)
        {
            Status = "中止しました。";
            QualityVerifier.Log("CANCELED");
        }
        catch (Exception error)
        {
            Status = $"FAIL ({target.Passed}項目): {error.Message}";
            global::Android.Util.Log.Error(QualityVerifier.Tag, error.ToString());
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

    public override Task OnNavigatingFromAsync(INavigationContext context)
    {
        verifier?.Cancel();
        return Task.CompletedTask;
    }

    protected override Task OnNotifyBackAsync() => IsRunning ? Task.CompletedTask : Navigator.ForwardAsync(ViewId.GridMenu);

    protected override Task OnNotifyFunction1() => OnNotifyBackAsync();
}
