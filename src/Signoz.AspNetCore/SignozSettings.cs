namespace A55.Signoz;

public class SignozSettings
{
    public bool Enabled { get; set; }
    public string? ServiceName { get; set; }
    public string? OtlpEndpoint { get; set; }
    public bool UseConsole { get; set; }
    public bool UseOtlp { get; set; }
    public bool ExportLogs { get; set; }
    public bool ExportTraces { get; set; }
    public bool ExportMetrics { get; set; }
}
