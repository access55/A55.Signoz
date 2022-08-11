using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace A55.Signoz;

using System.Reflection;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

public static class TelemetryExtensions
{
    const string SignozSettingsSection = "Signoz";

    public static void UseSignoz(this WebApplicationBuilder builder)
    {
        var config = builder.Configuration.GetSection(SignozSettingsSection).Get<SignozSettings>();

        if (!config.Enabled) return;

        AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
        var assemblyVersion = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "unknown";

        var configureResource = (ResourceBuilder r) =>
        {
            r.AddService(
                string.IsNullOrWhiteSpace(config.ServiceName)
                    ? builder.Environment.ApplicationName.ToLowerInvariant()
                    : config.ServiceName.ToLowerInvariant(),
                serviceVersion: assemblyVersion,
                serviceInstanceId: Environment.MachineName);
        };

        builder.Services
            .AddOpenTelemetryTracing(traceBuilder =>
            {
                traceBuilder
                    .ConfigureResource(configureResource)
                    .SetSampler(new AlwaysOnSampler())
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddSqlClientInstrumentation(options => options.RecordException = true);

                if (config.UseOtlp && config.OtlpEndpoint is not null)
                    traceBuilder.AddOtlpExporter(o => o.Endpoint = new Uri(config.OtlpEndpoint));

                if (config.UseConsole)
                    traceBuilder.AddConsoleExporter();
            });

        builder.Services.AddOpenTelemetryMetrics(metricsBuilder =>
        {
            metricsBuilder
                .AddRuntimeInstrumentation()
                .AddHttpClientInstrumentation()
                .AddAspNetCoreInstrumentation();

            if (config.UseOtlp && config.OtlpEndpoint is not null)
                metricsBuilder.AddOtlpExporter(o => o.Endpoint = new Uri(config.OtlpEndpoint));

            if (config.UseConsole)
                metricsBuilder.AddConsoleExporter();
        });

        if (config.ExportLogs)
        {
            builder.Logging.AddOpenTelemetry(options =>
            {
                options.ConfigureResource(configureResource);
                if (config.UseOtlp && config.OtlpEndpoint is not null)
                    options.AddOtlpExporter(otlpOptions => otlpOptions.Endpoint = new Uri(config.OtlpEndpoint));
                if (config.UseConsole)
                    options.AddConsoleExporter();
            });
            builder.Services.Configure<OpenTelemetryLoggerOptions>(opt =>
            {
                opt.IncludeScopes = true;
                opt.ParseStateValues = true;
                opt.IncludeFormattedMessage = true;
            });
        }
    }
}