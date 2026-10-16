---
name: cratis-fundamentals-telemetry
description: Instrument a .NET service with Cratis.Metrics.Roslyn generated counters, gauges, histograms and spans, and subscribe/export them through Cratis.OpenTelemetry. Use for partial instrumentation declarations, missing telemetry, host setup or metric-name upgrades. Do not use for operating a collector or for application audit records.
license: MIT
---
<!-- cratis-ai-managed: skills/cratis-fundamentals-telemetry/SKILL.md -->

<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

# Generated metrics and traces

Use this when a service needs measurements and spans, not business facts. Keep event sourcing and audit records separate from telemetry.

## Verified product sources

All source locators below are in **Cratis/Fundamentals**. Read the release you consume, not a sibling checkout's HEAD.

| Package | Source |
| --- | --- |
| `Cratis.Fundamentals` | `Source/DotNET/Fundamentals/Metrics/{Counter,Gauge,Histogram}Attribute.cs`, `Traces/SpanAttribute.cs`, `Diagnostics/TagAttribute.cs` |
| `Cratis.Metrics.Roslyn` | `Source/DotNET/Metrics.Roslyn/MetricsSourceGenerator.cs`; `Templates/Metrics.hbs` |
| `Cratis.OpenTelemetry` | `Source/DotNET/OpenTelemetry/OpenTelemetryExtensions.cs`; `InstrumentationExtensions.cs`; `CratisOpenTelemetryOptions.cs` |

Registration evidence: `Source/DotNET/Fundamentals/DependencyInjection/ServiceCollectionExtensions.cs`. Unkeyed names: `Metrics/UnkeyedMeter.cs`, `Traces/UnkeyedActivitySource.cs` under the same Fundamentals source root. Upgrade context: `Documentation/csharp/metrics/roslyn.md` (Exported Metric Names) and `Documentation/csharp/opentelemetry.md`.

## 1. Declare instrumentation beside its owner

Reference `Cratis.Fundamentals` for runtime types and `Cratis.Metrics.Roslyn` for generation. Use a **static partial class in a namespace**, containing **static partial methods**. The generator's candidate filter requires static classes even for metrics; a merely partial instance class is not enough (`MetricsSourceGenerator.cs`). Prefer extension declarations with short, unqualified attribute/type names, as below.

This declaration file is complete; it assumes the application's existing `ReportWorker` class in `MyApplication.Reports`:

```csharp
// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using Cratis.Diagnostics;
using Cratis.Metrics;
using Cratis.Traces;

namespace MyApplication.Reports;

public static partial class ReportInstrumentation
{
    [Counter<int>("reports.completed", "Completed reports")]
    internal static partial void Completed(this IMeter<ReportWorker> meter,
        [Tag("reports.outcome")] string outcome);

    [Gauge<int>("reports.pending", "Pending reports")]
    internal static partial void Pending(this IMeter<ReportWorker> meter, int count);

    [Histogram<double>("reports.duration", "Report duration", "s")]
    internal static partial void Duration(this IMeter<ReportWorker> meter, double seconds);

    [Span("reports.build", ActivityKind.Internal)]
    internal static partial IActivityScope<ReportWorker> Build(
        this IActivitySource<ReportWorker> source,
        [Tag("reports.format")] string format);
}
```

- The first metric argument is `IMeter<T>` or `IMeterScope<T>`. A counter without a measurement increments by one. The first remaining parameter, in any position, whose type exactly matches the attribute's numeric type is the measurement, not a tag; avoid a second parameter of that type. Gauges and histograms require that measurement (`MetricsSourceGenerator.cs`).
- Remaining parameters are tags. Metric keys default to parameter names; span keys default to snake_case. `[Tag("key")]` from `Cratis.Diagnostics` overrides either.
- `IMeterScope<T>` merges scope tags with method tags (`Templates/Metrics.hbs`). Do not put credentials, personal payloads or unbounded entity IDs in metric labels; choose bounded categories.
- Name, description and optional UCUM unit are distinct. A duration in seconds uses `"s"` and a measurement in seconds, not milliseconds (`HistogramAttribute.cs`, template).
- A span returns `IActivityScope<T>` and takes `IActivitySource<T>` first. Assign the returned scope with `using`; `CRT0001` checks disposal (`ActivityScopeUsingAnalyzer.cs`, `Templates/Metrics.hbs`).

Call-site excerpt inside the existing worker, with injected `IMeter<ReportWorker> meter` and `IActivitySource<ReportWorker> source`:

```csharp
using var activity = source.Build("pdf");
meter.Pending(3);
meter.Completed("succeeded");
meter.Duration(0.25);
```

This shows recording, not report execution. Both `AddBindingsByConvention()` and `AddSelfBindings()` register unkeyed `IMeter<>` and `IActivitySource<>` as singletons. For a plain host, use the normal type-discovery/convention setup from **cratis-fundamentals-type-discovery**; a generated method alone does not register its dependencies.

## 2. Subscribe the actual instrumentation name

**Unkeyed instruments use `typeof(T).FullName`**, not the metric name and not the host service name. `AddCratisInstrumentation()` subscribes only to `Cratis.*`. An application namespace such as `MyApplication.Reports.ReportWorker` needs an explicit meter/source subscription. This is the first check when generation works but nothing is exported.

Reference `Cratis.OpenTelemetry` explicitly for SDK/exporter setup. Host-setup excerpt for an existing `IHostApplicationBuilder builder` (including a `WebApplicationBuilder`), before building the host:

```csharp
using Cratis.OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using MyApplication.Reports;

builder.AddCratisOpenTelemetry(options =>
{
    options.ServiceName = "report-service";
    options.ConfigureMetrics = metrics => metrics.AddMeter(typeof(ReportWorker).FullName!);
    options.ConfigureTracing = tracing => tracing.AddSource(typeof(ReportWorker).FullName!);
});
```

`AddCratisOpenTelemetry` extends `IHostApplicationBuilder`. When the host already owns an OpenTelemetry builder, use `services.AddOpenTelemetry().WithCratis(configuration, configure)` instead; `WithCratis` extends `OpenTelemetryBuilder`. Do not add both setups. If the application only wants subscription helpers, `AddCratisInstrumentation()` extends both `TracerProviderBuilder` and `MeterProviderBuilder`, not the service collection (`OpenTelemetryExtensions.cs`, `InstrumentationExtensions.cs`).

Shared setup adds Cratis sources/meters, ASP.NET Core and HTTP client instrumentation, runtime metrics, and scoped/formatted logs. Application callbacks run before DI is built (`OpenTelemetryExtensions.cs`).

## 3. Configure export, without baking endpoints or secrets into code

OTLP export is **opt-in**: no configured endpoint means no OTLP exporter. Environment values take precedence over configuration keys. Relevant standard keys (`OpenTelemetryExtensions.cs`):

| Key | Effect |
| --- | --- |
| `OTEL_SERVICE_NAME`, `OTEL_RESOURCE_ATTRIBUTES` | Resource values override application defaults |
| `OTEL_EXPORTER_OTLP_ENDPOINT` | Shared absolute HTTP/HTTPS endpoint |
| `OTEL_EXPORTER_OTLP_TRACES_ENDPOINT`, `_METRICS_ENDPOINT`, `_LOGS_ENDPOINT` | Signal-specific endpoints override the shared endpoint |
| `OTEL_EXPORTER_OTLP_PROTOCOL` (or signal-specific equivalent) | `http/protobuf` selects HTTP; otherwise gRPC |
| `OTEL_TRACES_EXPORTER`, `OTEL_METRICS_EXPORTER`, `OTEL_LOGS_EXPORTER` | If supplied, must include `otlp` for that signal to export |
| `OTEL_SDK_DISABLED=true` | Disables providers |
| `OTEL_METRIC_EXPORT_INTERVAL`, `OTEL_METRIC_EXPORT_TIMEOUT` | Metric reader timing in milliseconds |
| `OTEL_TRACES_SAMPLER`, `OTEL_TRACES_SAMPLER_ARG` | Sampler and ratio configuration |

For HTTP/protobuf, the shared endpoint gains `/v1/traces`, `/v1/metrics` or `/v1/logs`; signal-specific endpoints are used as supplied. An enabled invalid endpoint throws `InvalidOpenTelemetryEndpoint`. Treat exporter headers as secrets; obtain them through the approved environment/secret mechanism, never a snippet or committed configuration.

## Upgrade and prove it

- Fundamentals stopped bringing the OpenTelemetry SDK transitively; add the explicit telemetry package when migrating a consumer that relied on that dependency (`Documentation/csharp/opentelemetry.md`).
- Regenerating after an upgrade from a Fundamentals older than 7.20.0 can rename exported Prometheus metrics: descriptions are HELP text, not units. Rebuild the consumer and update dashboards/alerts that used the old description-suffixed names (`metrics/roslyn.md`).
- Build with analyzers and warnings as errors. Inspect generated implementations for correct measurements/tags and scope disposal. Do not edit generated files.
- Exercise one code path with a listener or the configured collector: observe the counter increment, histogram unit/measurement and completed span. A build or `skill-contains` assertion is not proof of export. Check namespace subscriptions, SDK disablement, sampler and endpoint independently before diagnosing the collector.
