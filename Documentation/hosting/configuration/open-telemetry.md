---
title: Chronicle OpenTelemetry configuration
description: Export Chronicle metrics, traces, and logs over OTLP, choose how traces expose event source ids, and connect a local or hosted telemetry backend.
---
# Open Telemetry

Chronicle exports metrics, traces, and logs through the [OpenTelemetry Protocol (OTLP)](https://opentelemetry.io/docs/specs/otlp/) so you can investigate event processing and observer failures in your telemetry backend. Instrumentation is always enabled; use standard OpenTelemetry environment variables to configure export.

## Signals

Chronicle exports the following signals:

| Signal | Description |
| --- | --- |
| Metrics | Application and infrastructure metrics (event sequences, observers, .NET runtime) |
| Traces | Distributed traces for Chronicle operations (gRPC, HTTP, Orleans) |
| Logs | Structured application logs |

## Configuration

Telemetry is configured using standard OpenTelemetry environment variables:

| Variable | Default | Description |
| --- | --- | --- |
| `OTEL_EXPORTER_OTLP_ENDPOINT` | exporter default | OTLP receiver endpoint (e.g. `http://localhost:4317`) |
| `OTEL_EXPORTER_OTLP_PROTOCOL` | `grpc` | Export protocol: `grpc` or `http/protobuf` |
| `OTEL_EXPORTER_OTLP_HEADERS` | none | Additional headers (e.g. API keys for cloud backends) |
| `OTEL_SERVICE_NAME` | `Chronicle` | Service name reported to the telemetry backend |

Set `OTEL_EXPORTER_OTLP_ENDPOINT` to the receiver you want Chronicle to use. Leaving it unset is not an opt-out: Chronicle registers OTLP exporters without requiring this variable.

## Event source ids and log levels

Before exporting traces, decide whether your backend should receive original event source ids. Chronicle uses `Raw` by default; choose `Hashed` for keyed correlation or `Omitted` to leave the id out. Apply the choice separately in the kernel and each client.

The kernel removes Orleans grain-instance id tags before span export, and Chronicle append metrics do not use event source ids as dimensions. Logs can still contain raw ids and exception details. Follow [event source ids in telemetry](/chronicle/hosting/configuration/telemetry-identifiers/) to configure the policy and plan key rotation; protect log access and retention separately.

## Local development with .NET Aspire Dashboard

For a local telemetry viewer, use the [.NET Aspire Dashboard](https://learn.microsoft.com/en-us/dotnet/aspire/fundamentals/dashboard/overview) service in `Source/Kernel/Server/docker-compose.yml`. It receives OTLP data and displays metrics, traces, and logs. The example below allows anonymous access, so keep it local and do not expose its ports publicly.

```yaml
aspire-dashboard:
  image: mcr.microsoft.com/dotnet/aspire-dashboard:latest
  environment:
    - DOTNET_DASHBOARD_UNSECURED_ALLOW_ANONYMOUS=true
    - ALLOW_UNSECURED_TRANSPORT=true
  ports:
    - 18888:18888   # dashboard UI
    - 4317:18889    # OTLP/gRPC receiver
```

From `Source/Kernel/Server`, start the services:

```bash
docker compose up -d
```

When running the kernel on your host machine, set the endpoint to the dashboard's published OTLP port:

```bash
OTEL_EXPORTER_OTLP_ENDPOINT=http://localhost:4317 dotnet run
```

Open the Aspire Dashboard at [http://localhost:18888](http://localhost:18888) to view metrics, traces, and logs.

## Example: Prometheus and Grafana

To view Chronicle metrics in Grafana through Prometheus, send them to an OTLP receiver with Prometheus export configured, such as the [OpenTelemetry Collector](https://opentelemetry.io/docs/collector/):

```bash
OTEL_EXPORTER_OTLP_ENDPOINT=http://otel-collector:4317
```

## Example: cloud observability backends

Most cloud observability backends (Datadog, Honeycomb, Grafana Cloud, Azure Monitor) accept OTLP. Set the endpoint and any required authentication headers:

```bash
OTEL_EXPORTER_OTLP_ENDPOINT=https://otlp.example.com
OTEL_EXPORTER_OTLP_HEADERS=x-api-key=your-api-key
```

## Generated instrument names

Chronicle uses `Cratis.Metrics.Roslyn` 7.32.0 to generate counters and gauges. Descriptions belong to the instrument's description metadata, not its unit. For example, the successful-observation counter is named `chronicle-observer-successful-observations`, has no unit, and has the description `Number of successful observations per observer in a given event store and namespace`.

With default Prometheus translation, expect `chronicle_observer_successful_observations_total` with that description as its HELP text, rather than a name containing the description. Check the name exposed by your backend before writing a query: OTLP serialization alone does not verify Prometheus exposition or HELP text.

The observer failure counters and suspected-undelivered queue counter are hand-written to preserve their failure-path initialization and observer tagging. Their names are unchanged. The [convention-name migration](https://github.com/Cratis/Chronicle/issues/4459) is a separate change; do not assume a generator upgrade also renames instruments in your dashboards and alerts.

## Metrics instrumented

Chronicle instruments the following meters:

| Meter | Description |
| --- | --- |
| `Cratis.Chronicle` | Chronicle application metrics (events appended, observers, etc.) |
| `Microsoft.Orleans` | Orleans distributed actor framework metrics |
| `Grpc.AspNetCore.Server` | gRPC server request metrics |
| .NET runtime | GC, thread pool, and memory metrics from the .NET runtime |

### SQL startup, contention and live queries

SQL storage publishes these instruments on the `Cratis.Chronicle` meter:

| Instrument | Kind | Tags | Meaning |
| --- | --- | --- | --- |
| `chronicle-sql-schema-lock-wait` | Histogram (seconds) | `provider`, `outcome` | Elapsed database schema-lock acquisition time, including failed waits. Outcomes are `acquired` and `failed`. |
| `chronicle-sql-upsert-collisions-recovered` | Counter | `operation` | Concurrent inserts handled by an existing SQL recovery or idempotency path. Unhandled conflicts and ordinary updates do not increment it. |
| `chronicle-sql-live-query-poll-failures` | Counter | `query`, `kind` | A failed SQL live-query poll. The query is retried with backoff (polling interval doubling to 30 s, or the polling interval if longer) and the subscription stays open. `kind` is `transient` or `other`; a sustained `other` rate needs an operator. |

Schema-lock timing excludes opening the connection, migration execution, and the
in-process migration semaphore. SQLite has no database schema lock and emits no
schema-lock wait measurement. Collision operation labels identify storage paths,
not individual rows, event stores, or namespaces.

The `Cratis.Chronicle.Storage.Sql.Database` logger emits an Information event for
each EF migration it starts applying, with `Node` (machine name), `ProcessId`,
`Store` (physical database name), and `Migration`. An attempt can still fail or
roll back; this event is not proof of completion. Already-applied migrations emit
no event. Connection strings and credentials are not included.

The kernel health endpoint reports healthy only after the host signals that
startup has completed, including startup migrations and silo initialization. It
reports unhealthy during shutdown. Later on-demand schema work does not reopen
the startup readiness gate.

### Observer failure metrics

Chronicle counts observer failures on the `Cratis.Chronicle` meter. Every one of these instruments carries the tags `EventStore`, `Namespace`, `ObserverId` and `EventSequenceId`, and no others. Neither the partition nor the event source id is a tag, so the number of series depends on the number of observers and never on how many partitions fail.

| Instrument | Prometheus name | Counts |
| --- | --- | --- |
| `chronicle-observer-partitions-failed` | `chronicle_observer_partitions_failed_total` | Every failed handling attempt of a partition, including the failure of each retry. It is not a count of failed partitions. |
| `chronicle-observer-partition-retry-attempts` | `chronicle_observer_partition_retry_attempts_total` | Every time a failed partition was evaluated for retry. It is recorded together with each failed attempt, whether or not a retry ends up being scheduled, so it does not count retries that ran. |
| `chronicle-observer-partitions-quarantined` | `chronicle_observer_partitions_quarantined_total` | Partitions that ran out of retry attempts and were quarantined. It does not count an observer being quarantined. |
| `chronicle-observer-quarantined` | `chronicle_observer_quarantined_total` | Times an observer was quarantined. |

The Prometheus names are what the OpenTelemetry Collector's Prometheus exporter and Prometheus's own OTLP receiver produce by default. These four instruments have no unit and carry their description, so a Prometheus `HELP` line explains each of them. Earlier Kernels created them with the description in the unit position, which gave them long names such as `chronicle_observer_partitions_failed_Number_of_failed_partitions_per_observer_in_a_given_event_store_and_namespace_total` and an empty `HELP`.

An observer has a series on these instruments from its first failure, not before: Chronicle records nothing when an observer starts, so healthy observers add no series and the number of series follows the number of observers that fail. The first value a backend receives for an observer is therefore already `1`, and alerts on these counters should not depend on seeing a `0` first. The [alerting guide](../alerting-on-observer-failures.md) shows how.

The SDK limits each instrument to 500 series. Beyond that it folds the excess into one series without the tags, which can no longer be attributed to an observer. That takes more than 500 observer instances in one process, counted across observers, namespaces and event stores.

### Suspected skipped events

Two counters on the `Cratis.Chronicle` meter show how often an observer's position moved past a live event without any delivery path covering it. The first has the observer tags `EventStore`, `Namespace`, `ObserverId` and `EventSequenceId`, and no others. The second has the appended-events queue tags `EventStore`, `Namespace`, `EventSequenceId` and `QueueId`, plus `ObserverId`.

| Instrument | Prometheus name | Counts |
| --- | --- | --- |
| `chronicle-observer-events-suspected-skipped` | `chronicle_observer_events_suspected_skipped_total` | Live events an observer's position moved past without the observer seeing any delivery path cover them. |
| `chronicle-appended-events-queue-suspected-undelivered-events` | `chronicle_appended_events_queue_suspected_undelivered_events_total` | Event deliveries to an observer whose dispatch failed. One event counts once for each observer it was dispatched to. |

These counters show suspicions, not proof. A catch-up job may have delivered an event without reporting it, and a dispatch that failed may still have reached the observer before it failed. Counting changes nothing about delivery. The counters count every suspicion, while the matching warnings are rate-limited to 10 per observer, or per queue, per minute, with a summary of how many were not logged. Like the failure counters, they only have a series once something is suspected.

When one of them increases, see [An observer may have skipped events](/chronicle/troubleshooting/#an-observer-may-have-skipped-events).

### Spills to catch-up

The appended-events queue counts every time it moves an observer from live delivery to catch-up. The instrument has the appended-events queue tags `EventStore`, `Namespace`, `EventSequenceId` and `QueueId`, plus `ObserverId` and `Reason`.

| Instrument | Prometheus name | Counts |
| --- | --- | --- |
| `chronicle-appended-events-queue-observers-spilled-to-catchup` | `chronicle_appended_events_queue_observers_spilled_to_catchup_total` | Observers moved from live delivery to catch-up, once per observer and spill. |

`Reason` is `QueueFull` when the queue overflowed, `DispatchFailed` when delivery to the observer failed, and `FragmentedBacklog` when the observer's share of one dispatch would have needed more than 256 sequential per-partition deliveries. Like the failure counters, an observer only has a series once it has been spilled.

### Open alert incident metrics

Open alert incident metrics are enabled by default. To opt out, set
`Cratis__Chronicle__Alerts__IncidentMetricsEnabled=false` on **every silo** and restart
all silos. Alternatively, set `Alerts.IncidentMetricsEnabled` to `false` in
`chronicle.json`:

```json
{
  "Alerts": {
    "IncidentMetricsEnabled": false
  }
}
```

This startup setting prevents registration of both incident gauges, owner keepalive
pings, periodic sampling, and incident-storage reads for metrics. It does not merely
filter exported data. It leaves alert raising, incident materialization, incident
queries, observer failure counters, and other telemetry unchanged. Setting
`Alerts.Enabled=false` controls alert raising, not incident metrics. To restore
incident metrics, set `IncidentMetricsEnabled` to `true` and restart every silo.
A mixed cluster can still collect and publish metrics from enabled silos; complete
the restart before relying on the opt-out. Disable or adjust alerts that depend on
these gauges, including availability alerts, because disabled gauges are absent,
not zero.

Chronicle publishes the number of open alert incidents on the `Cratis.Chronicle` meter. The `chronicle-alerts-open-incidents` gauge is exported to Prometheus as `chronicle_alerts_open_incidents` (a gauge has no `_total` suffix). It has one series per bucket, tagged `EventStore`, `Namespace`, `ObserverId`, `EventSequenceId`, `Condition` and `Severity`, and no others. The `Severity` tag holds the incident's severity (`Warning` or `Critical`).

The value is sampled from incident storage, not counted from live events. One owner grain in the cluster aggregates the open incident rows every 30 seconds and publishes the result; a scrape reads that snapshot and does no storage I/O. An escalation moves an incident between `Severity` buckets without changing the total.

- A snapshot older than 90 seconds publishes nothing, rather than zero. The same applies to a refresh that cannot run: while incident materialization is catching up or degraded, or storage fails, Chronicle keeps the previous snapshot until it ages out. Absence of the series therefore means "unknown", not "no incidents".
- A bucket that empties reports `0` for 5 minutes and is then retired.
- `chronicle-alerts-open-incidents-available` (`chronicle_alerts_open_incidents_available`, no tags) is `1` while the instance that owns the snapshot has a fresh one and `0` while its snapshot is missing or stale. Instances that do not own the snapshot report no series.

Aggregate across instances with `max`, never `sum`. A silo that is partitioned or hung can keep publishing its last snapshot for up to 90 seconds after another instance has taken ownership, so more than one instance can report a series for a short time. Summing would double count; `max by (EventStore, Namespace, ObserverId, EventSequenceId, Condition, Severity)` does not.

When the silo that owns the snapshot dies, another silo re-activates the owner within about 30 seconds. The new owner starts with no memory of the buckets that were retiring, so a series can disappear without ever reporting `0`, and a backend's lookback can keep returning the last value briefly. Do not depend on seeing a `0` before a series ends.

Like the other instruments, this gauge is limited to 500 series per process. The series count is the number of observers with incidents, times the conditions and severities they hold.

To be alerted when an observer's partitions keep failing or run out of retries, see [Get alerted when observers stop processing](../alerting-on-observer-failures.md).

## Traces instrumented

These are the server's sources. The Chronicle client in your application emits its own spans on the `Cratis.Chronicle.Client` source, which you register yourself; see [Trace Chronicle client operations with OpenTelemetry](../client-tracing).

Failed append and observer handle operations set Error status and `error.type` to an exception type or a low-cardinality failure code, without recording exception messages. Append spans also carry `cratis.chronicle.append.outcome`: `success`, `error`, `concurrency_violation`, or `constraint_violation`.

Chronicle traces the following activity sources:

| Source | Description |
| --- | --- |
| `Cratis.Chronicle` | Chronicle internal operations |
| `Microsoft.Orleans.Runtime` | Orleans runtime activities (opt-in) |
| `Microsoft.Orleans.Application` | Orleans grain-call activities (opt-in) |
| HTTP client | Outgoing HTTP requests |
| ASP.NET Core | Incoming HTTP and unary gRPC requests, excluding health checks and connection keep-alive |
| gRPC client | Outgoing gRPC calls (opt-in) |

### Trace volume defaults

Chronicle excludes connection keep-alive requests, the configured health endpoint (including its `/live`
and `/ready` routes), and all client-streaming, server-streaming, or duplex gRPC requests from server
traces. Streaming calls can remain open for the lifetime of a client; tracing them would produce
long-running spans rather than useful delivery traces. Reactor and reducer registration is traced
as a separate root operation, without the untraced stream request as its parent.

Queue enqueue and dispatch operations do not create spans. Accepted, non-empty observer deliveries
produce `cratis.chronicle.observer.handle` spans with `cratis.observer.id`, `cratis.eventstore.name`,
`cratis.eventstore.namespace`, `cratis.eventsequence.id`, `cratis.observer.type`, and the numeric
`cratis.event.count` tags.

Kernel gRPC client instrumentation is disabled by default to reduce trace volume. To opt in, set
`Cratis:Chronicle:Telemetry:GrpcClientTracing` to `true`. This example shows the full configuration
hierarchy used in `appsettings.json`; in `chronicle.json`, use only the inner `telemetry` section:

```json
{
    "cratis": {
        "chronicle": {
            "telemetry": {
                "grpcClientTracing": true
            }
        }
    }
}
```

The equivalent environment variable is `Cratis__Chronicle__Telemetry__GrpcClientTracing=true`.
HTTP client tracing remains enabled. These exclusions do not disable metrics or change OTLP
exporter, endpoint, or resource configuration.

### Orleans trace volume

Chronicle's telemetry setup (`AddChronicleTelemetry`) leaves Orleans runtime and grain-call spans
out of exports by default to reduce trace volume. Chronicle spans remain enabled, and Orleans
`AddActivityPropagation()` keeps trace context flowing across grain calls without exporting the
Orleans activities that carry it. Chronicle spans in the called grain keep the caller's trace id,
but their direct parent is an unexported Orleans span, so a trace viewer may show a missing
intermediate parent. Calls outside an existing trace do not create carrier activities.

To include Orleans spans, set `Cratis:Chronicle:Telemetry:OrleansTracing=true`. As above, this is the
full configuration hierarchy; omit the `cratis` and `chronicle` wrappers in `chronicle.json`:

```json
{
    "cratis": {
        "chronicle": {
            "telemetry": {
                "orleansTracing": true
            }
        }
    }
}
```

The equivalent environment variable is `Cratis__Chronicle__Telemetry__OrleansTracing=true`.
`OTEL_TRACES_SAMPLER` and `OTEL_TRACES_SAMPLER_ARG` retain their OpenTelemetry behavior; Chronicle
sets no sampler override. Exporter, endpoint, resource, and metric configuration are unchanged.

The default applies only to `AddChronicleTelemetry`. The public `TracerProviderBuilder.AddChronicleInstrumentation()`
extension is unchanged and still adds both Orleans sources; pass `false` to its `bool` overload to leave them out.
