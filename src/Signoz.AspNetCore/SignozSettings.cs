namespace A55.Signoz;

/// <summary>
/// Signoz configuration binded from appsettings or configurarion
/// </summary>
public sealed class SignozSettings
{
    /// <summary>
    /// Enable or disables telemetry
    /// </summary>
    public bool Enabled { get; set; }
    /// <summary>
    /// The service name which will show in signoz
    /// If null or not define will use Environment.ApplicationName
    /// </summary>
    public string? ServiceName { get; set; }
    /// <summary>
    /// The OTLP signoz endpoint
    /// </summary>
    public string? OtlpEndpoint { get; set; }
    /// <summary>
    /// Flags if opentelemetry will print the spans on console
    /// </summary>
    public bool UseConsole { get; set; }
    /// <summary>
    /// Flags if opentelemetry send to the OTPL endpoint
    /// </summary>
    public bool UseOtlp { get; set; }
    /// <summary>
    /// Flags if opentelemetry will send logs
    /// </summary>
    public bool ExportLogs { get; set; }
    /// <summary>
    /// Flags if opentelemetry will send traces
    /// </summary>
    public bool ExportTraces { get; set; }
    /// <summary>
    /// Flags if opentelemetry will send metrics
    /// </summary>
    public bool ExportMetrics { get; set; }
}
