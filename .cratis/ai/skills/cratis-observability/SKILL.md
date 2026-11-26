---
name: cratis-observability
description: Use when setting up, verifying or troubleshooting OpenTelemetry in an application built on Cratis (Arc, Chronicle client, kernel), such as missing spans, export, sampling or sensitive span data. Not for generated instruments (cratis-fundamentals-telemetry).
license: MIT
---
<!-- cratis-ai-managed: skills/cratis-observability/SKILL.md -->

<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

# Observability across the Cratis stack

Wire telemetry once, prove spans and metrics reach a backend, and keep sensitive values out of them. The binding naming and "never record" contract is the Cratis OpenTelemetry convention; read `.cratis/ai/rules/observability.md` for it and do not restate it in code comments.

Sibling skills own adjacent ground:

- **cratis-fundamentals-telemetry** declares generated metrics and spans (`[Counter]`, `[Span]`) and subscribes application meters. Use it for custom instruments.
- **cratis-chronicle-configuration** owns kernel storage, TLS and other kernel environment settings.
- **cratis-chronicle-client-elixir** (`references/observability.md`) owns Elixir client tracing.

## Setup (.NET)

The single entry point is `AddCratisOpenTelemetry` from the `Cratis.OpenTelemetry` package (Fundamentals; a separate package from `Cratis.Fundamentals`). Do not hand-roll `AddSource` lists for Cratis scopes.

```csharp
using Cratis.OpenTelemetry;

builder.AddCratisOpenTelemetry(options => options.ServiceName = "orders-api");
```

`builder` is the host's `IHostApplicationBuilder` (a `WebApplicationBuilder` or a worker host builder). Call it once, before building the host.

It registers the `Cratis.*` activity sources and meters (so Arc, the Chronicle client and kernel scopes are all covered), ASP.NET Core, HttpClient and runtime instrumentation, scoped and formatted logs, and its own OTLP exporter per signal. It brings its own exporter: **do not also call `UseOtlpExporter()` or `AddOtlpExporter()`**, or the host exports twice.

Alternatives, chosen only by condition:

| Condition | Use |
| --- | --- |
| The host already calls `AddOpenTelemetry()` | `services.AddOpenTelemetry().WithCratis(configuration)` (do not also call `AddCratisOpenTelemetry`) |
| The application owns the whole provider pipeline | `AddCratisInstrumentation()` on `TracerProviderBuilder` and `MeterProviderBuilder`: it only subscribes `Cratis.*` |
| Extra sources such as gRPC client, MongoDB or Orleans | `ConfigureTracing` / `ConfigureMetrics` on the options |

Read `references/dotnet-setup-and-options.md` when you need the option table, environment keys, resource identity, metric views or cardinality limits.

### Configure through `OTEL_*` only

- Export is opt-in: with no OTLP endpoint configured there is no OTLP exporter. Set `OTEL_EXPORTER_OTLP_ENDPOINT` (or a per-signal endpoint) in the deployment environment.
- `OTEL_SERVICE_NAME` wins. `options.ServiceName` is only the fallback when it is unset; never hard-code the service name.
- Sampling: set `OTEL_TRACES_SAMPLER` (for example `parentbased_traceidratio`) with `OTEL_TRACES_SAMPLER_ARG`, and justify a changed rate with a measured span rate. A parent-based sampler inherits the upstream decision, so an unsampled caller hides downstream spans.
- `OTEL_SDK_DISABLED=true` turns the providers off.
- Exporter headers are secrets: take them from the deployment's secret store, never from code or committed configuration.

## What you get

| Scope (source and meter) | Emits |
| --- | --- |
| `Cratis.Arc` | Command and query spans and metrics (`cratis.arc.command.execute`, `cratis.arc.query.perform`, `cratis.arc.command.duration`) |
| `Cratis.Arc.MongoDB` | MongoDB meter |
| `Cratis.Chronicle.Client` | Client spans for append, unit of work, reactor and reducer handling |
| `Cratis.Chronicle` | Kernel spans and meters (kernel also uses `Cratis.Chronicle.Federation`) |

Read `references/arc-and-chronicle-signals.md` when you need span, attribute and metric names or the legacy versus convention naming switches.

Chronicle emits **legacy** names by default (client spans `client.event_sequence.append`, kernel metric names unchanged). `Telemetry:SpanNames=Convention` (client) and `Telemetry:MetricNames=Convention` or `Both` (kernel) opt into the `cratis.chronicle.*` names. Dashboards and alerts must use whichever mode is active; never sum a legacy and a convention series.

## Trace boundaries

- A command reaching the Chronicle client and its append calls are one trace; the client propagates trace context over gRPC.
- Observer, reactor and reducer processing is **not** guaranteed to sit under the command's trace. Do not expect observer spans inside the trace of the command that caused them.
- Correlation is partial. Arc spans and the unit-of-work commit and rollback client spans carry the correlation id (`cratis.correlation_id`; the commit span uses `correlation_id` in Legacy mode). Append, reactor and reducer spans carry none in either mode, and observer spans cannot be joined to the command by id: find them by observer id, store, namespace and sequence. The only baggage key is `cratis.correlation_id`. Exact attributes per mode are in `references/arc-and-chronicle-signals.md`.

## Never record

The rule lists the contract. The practical traps:

- Event source ids: the .NET client's `EventSourceIdTracing` defaults to `Raw`, so ids reach the backend unchanged. Choose `Hashed` (needs `Telemetry:EventSourceIdHashKey` of at least 32 characters from a secret store) or `Omitted` unless the id is known to be non-personal. The kernel defaults to `Hashed` with a per-process random key when none is set, so kernel hashes correlate only within one process. Set client and kernel separately, with the same key to correlate. These options do not redact logs.
- Failed append and handle spans carry `error.type`, not exception messages. Do the same in your own spans; `IncludeExceptionDetails` is off by default, keep it off in production.
- No command, event, query or read model payloads, tokens or identity values in span attributes, metric tags, baggage or resource attributes. Tenant is never a resource attribute or baggage.
- Metric tags are bounded categories, never ids.

## Verify

Run the app against a local viewer; do not claim telemetry works from a clean build.

1. Start the Aspire dashboard container (image `mcr.microsoft.com/dotnet/aspire-dashboard`, UI on 18888, OTLP/gRPC published as 4317). Anonymous access is local-only. See `references/chronicle-kernel-export.md` (Local viewer).
2. Start the app with `OTEL_EXPORTER_OTLP_ENDPOINT=http://localhost:4317` and open `http://localhost:18888`.
3. Issue one command that appends an event. Pass when the **Traces** view shows your service name, a `cratis.arc.command.execute` span and a client append span (`client.event_sequence.append`, or `cratis.chronicle.client.event_sequence.append` in convention mode) in one trace.
4. Pass when a reactor span (`client.reactor.handle`; a reducer emits `client.reducer.handle`) appears as its own trace, not nested under the command.
5. Pass when the same run with `OTEL_EXPORTER_OTLP_ENDPOINT` unset exports nothing and nothing fails.
6. Stop the dashboard container when done.

## Other platforms

- Elixir: read `cratis-chronicle-client-elixir` `references/observability.md`.
- TypeScript, JVM, Python and Go: the convention defines a shared setup entry point for each, but none is verified in this corpus. Treat them as planned; do not invent an API. Use the standard `OTEL_*` variables and the naming rule meanwhile.

## Pitfalls

- A source missing from the tracer provider drops its spans silently with no warning.
- Calling both `AddCratisOpenTelemetry` and `UseOtlpExporter()` duplicates every signal.
- A hard-coded service name hides a wrong deployment identity; supply it through `OTEL_SERVICE_NAME`, with `options.ServiceName` only as the fallback.
- Unbounded metric tags create series until the SDK folds the rest into one `otel.metric.overflow=true` series. Arc separately caps 1,000 distinct command or query names and records the rest as `_other`. These are different limits.
- Rejections (validation, authorization) do not fail a command span; read the outcome attribute.
- The Chronicle kernel's own setup (`AddChronicleTelemetry`) exports only when an endpoint is set and always reports `service.name` as `Chronicle` (`OTEL_SERVICE_NAME` does not change it); the kernel is configured separately from the application.

## References

- Read `references/dotnet-setup-and-options.md` when configuring options, environment keys, resource identity, sampling or cardinality.
- Read `references/arc-and-chronicle-signals.md` when you need exact span, attribute and metric names for Arc or Chronicle.
- Read `references/chronicle-kernel-export.md` when exporting the kernel's telemetry, tuning its options or alerting on observer failures.
- Read `references/troubleshooting.md` when spans are missing, duplicated or orphaned, or sensitive values appear.
