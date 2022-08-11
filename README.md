# A55 Signoz Extensions for .NET
[![CI](https://github.com/access55/A55.Signoz/actions/workflows/publish.yml/badge.svg)](https://github.com/access55/A55.Signoz/actions/workflows/publish.yml)
![](https://img.shields.io/badge/Lang-C%23-green)
![https://editorconfig.org/](https://img.shields.io/badge/style-EditorConfig-black)

This project groups and configure [opentelemetry instrumentation for .NET](https://opentelemetry.io/docs/instrumentation/net) focusing on OTPL exporter for Signoz

> **💡** _More about [opentelemetry-dotnet HERE](https://github.com/open-telemetry/opentelemetry-dotnet)_

---
## 🌐 ASPNET CORE


**Instalation**:

From command line:

```ps
$ dotnet add PROJECT package A55.Signoz.AspNetCore
```


## How to use it:

### Configuration

Add services on `WebApplicationBuild` in your `Program.cs` or another startup file:

```cs
using A55.Signoz;

var builder = WebApplication.CreateBuilder(args);

builder.UseSignoz();
/* other configurations */

var app = builder.Build();
```

And add the `Signoz` key in your `appsettings.json` and/or `appsettings.Environment.json`

```json
{
  // other configuration keys
  "Signoz": {
    "Enabled": true, // Enable or disables telemetry
    "ServiceName": null, // optional: The service name which will show in signoz, if null or not define will use Environment.ApplicationName
    "OtlpEndpoint": "https://signoz-env.a55.tech:4317", // The OTLP signoz endpoint
    "UseConsole": false, // Flags if opentelemetry will print the spans on console
    "UseOtlp": true, // Flags if opentelemetry send to the OTPL endpoint
    "ExportLogs": true, // Flags if opentelemetry will send logs
    "ExportTraces": true, // Flags if opentelemetry will send traces
    "ExportMetrics": true // Flags if opentelemetry will send metrics
  }
}

```
> ### See a configured [SAMPLE HERE](src/Signoz.Api.Sample/)

---
## 🌱 Environment Variables

If you enable use of Environment Variables in your web app with `.AddEnvironmentVariables()`, you can set any configuration value with `SIGNOZ__{PROPERTYNAME}`

Eg:

```bash
SIGNOZ__ServiceName=MyVeryCoolApp
SIGNOZ__UseOtplp=true
SIGNOZ__UseConsole=false
```

## 💅 Formating

The formating are defined by [EditorConfig](https://editorconfig.org) on [`.editorconfig`](.editorconfig)

The [CI](.github/workflows/publish.yml) uses [dotnet format](https://github.com/dotnet/format) to enforces the [`.editorconfig`](.editorconfig) and [installed analyzers](Directory.Build.props).

If you need to format the code, just run:

```ps
dotnet tool restore
dotnet format .
```

## 📝 Versioning
This project is versioned with [Semantic Versioning](https://semver.org/) aka `SemVer` using [GitVersion](https://gitversion.net/docs/)
