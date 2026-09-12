namespace Example.Modules.Grid;

public sealed partial class QualityVerifier : IDisposable
{
    public const string Tag = "ClamGrid.Quality";

    private readonly ContentView host;

    private readonly CancellationTokenSource cancellation = new();

    private readonly List<string> summaries = [];

    private ClamGridView grid = new() { AutomationId = "QualityGrid" };

    private GridDataView<TicketRow>? data;

    private Action<string> report = static _ => { };

    private bool disposed;

    public int Passed { get; private set; }

    public QualityVerifier(ContentView host)
    {
        this.host = host;
        host.Content = grid;
    }

    public async Task RunAsync(Action<string> status)
    {
        report = status;
        await Task.Delay(600, cancellation.Token).ConfigureAwait(true);
        var deviceModel = global::Android.OS.Build.Model;
        var sdk = (int)global::Android.OS.Build.VERSION.SdkInt;
        Log($"ENV model={deviceModel};sdk={sdk};processor_count={Environment.ProcessorCount};runtime={Environment.Version}");
        await VerifyScenariosAsync().ConfigureAwait(true);
        await VerifyBindingAsync().ConfigureAwait(true);
        await VerifyColorsAsync().ConfigureAwait(true);
        await VerifyAccessibilityAsync().ConfigureAwait(true);
        await VerifyLifecycleAsync().ConfigureAwait(true);
        foreach (var (rows, columns) in new[] { (1000, 18), (10000, 18), (50000, 30) })
        {
            await BenchmarkAsync(rows, columns).ConfigureAwait(true);
        }

        await BenchmarkColorsAsync().ConfigureAwait(true);

        report($"PASS: {Passed}項目\n" + String.Join("\n", summaries));
        Log($"PASS checks={Passed}");
    }

    public void Cancel()
    {
        if (!disposed)
        {
            cancellation.Cancel();
            grid.CancelInteraction();
        }
    }

    public void Dispose()
    {
        if (!disposed)
        {
            disposed = true;
            cancellation.Cancel();
            host.Content = null;
            grid.Dispose();
            data?.Dispose();
            cancellation.Dispose();
        }
    }

    public static void Log(string message) => global::Android.Util.Log.Info(Tag, message);

    private static double P95(IEnumerable<double> values)
    {
        var sorted = values.Order().ToArray();
        return sorted[(int)Math.Ceiling(sorted.Length * 0.95) - 1];
    }

    private async Task<GridFrameEventArgs> NextFrameAsync(Action action)
    {
        var completion = new TaskCompletionSource<GridFrameEventArgs>(TaskCreationOptions.RunContinuationsAsynchronously);
        void OnFrame(object? sender, GridFrameEventArgs args) => completion.TrySetResult(args);
        var current = grid;
        current.FrameRendered += OnFrame;
        try
        {
            action();
            current.InvalidateSurface();
            return await completion.Task.WaitAsync(TimeSpan.FromSeconds(20), cancellation.Token).ConfigureAwait(true);
        }
        finally
        {
            current.FrameRendered -= OnFrame;
        }
    }

    private void Check(string name, bool condition)
    {
        if (!condition)
        {
            throw new InvalidOperationException(name);
        }

        Passed++;
        Log($"CHECK {Passed}: {name}");
    }
}
