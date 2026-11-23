<!-- cratis-ai-managed: skills/cratis-observability/references/dotnet-setup-and-options.md -->
<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

# .NET setup and options

Source: Fundamentals `Source/DotNET/OpenTelemetry/` (`OpenTelemetryExtensions.cs`, `InstrumentationExtensions.cs`, `CratisOpenTelemetryOptions.cs`), package `Cratis.OpenTelemetry`, namespace `Cratis.OpenTelemetry`. Read the release you consume.

## Entry points

| Call | Extends | Notes |
| --- | --- | --- |
| `AddCratisOpenTelemetry(configure?)` | `IHostApplicationBuilder` | Default choice. Reads configuration from the builder |
| `WithCratis(configure?)` and `WithCratis(configuration, configure?)` | `OpenTelemetryBuilder` | When the host already calls `AddOpenTelemetry()` |
| `AddCratisInstrumentation()` | `TracerProviderBuilder`, `MeterProviderBuilder` | Only `AddSource("Cratis.*")` / `AddMeter("Cratis.*")`; no exporter, no resource |

Use one of them, never two. The application callbacks run before the service provider is built.

## Options (`CratisOpenTelemetryOptions`)

| Option | Default | Purpose |
| --- | --- | --- |
| `ServiceName` | lowercase entry assembly name | Fallback only; `OTEL_SERVICE_NAME` wins |
| `ServiceVersion` | entry assembly informational version | Resource version |
| `IncludeExceptionDetails` | `false` | When false, exception log messages are reduced to `exception.type` |
| `ConfigureTracing` | none | Extra sources, for example gRPC client, MongoDB, Orleans |
| `ConfigureMetrics` | none | Extra meters |
| `EnableMetricViews` | `false` | Opt in to Cratis-owned metric views; the next three options require it (do not register other views for the same instruments) |
| `MetricCardinalityLimit` | `2000` | Series cap per stream; requires `EnableMetricViews` |
| `SecondsHistogramBoundaries` | 0, 0.005 ... 10 seconds | Bucket boundaries for duration histograms |
| `MetricPolicies` | empty | Per-metric overrides |

Extra sources example (application-owned scopes outside `Cratis.*` need an explicit subscription; see **cratis-fundamentals-telemetry**):

```csharp
builder.AddCratisOpenTelemetry(options =>
    options.ConfigureTracing = tracing => tracing.AddSource("MyApplication.Reports.ReportWorker"));
```

## What is registered

- Logs with `IncludeScopes` and `IncludeFormattedMessage`.
- `Cratis.*` sources and meters.
- ASP.NET Core (health-check requests are filtered out), HttpClient and runtime instrumentation.
- Resource defaults and its own OTLP exporter per signal, only when an endpoint is configured.

## Environment keys

Environment values win over configuration keys.

| Key | Effect |
| --- | --- |
| `OTEL_SERVICE_NAME`, `OTEL_RESOURCE_ATTRIBUTES` | Resource identity |
| `OTEL_EXPORTER_OTLP_ENDPOINT`, `..._PROTOCOL`, `..._HEADERS`, `..._TIMEOUT`, `..._COMPRESSION` | Shared exporter settings; per-signal forms (`OTEL_EXPORTER_OTLP_TRACES_ENDPOINT` and so on) override |
| `OTEL_TRACES_EXPORTER`, `OTEL_METRICS_EXPORTER`, `OTEL_LOGS_EXPORTER` | When set, must include `otlp` for that signal to export |
| `OTEL_TRACES_SAMPLER`, `OTEL_TRACES_SAMPLER_ARG` | Sampler |
| `OTEL_PROPAGATORS` | Context propagators |
| `OTEL_SDK_DISABLED` | `true` disables the providers |
| `OTEL_METRIC_EXPORT_INTERVAL`, `OTEL_METRIC_EXPORT_TIMEOUT` | Metric reader timing, milliseconds |

An invalid enabled endpoint fails at startup. Do not add product-specific telemetry variables: configure with the standard `OTEL_*` set only.

## Sampling

State the sampler explicitly in the deployment, for example `OTEL_TRACES_SAMPLER=parentbased_traceidratio` with `OTEL_TRACES_SAMPLER_ARG`. Changing sampling or adding instrumentation to raise volume needs a measured span rate as justification; this skill gives no default ratio. With a parent-based sampler an upstream "not sampled" decision removes the whole downstream trace.

## Cardinality

Keep metric tags to bounded categories. Excess series fold into one series carrying only `otel.metric.overflow=true`. Raising a limit reserves memory per instrument; raise it only as far as measured.
