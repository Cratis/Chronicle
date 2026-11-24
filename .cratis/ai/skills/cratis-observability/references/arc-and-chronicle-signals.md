<!-- cratis-ai-managed: skills/cratis-observability/references/arc-and-chronicle-signals.md -->
<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

# Arc and Chronicle signals

Sources: Arc `Source/DotNET/Arc.Core/WellKnownDiagnostics.cs`, `WellKnownTelemetryNames.cs`, `Source/DotNET/MongoDB/WellKnownMongoDBDiagnostics.cs`, `Documentation/backend/csharp/observability.md`; Chronicle `Source/Clients/DotNET/Diagnostics/OpenTelemetry/Tracing/ClientActivity.cs`, `ChronicleTelemetryOptions.cs`, `Documentation/hosting/client-tracing.mdx`, `Documentation/hosting/configuration/telemetry-names.md`. Read the release you consume.

## Arc

Activity source and meter: `Cratis.Arc`. MongoDB meter: `Cratis.Arc.MongoDB`.

| Span | Meaning |
| --- | --- |
| `cratis.arc.command.execute` | Whole command execution |
| `cratis.arc.command.validate`, `.filter`, `.authorize`, `.provide`, `.handle` | Pipeline stages |
| `cratis.arc.validator.invoke` | One validator |
| `cratis.arc.query.perform`, `cratis.arc.query.push` | Query execution and observable push |
| `cratis.arc.command.action`, `cratis.arc.query.action` | Controller-based actions (server spans) |

| Attribute | Meaning |
| --- | --- |
| `cratis.arc.command.type`, `cratis.arc.command.outcome`, `cratis.arc.command.key.type` | Command identity and result |
| `cratis.arc.query.name`, `cratis.arc.query.transport`, `cratis.arc.query.outcome` | Query identity, transport and result |
| `cratis.correlation_id` | Business correlation id |
| `cratis.tenant.id` | Tenant (also legacy `cratis.tenant`); span attribute only, never resource or baggage |
| `http.route` | Route template, never a concrete URL |

| Metric | Kind |
| --- | --- |
| `cratis.arc.command.duration` | Histogram, seconds |
| `cratis.arc.command.outcomes` | Counter |
| `cratis.arc.query.duration` | Histogram, seconds |
| `cratis.arc.query.subscriptions` | Up-down counter of open subscriptions |
| `cratis.arc.query.pushes` | Counter |

Arc records at most 1,000 distinct command types or query names per metric and folds the rest into `_other`. A command rejected by validation or authorization does not fail the span; the outcome attribute carries the rejection.

## Chronicle .NET client

Activity source: `Cratis.Chronicle.Client` (`ClientActivity.SourceName`). Registration: `AddCratisOpenTelemetry` covers it through `Cratis.*`. Without it, `AddCratisChronicleInstrumentation()` (package `Cratis.Chronicle.AspNetCore`, namespace `Cratis.Chronicle.AspNetCore.OpenTelemetry`) or `AddSource("Cratis.Chronicle.Client")` is required, in the same tracing configuration as the exporter.

| Legacy span (default) | Convention span (`Telemetry:SpanNames=Convention`) |
| --- | --- |
| `client.event_sequence.append` | `cratis.chronicle.client.event_sequence.append` |
| `client.event_sequence.append_many` | `cratis.chronicle.client.event_sequence.append_many` |
| `client.unit_of_work.commit`, `client.unit_of_work.rollback` | `cratis.chronicle.client.unit_of_work.commit`, `...rollback` |
| `client.reactor.handle` | `cratis.chronicle.client.reactor.handle` |
| `client.reducer.handle` | `cratis.chronicle.client.reducer.handle` |

The option form is `ChronicleTelemetryOptions.SpanNames = ClientSpanNames.Convention`. Failed append and handle operations set Error status and `error.type` without messages. Append spans carry `cratis.chronicle.append.outcome`: `success`, `error`, `concurrency_violation` or `constraint_violation`.

Attributes per span and mode (legacy keys are always set; the shared keys are added only in Convention mode):

| Span | Legacy keys (both modes) | Added in Convention mode |
| --- | --- | --- |
| append | `event_store_name`, `namespace_name`, `event_sequence_id`, `event_source_type`, `event_source_id` (when traced) | `cratis.event_store.name`, `cratis.event_store.namespace`, `cratis.event_sequence.id`, `cratis.event_source.type`, `cratis.event_source.id` (when traced) |
| append_many | `event_store_name`, `namespace_name`, `event_sequence_id` | the three matching shared keys |
| reactor handle | `event_store_name`, `namespace_name`, `event_sequence_id`, `reactor_id` | shared store, namespace and sequence keys, `cratis.observer.id`, `cratis.observer.type=reactor` |
| reducer handle | same with `reducer_id` | same with `cratis.observer.type=reducer` |
| unit of work commit and rollback | `correlation_id` | `cratis.correlation_id` |

So in Legacy mode (the default) no client span carries a shared key, and in either mode only the unit-of-work commit and rollback spans carry a correlation id; append, reactor and reducer spans never do. Correlate as follows: Arc command spans carry `cratis.correlation_id` and the client append sits in the same trace, so join them by trace id; the commit or rollback span carries the id under `correlation_id` (Legacy) or `cratis.correlation_id` (Convention); observer spans carry no correlation id, so identify them by observer id and store, namespace and sequence, not by correlation.

Kernel spans call the event source id `cratis.eventsource.id`; client spans use `event_source_id`. Both follow their own exposure setting.

## Naming modes

Legacy names stay the default. Legacy keys are deprecated for removal in the next major version, so build new dashboards on convention names where the mode is available, and use `Both` only as a temporary migration mode.
