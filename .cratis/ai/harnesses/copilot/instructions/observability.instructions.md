---
applyTo: "**/*Traces.cs,**/*Metrics.cs,**/*Telemetry*,**/Telemetry/**,**/observability.ts,**/*Observation*.kt,**/*Observation*.java"
paths:
  - "**/*Traces.cs"
  - "**/*Metrics.cs"
  - "**/*Telemetry*"
  - "**/Telemetry/**"
  - "**/observability.ts"
  - "**/*Observation*.kt"
  - "**/*Observation*.java"
---
<!-- cratis-ai-managed: harnesses/copilot/instructions/observability.instructions.md -->
<!-- cratis-ai: generated Copilot instructions. Do not edit; rules/ is the source. -->

# Observability (OpenTelemetry)

Every line is **[contract]** (binding) or **[convention]** (the house default). The source of truth is
[Architecture decision 0001](https://github.com/Cratis/Architecture/blob/c70fab646ed576bea054744d34bcbb0bc0cd44f3/decisions/0001-opentelemetry-convention.md);
generated .NET instruments are in the **cratis-fundamentals-telemetry** skill. Names are the same in every language.

## Naming

- **[contract]** Instrumentation scope (ActivitySource, Meter, tracer): `Cratis.<Product>[.<Part>]`, version = package version.
- **[contract]** Span names: `cratis.<product>.<entity>.<operation>`, lowercase, low cardinality. Event, command and query type names go in attributes, never in the span name.
- **[contract]** Attribute keys use OTel semantic conventions first (`error.type`, `rpc.*`, `http.*`, `db.*`), else `cratis.<concept>.<attr>` (cross-product) or `cratis.<product>.<concept>.<attr>`. Enum values are lowercase.
- **[contract]** Metrics: `cratis.<product>.<entity>.<measure>`, no unit or `_total` suffix, with a UCUM unit and a description. Durations are histograms in seconds.
- **[contract]** Metric attributes never carry per-partition, per-event-source or correlation values.
- **[contract]** Names are public API: expose them as constants, and on a rename ship both names for one minor release.

## Never record

- **[contract]** Payload values of events, commands, queries or read models; `[PII]` or `[NotAudited]` values; identity subject, name or email; tokens.
- **[contract]** Exception messages from non-Cratis code: record `exception.type` only.
- **[contract]** Event source id is off by default (`cratis.event_source.id`). Record it only when explicitly enabled, hashed (HMAC) or raw.
- **[contract]** Tenant never goes in the resource or in baggage. The only baggage key is `cratis.correlation_id`.
- **[contract]** Redact structured values before formatting a log message; an interpolated value cannot be reliably removed later.

## Setup

- **[contract]** Call the platform's single Cratis setup entry point; never hand-roll providers. In .NET that is `AddCratisOpenTelemetry` (`Cratis.OpenTelemetry`, Fundamentals). Node, JVM, Elixir and Python entry points are defined by the decision but not verified here; state them as planned.
- **[contract]** Never hard-code `service.name`: `OTEL_SERVICE_NAME` wins, code supplies only a default when it is unset.
- **[contract]** Configure with standard `OTEL_*` variables only; no product-specific telemetry variables or switches.
- **[contract]** Export only when an OTLP endpoint is configured. Raising telemetry volume (sampling, new instrumentation) cites a measured rate.
