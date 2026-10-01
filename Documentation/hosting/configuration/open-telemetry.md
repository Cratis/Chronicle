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

When an observer starts, Chronicle records a `0` for each of these, so the series exists before the first failure. A backend that exports cumulative values only sees that `0` if it exports before the first failure. If the failure comes first, the first value it sees is already `1`, so alerts on these counters should not rely on the `0` being there. The [alerting guide](../alerting-on-observer-failures.md) shows how.

To be alerted when an observer's partitions keep failing or run out of retries, see [Get alerted when observers stop processing](../alerting-on-observer-failures.md).

## Traces instrumented

Chronicle traces the following activity sources:

| Source | Description |
| --- | --- |
| `Cratis.Chronicle` | Chronicle internal operations |
| `Microsoft.Orleans.Runtime` | Orleans runtime activities |
| `Microsoft.Orleans.Application` | Orleans application activities |
| HTTP client | Outgoing HTTP requests |
| ASP.NET Core | Incoming HTTP and gRPC requests |
| gRPC client | Outgoing gRPC calls |
