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
