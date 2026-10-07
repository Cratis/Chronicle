# Open Telemetry

Chronicle exports telemetry signals through the [OpenTelemetry Protocol (OTLP)](https://opentelemetry.io/docs/specs/otlp/). Telemetry is always enabled and configured via standard OpenTelemetry environment variables.

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
| `OTEL_EXPORTER_OTLP_ENDPOINT` | none | OTLP receiver endpoint (e.g. `http://localhost:4317`) |
| `OTEL_EXPORTER_OTLP_PROTOCOL` | `grpc` | Export protocol: `grpc` or `http/protobuf` |
| `OTEL_EXPORTER_OTLP_HEADERS` | none | Additional headers (e.g. API keys for cloud backends) |
| `OTEL_SERVICE_NAME` | `Chronicle` | Service name reported to the telemetry backend |

When `OTEL_EXPORTER_OTLP_ENDPOINT` is not set, Chronicle does not export telemetry but the instrumentation is still active.

## Local development with .NET Aspire Dashboard

The Kernel ships with a `docker-compose.yml` that starts the [.NET Aspire Dashboard](https://learn.microsoft.com/en-us/dotnet/aspire/fundamentals/dashboard/overview) as a lightweight all-in-one OTLP receiver and telemetry viewer.

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

Start the services:

```bash
docker compose up -d
```

Then set the endpoint environment variable before starting the Kernel:

```bash
OTEL_EXPORTER_OTLP_ENDPOINT=http://localhost:4317 dotnet run
```

Open the Aspire Dashboard at [http://localhost:18888](http://localhost:18888) to view metrics, traces, and logs.

## Example: Prometheus and Grafana

For production-grade metric collection, connect Chronicle to a Prometheus-compatible OTLP receiver (for example the [OpenTelemetry Collector](https://opentelemetry.io/docs/collector/)):

```bash
OTEL_EXPORTER_OTLP_ENDPOINT=http://otel-collector:4317
```

## Example: cloud observability backends

Most cloud observability backends (Datadog, Honeycomb, Grafana Cloud, Azure Monitor) accept OTLP. Set the endpoint and any required authentication headers:

```bash
OTEL_EXPORTER_OTLP_ENDPOINT=https://otlp.example.com
OTEL_EXPORTER_OTLP_HEADERS=x-api-key=your-api-key
```

## Metrics instrumented

Chronicle instruments the following meters:

| Meter | Description |
| --- | --- |
| `Cratis.Chronicle` | Chronicle application metrics (events appended, observers, etc.) |
| `Microsoft.Orleans` | Orleans distributed actor framework metrics |
| `Grpc.AspNetCore.Server` | gRPC server request metrics |
| .NET runtime | GC, thread pool, and memory metrics from the .NET runtime |

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

### Open alert incident metrics

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

Chronicle traces the following activity sources:

| Source | Description |
| --- | --- |
| `Cratis.Chronicle` | Chronicle internal operations |
| `Microsoft.Orleans.Runtime` | Orleans runtime activities |
| `Microsoft.Orleans.Application` | Orleans application activities |
| HTTP client | Outgoing HTTP requests |
| ASP.NET Core | Incoming HTTP and gRPC requests |
| gRPC client | Outgoing gRPC calls |
