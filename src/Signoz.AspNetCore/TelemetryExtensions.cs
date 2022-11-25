using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Npgsql;
using OpenTelemetry;
using OpenTelemetry.Instrumentation.AspNetCore;

namespace A55.SigNoz;

using System.Reflection;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

/// <summary>
/// Extensions Configurations for SigNoz in AspNetCore
/// </summary>
public static class SigNozTelemetry
{
    const string SigNozSettingsSection = "SigNoz";
    const string SqsHost = "sqs.*.amazonaws.com";

    static Action<ResourceBuilder> GetConfigureResource(SigNozSettings config,
        string applicationNameFallback) => r => r
        .AddService(
            serviceName: (
                string.IsNullOrWhiteSpace(config.ServiceName)
                    ? applicationNameFallback
                    : config.ServiceName
            ).ToLowerInvariant() + (config.ServiceNameSuffix ?? string.Empty),
            serviceVersion: Assembly.GetCallingAssembly().GetName().Version?.ToString() ?? "0.0.0",
            serviceInstanceId: Environment.MachineName);

    /// <summary>
    /// enable and configure SigNoz on WebApplicationBuilder
    /// </summary>
    /// <param name="builder"></param>
    public static void UseSigNoz(this WebApplicationBuilder builder)
    {
        var config = builder.Configuration.GetSection(SigNozSettingsSection).Get<SigNozSettings>();

        if (!config.Enabled) return;
        AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);

        var configureResource = GetConfigureResource(config, builder.Environment.ApplicationName);

        if (config.ExportTraces)
            builder.Services
                .AddOpenTelemetryTracing(b => b
                    .ConfigureResource(configureResource)
                    .AddCustomTraces(config)
                    .AddAspNetCoreInstrumentation(options =>
                    {
                        options.RecordException = true;
                        AddCorrelationId(options);
                    }));

        if (config.ExportMetrics)
            builder.Services
                .AddOpenTelemetryMetrics(b => b
                    .ConfigureResource(configureResource)
                    .AddCustomMeter(config)
                    .AddAspNetCoreInstrumentation());

        if (config.ExportLogs)
            builder.Logging.AddSigNoz(configureResource, config);
    }

    static void AddCorrelationId(AspNetCoreInstrumentationOptions options)
    {
        static void AddCorrelation(Activity? activity, IHeaderDictionary context)
        {
            if (!context.TryGetValue("X-Correlation-ID", out var correlationId)) return;
            activity?.SetTag("http.conversation_id", correlationId);
            activity?.SetTag("http.correlation_id", correlationId);
        }

        options.EnrichWithHttpRequest = (activity, httpRequest) =>
            AddCorrelation(activity, httpRequest.Headers);
        options.EnrichWithHttpResponse = (activity, httpResponse) =>
            AddCorrelation(activity, httpResponse.Headers);
    }

    static TracerProviderBuilder AddCustomTraces(this TracerProviderBuilder builder,
        SigNozSettings config)
    {
        builder
            .SetSampler(new AlwaysOnSampler())
            .AddSource("A55.Subdivisions")
            .AddNpgsql()
            .AddHttpClientInstrumentation(o => o.FilterHttpRequestMessage = request =>
                !Regex.IsMatch(request.RequestUri?.Host ?? "", SqsHost));

        if (config.ValidOtlp)
            builder.AddOtlpExporter(o => o.Endpoint = new Uri(config.OtlpEndpoint));

        if (config.UseConsole)
            builder.AddConsoleExporter();

        return builder;
    }

    static MeterProviderBuilder AddCustomMeter(this MeterProviderBuilder builder,
        SigNozSettings config)
    {
        builder
            .AddMeter("A55.Subdivisions")
            .AddRuntimeInstrumentation()
            .AddHttpClientInstrumentation();

        if (config.ValidOtlp)
            builder.AddOtlpExporter(o => o.Endpoint = new Uri(config.OtlpEndpoint));

        if (config.UseConsole)
            builder.AddConsoleExporter();

        return builder;
    }

    static SigNozSettings AddSigNozConfig(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var configSection = configuration.GetSection(SigNozSettingsSection);
        services.Configure<SigNozSettings>(configSection);
        return configSection.Get<SigNozSettings>();
    }

    /// <summary>
    /// Adds SigNoz logging capabilities
    /// </summary>
    /// <param name="loggerBuilder"></param>
    /// <param name="configuration"></param>
    /// <param name="environment"></param>
    public static void AddSigNoz(
        this ILoggingBuilder loggerBuilder,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        var config = loggerBuilder.Services.AddSigNozConfig(configuration);

        if (config is not {Enabled: true, ExportLogs: true})
            return;

        var configureResource = GetConfigureResource(config, environment.ApplicationName);
        loggerBuilder.AddSigNoz(configureResource, config);
    }

    static ILoggingBuilder AddSigNoz(
        this ILoggingBuilder loggerBuilder,
        Action<ResourceBuilder> configureResource,
        SigNozSettings config)
    {
        void ConfigureOptions(OpenTelemetryLoggerOptions opt)
        {
            opt.IncludeScopes = true;
            opt.ParseStateValues = true;
            opt.IncludeFormattedMessage = true;
            opt.ConfigureResource(configureResource);
            if (config.UseConsole) opt.AddConsoleExporter();
            if (config.ValidOtlp)
                opt.AddOtlpExporter(otlp => otlp.Endpoint = new Uri(config.OtlpEndpoint));
        }

        loggerBuilder.Services.Configure<OpenTelemetryLoggerOptions>(ConfigureOptions);
        loggerBuilder.AddOpenTelemetry(ConfigureOptions);
        return loggerBuilder;
    }
}
