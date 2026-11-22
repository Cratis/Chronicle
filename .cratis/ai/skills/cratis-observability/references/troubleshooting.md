<!-- cratis-ai-managed: skills/cratis-observability/references/troubleshooting.md -->
<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

# Troubleshooting

Check in this order for any missing signal: SDK not disabled, source subscribed, sampler, endpoint, then the backend.

| Symptom | Check |
| --- | --- |
| No Chronicle client spans | `Cratis.Chronicle.Client` is subscribed in the same tracing setup as the exporter (`AddCratisOpenTelemetry` does it via `Cratis.*`); an operation actually ran; the kernel's `Cratis.Chronicle` source does not enable client spans |
| No kernel spans or metrics | The kernel is configured separately; it exports only when an OTLP endpoint env var is set on the kernel process; `OrleansTracing` and `GrpcClientTracing` are off by default |
| No export at all | No endpoint set (export is opt-in); `OTEL_SDK_DISABLED=true`; `OTEL_*_EXPORTER` set without `otlp`; sampler records nothing or parent was unsampled |
| Application's own meter or source is silent | It is outside `Cratis.*`; subscribe it with `ConfigureTracing` / `ConfigureMetrics` (see **cratis-fundamentals-telemetry**) |
| Observer or reactor spans not under the command trace | Expected: observer spans carry no correlation id. Find them by observer id, event store, namespace and sequence; see `arc-and-chronicle-signals.md` |
| Search on correlation id finds only commit spans | Only unit-of-work commit and rollback spans carry it: key `correlation_id` in Legacy mode, `cratis.correlation_id` in Convention mode. Arc spans carry `cratis.correlation_id`. Append and observer spans never do |
| Duplicate spans or metrics | Both `AddCratisOpenTelemetry` and `UseOtlpExporter()` / `AddOtlpExporter()`, or both `AddCratisOpenTelemetry` and `WithCratis`; keep one |
| Dashboard shows old names after enabling convention | Legacy and convention series are distinct; update queries; do not sum them |
| Metric series stop growing or show `otel.metric.overflow=true` | SDK series limit reached; a tag is unbounded or the limit is too low. Arc `_other` is a separate 1,000-name cap |
| Wrong `service.name` | Something set it in code or the environment; `OTEL_SERVICE_NAME` wins for applications. The kernel always reports `Chronicle` |
| Personal ids or payloads in spans | Event source id mode is `Raw` (client default): choose `Hashed` or `Omitted`; remove the offending custom attribute; logs are not covered by these options, redact before formatting |
| Hashed ids do not match between client and kernel | Different keys, or the kernel generated a per-process key; configure the same 32-plus character key in both |
| Startup fails on telemetry configuration | Invalid OTLP endpoint, or `Hashed` chosen without a long enough key |
