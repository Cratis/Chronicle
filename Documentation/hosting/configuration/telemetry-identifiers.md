---
title: Event source ids in telemetry
description: Choose Raw, Hashed, or Omitted event source ids in traces, and understand which identifiers Chronicle excludes from metric dimensions.
---
# Event source ids in telemetry

Event source ids can contain personal data. Chronicle includes them unchanged in traces by default. Choose `Hashed` to correlate an id without sending its original value, or `Omitted` to keep the id out of spans entirely. Configure the kernel and each client separately; neither inherits the other's choice.

## Configure kernel traces

Set these properties in the kernel configuration file:

```json
{
  "Telemetry": {
    "EventSourceIdTracing": "Hashed"
  }
}
```

Supply `Telemetry__EventSourceIdHashKey` from your deployment's secret store. `Hashed` requires a key of at least 32 characters; missing or shorter keys fail configuration validation before the kernel starts. Do not put the key in a checked-in configuration file, a span, or a log.

| Setting | Default | Effect |
| --- | --- | --- |
| `Telemetry:EventSourceIdTracing` | `Raw` | `Raw` includes the id, `Hashed` includes its keyed hash, and `Omitted` leaves out `cratis.eventsource.id` on kernel spans (`event_source_id` on client spans). |
| `Telemetry:EventSourceIdHashKey` | none | Required only for `Hashed`; at least 32 characters. |

The modes are mutually exclusive: `Hashed` and `Omitted` are both opt-ins. Choose `Raw` explicitly when you deliberately want the original id, including for local development. Raw ids leave the process and may identify people; protect your tracing backend accordingly.

Kernel spans always omit `orleans.rpc.target_id` and `orleans.rpc.source_id`, regardless of this setting. These Orleans grain ids embed instance keys. Other Orleans tags, such as grain type and method, remain available.

## Configure clients

Use `WithTelemetry` when configuring your client host. See [client tracing](/chronicle/hosting/client-tracing/#choose-how-event-source-ids-appear) for the builder example. Client options expose the same `Telemetry:EventSourceIdTracing` and `Telemetry:EventSourceIdHashKey` properties. Worker and ASP.NET Core hosts both apply them; they do not change event ids sent to the kernel or written to storage. Client spans use `event_source_id`; kernel spans use `cratis.eventsource.id`.

## Correlate hashes across processes

Chronicle uses HMAC-SHA256 over the UTF-8 id with the deployment key, takes the first 16 bytes, and writes 32 lowercase hexadecimal characters. This is a keyed pseudonym, not an unkeyed hash or a guarantee of anonymization.

Use the same key in the kernel and clients when you need the same id to have the same hash across those processes. Generate the key outside the application and supply it through your secret store. Rotating it changes every hash: searches and correlations across the rotation boundary no longer match. Keep retention and incident investigations in mind when you schedule a rotation.

## Metrics do not carry event source ids

Chronicle's append counters retain event name, store, and namespace dimensions, but never add `EventSourceId`. Observer failure counters exclude partition and event source id dimensions while retaining observer scope dimensions.

Hashes do not fix metric cardinality: one hash per id still creates one series per id. These exclusions apply in every tracing mode, including `Raw`; changing trace configuration cannot add these metric dimensions back. Instrument names are unchanged. The [convention-name migration](https://github.com/Cratis/Chronicle/issues/4459) is separate.

## Log levels during observer failures

Identifier tracing controls spans, not log redaction. Logs can still contain raw ids and, at Debug, exception details. Protect log access and retention independently of your tracing policy.

| Level | Observer behavior |
| --- | --- |
| Warning | The first failure in a partition episode reports partition, sequence number, and a low-cardinality failure kind. Quarantine transitions remain warnings. |
| Debug | Repeated failures report partition, sequence number, and attempt. Exception messages and stack traces are emitted separately at this level. |

For a local investigation, enable Debug for the observer category only, then turn it off again:

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
