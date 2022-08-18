using System.Diagnostics;
using OpenTelemetry.Trace;

namespace A55.SigNoz;

/// <summary>
/// Telemtry scope
/// </summary>
public sealed class TelemetryScope : IDisposable
{
    readonly IDisposable deps;
    readonly Activity? activity;
    bool success = true;

    internal TelemetryScope(IDisposable deps, Activity? activity)
    {
        this.deps = deps;
        this.activity = activity;
    }

    /// <summary>
    /// Register an exception
    /// </summary>
    /// <param name="ex"></param>
    public void CaptureException(Exception ex)
    {
        activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
        activity?.RecordException(ex);
        success = false;
    }

    /// <summary>
    /// Flush traces
    /// </summary>
    public void Dispose()
    {
        if (success)
            activity?.SetStatus(ActivityStatusCode.Ok);

        activity?.Stop();
        deps.Dispose();
    }
}

class DisposableCollection : IDisposable
{
    readonly List<IDisposable> disposables = new();
    public void Add(params IDisposable[] disposable) => disposables.AddRange(disposable);
    public void Dispose() => disposables.ForEach(d => d.Dispose());
}
