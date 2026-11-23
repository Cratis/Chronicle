<!-- cratis-ai-managed: skills/cratis-observability/references/chronicle-kernel-export.md -->
<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

# Chronicle kernel export

Sources: Chronicle `Source/Kernel/Core/Diagnostics/OpenTelemetry/OpenTelemetryConfigurationExtensions.cs`, `Source/Kernel/Core/Configuration/Telemetry.cs`, `Documentation/hosting/configuration/open-telemetry.md`. The kernel is configured separately from the application: application telemetry setup does not export kernel signals. Read the release you run.

## Export

The kernel's own setup (`AddChronicleTelemetry`) adds an OTLP exporter per signal only when `OTEL_EXPORTER_OTLP_ENDPOINT` or the per-signal endpoint is set. The kernel host calls it without a service name, so the resource `service.name` is always `Chronicle`: the setup applies it after the SDK defaults, so `OTEL_SERVICE_NAME` does not change it (the product docs list that variable; the source does not honour it). Tell kernel telemetry apart from the application's by that name, and confirm in your backend. The kernel subscribes the activity source `Cratis.Chronicle`, the meters `Cratis.Chronicle` and `Cratis.Chronicle.Federation`, and the `Microsoft.Orleans` and `Grpc.AspNetCore.Server` meters.

## Options under `Telemetry`

Set through configuration or the environment form `Cratis__Chronicle__Telemetry__<Option>` (see **cratis-chronicle-configuration** `references/environment-variables.md` for the prefix rule).

| Option | Default | Effect |
| --- | --- | --- |
| `MetricNames` | `Legacy` | `Convention` publishes `cratis.chronicle.*` names; `Both` publishes both sets temporarily and raises volume |
| `OrleansTracing` | `false` | Orleans runtime and grain-call spans; very high volume |
| `GrpcClientTracing` | `false` | Outbound gRPC client spans; opt in only when needed |
| `MetricSeriesLimit` | `500` | Series per instrument before folding to `otel.metric.overflow=true` |
| `EventSourceIdTracing` | `Hashed` | `Raw`, `Hashed` or `Omitted` for `cratis.eventsource.id` |
| `EventSourceIdHashKey` | generated per process | At least 32 characters when `Hashed` is chosen explicitly; share with clients to correlate |

## Observer failure metrics

Counters on the `Cratis.Chronicle` meter (legacy names): `chronicle-observer-partitions-failed`, `chronicle-observer-partition-retry-attempts`, `chronicle-observer-partitions-quarantined`, `chronicle-observer-quarantined`. Each carries only `EventStore`, `Namespace`, `ObserverId` and `EventSequenceId`, never a partition or event source id. A healthy observer has no series until its first failure, so alert on increase and never depend on seeing a `0` first. Prometheus names depend on backend translation: check the exposed name before writing a query. The product's alerting guide (`Documentation/hosting/alerting-on-observer-failures.md`) has rules.

## Local viewer

The Aspire dashboard image `mcr.microsoft.com/dotnet/aspire-dashboard` serves its UI on 18888 and receives OTLP/gRPC on container port 18889, published as 4317. Run it as a service in a local compose file (Chronicle's `Source/Kernel/Server/docker-compose.yml` has one) or with the container runtime, mapping host 18888 to 18888 and host 4317 to container 18889, and pin the image tag you trust. Its anonymous-access setting is for local machines only; never publish its ports.

```shell
OTEL_EXPORTER_OTLP_ENDPOINT=http://localhost:4317 dotnet run
```
