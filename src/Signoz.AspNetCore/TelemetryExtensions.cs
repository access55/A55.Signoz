using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Npgsql;
using OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Instrumentation.AspNetCore;

namespace A55.Signoz;

using System.Reflection;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

/// <summary>
/// Extensions Configurations for Signoz in AspNetCore
/// </summary>
public static class SignozTelemetryExtensions
{
    const string SignozSettingsSection = "Signoz";

    /// <summary>
    /// enable and configure signoz on WebApplicationBuilder
    /// </summary>
    /// <param name="builder"></param>
    public static void UseSignoz(this WebApplicationBuilder builder)
    {
        var config = builder.Configuration.GetSection(SignozSettingsSection).Get<SignozSettings>();

        if (!config.Enabled) return;
        AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);

        var assemblyVersion = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "0.0.0";

        var configureResource = (ResourceBuilder r) =>
        {
            r.AddService(
                string.IsNullOrWhiteSpace(config.ServiceName)
                    ? builder.Environment.ApplicationName.ToLowerInvariant()
                    : config.ServiceName.ToLowerInvariant(),
                serviceVersion: assemblyVersion,
                serviceInstanceId: Environment.MachineName);
        };

        builder.Services.Configure<AspNetCoreInstrumentationOptions>(c => c.RecordException = true);

        if (config.ExportTraces)
            AddTraces(builder.Services, configureResource, config);

        if (config.ExportMetrics)
            AddMetrics(builder.Services, configureResource, config);

        if (config.ExportLogs)
            AddLogs(builder.Logging, builder.Services, configureResource, config);
    }

    static void AddTraces(IServiceCollection services, Action<ResourceBuilder> configureResource,
        SignozSettings config) =>
        services
            .AddOpenTelemetryTracing(traceBuilder =>
            {
                traceBuilder
                    .ConfigureResource(configureResource)
                    .SetSampler(new AlwaysOnSampler())
                    .AddNpgsql()
                    .AddHttpClientInstrumentation()
                    .AddAspNetCoreInstrumentation(c => c.RecordException = true);

                if (config.ValidOtlp)
                    traceBuilder.AddOtlpExporter(o => o.Endpoint = new Uri(config.OtlpEndpoint));

                if (config.UseConsole)
                    traceBuilder.AddConsoleExporter();
            });

    static void AddMetrics(IServiceCollection services, Action<ResourceBuilder> configureResource,
        SignozSettings config) =>
        services
            .AddOpenTelemetryMetrics(metricsBuilder =>
            {
                metricsBuilder
                    .ConfigureResource(configureResource)
                    .AddRuntimeInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddAspNetCoreInstrumentation();

                if (config.ValidOtlp)
                    metricsBuilder.AddOtlpExporter(o => o.Endpoint = new Uri(config.OtlpEndpoint));

                if (config.UseConsole)
                    metricsBuilder.AddConsoleExporter();
            });

    static void AddLogs(
        ILoggingBuilder loggerBuilder,
        IServiceCollection services,
        Action<ResourceBuilder> configureResource,
        SignozSettings config)
    {
        void ConfigureOptions(OpenTelemetryLoggerOptions opt)
        {
            opt.IncludeScopes = true;
            opt.ParseStateValues = true;
            opt.IncludeFormattedMessage = true;
            opt.ConfigureResource(configureResource);
            if (config.UseConsole) opt.AddConsoleExporter();
            if (config.ValidOtlp)
                opt.AddOtlpExporter(otlpOptions => otlpOptions.Endpoint = new Uri(config.OtlpEndpoint));
        }

        services.Configure<OpenTelemetryLoggerOptions>(ConfigureOptions);
        loggerBuilder.AddOpenTelemetry(ConfigureOptions);
    }
}
