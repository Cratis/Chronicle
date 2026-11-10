---
title: Chronicle telemetry identifier privacy
description: Choose raw, hashed, or omitted event source ids in Chronicle traces, correlate hashes across processes, and protect logs separately from traces.
---

The Chronicle kernel hashes event source ids in its traces by default, so the original value is not sent to your tracing backend; .NET clients include ids unchanged unless you configure them. Choose `Raw` to include the id unchanged, or `Omitted` to leave it out of spans. Configure the kernel and each client separately; neither inherits the other's choice. These settings do not redact logs.

## Configure kernel traces

Set the mode in `chronicle.json`, without a `Cratis:Chronicle` wrapper:

```json
{
  "Telemetry": {
    "EventSourceIdTracing": "Hashed"
  }
}
```

Without a key, `Hashed` uses a random key generated when the kernel process starts. Hashes then correlate within that process but change on every restart and differ between kernel instances. To get hashes that stay stable across restarts and instances, supply a key through `Cratis__Chronicle__Telemetry__EventSourceIdHashKey` from your deployment's secret store. A configured key must be at least 32 characters; a shorter one fails configuration validation before the kernel starts. Do not put the key in a checked-in configuration file, a span, or a log.

| Setting | Default | Effect |
| --- | --- | --- |
| `Telemetry:EventSourceIdTracing` | `Hashed` on the kernel, `Raw` on clients | `Raw` includes the id, `Hashed` includes its keyed hash, and `Omitted` leaves out `cratis.eventsource.id` on kernel spans (`event_source_id` on client spans). |
| `Telemetry:EventSourceIdHashKey` | random per process | Used by `Hashed`; set it for hashes that are stable across restarts and instances. At least 32 characters when set. |

Choose `Raw` only when you want the original id in your tracing backend, including during local development. Protect that backend's access and retention accordingly. To omit ids instead, set `EventSourceIdTracing` to `Omitted`; no hash key is needed.

Kernel spans always omit `orleans.rpc.target_id` and `orleans.rpc.source_id`, regardless of this setting. These Orleans grain ids embed instance keys. Other Orleans tags, such as grain type and method, remain available.

## Configure clients

Use `WithTelemetry` when configuring your .NET client host. The [client tracing guide](/chronicle/hosting/client-tracing/#choose-how-event-source-ids-appear) shows the builder example. Client options expose the same `Telemetry:EventSourceIdTracing` and `Telemetry:EventSourceIdHashKey` properties. Worker and ASP.NET Core hosts both apply these options; they do not change event ids sent to the kernel or written to storage.

Inspect a newly exported span after changing the policy. Client spans use `event_source_id`; kernel spans use `cratis.eventsource.id`. Kernel spans also carry `cratis.event_source.id`, and client spans carry it when they use convention span names. The policy applies equally to every copy: with `Hashed`, each value is the same 32-character lowercase hexadecimal string; with `Omitted`, the identifier appears under none of the keys.

## Correlate hashes across processes

Chronicle uses HMAC-SHA256 over the UTF-8 id with the deployment key, takes the first 16 bytes, and writes 32 lowercase hexadecimal characters. This is a keyed pseudonym, not an unkeyed hash or a guarantee of anonymization.

Use the same key in the kernel and clients when you need the same id to have the same hash across those processes. Generate the key outside the application and supply it through your secret store. Rotating it changes every hash: searches and correlations across the rotation boundary no longer match. Keep retention and incident investigations in mind when you schedule a rotation.

## Metrics do not carry event source ids

Chronicle's append counters retain event name, store, and namespace dimensions, but never add `EventSourceId`. Observer failure counters exclude partition and event source id dimensions while retaining observer scope dimensions. They count failure and retry activity, not the number of currently failing partitions; see [observer failure metrics](open-telemetry.md#observer-failure-metrics) before using them in an alert.

Hashes do not fix metric cardinality: one hash per id still creates one series per id. These exclusions apply in every tracing mode, including `Raw`; changing trace configuration cannot add these metric dimensions back. [Convention metric names](telemetry-names.md) are opt-in through `Telemetry:MetricNames=Convention`; the default is `Legacy`. Use `Both` temporarily to publish both sets during migration. Migrate names and dimension keys together; do not sum the two series.

## Log levels during observer failures

Identifier tracing controls spans, not log redaction. Logs can still contain raw ids and, at Debug, exception details. Protect log access and retention independently of your tracing policy.

| Level | Observer behavior |
| --- | --- |
| Warning | The first failure in a partition episode reports partition, sequence number, and a low-cardinality failure kind. Quarantine transitions remain warnings. |
| Debug | Repeated failures report partition, sequence number, and attempt. Exception messages and stack traces are emitted separately at this level. |

For a local investigation that needs exception details, enable Debug for the observer category only, then turn it off again:

```json
{
  "Logging": {
    "LogLevel": {
      "Cratis.Chronicle.Observation.Observer": "Debug"
    }
  }
}
```

This reveals exception details, not a redacted view. Avoid enabling broad Debug logging in production merely to find a partition id.
