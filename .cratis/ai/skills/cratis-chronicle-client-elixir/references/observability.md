<!-- cratis-ai-managed: skills/cratis-chronicle-client-elixir/references/observability.md -->
<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

# Observability: OpenTelemetry and `:telemetry`

Source: `Cratis/Chronicle.Elixir`: `Source/chronicle/lib/chronicle/diagnostics/tracing.ex`, `connections/trace_propagation_interceptor.ex`, and `Documentation/observability.md`.

Requires `cratis_chronicle` 3.13.0.

The client is always instrumented and depends only on `opentelemetry_api`. With no OpenTelemetry SDK started in the release, every span and propagation call is a no-op and nothing leaves the process; there is no Chronicle-specific enable switch. To export, add `opentelemetry` and `opentelemetry_exporter` to the application and configure them the standard way (`OTEL_*` variables or `config :opentelemetry`).

All spans use the instrumentation scope `Cratis.Chronicle.Client`, shared with the other Chronicle clients.

| Span | Kind | Where |
| --- | --- | --- |
| `cratis.chronicle.client.event_sequence.append` | Client | `append/3`, `append_with_result/3` |
| `cratis.chronicle.client.event_sequence.append_many` | Client | `append_many/3` variants, `append_many_for_event_sources/2`, unit-of-work commit |
| `cratis.chronicle.client.reactor.handle` | Consumer | a reactor delivery |
| `cratis.chronicle.client.reducer.reduce` | Consumer | a reducer delivery |

Attributes: `cratis.event_store.name`, `cratis.event_store.namespace`, `cratis.event_sequence.id`, `cratis.event_type.id` (append spans), `cratis.event.count` (batches), `cratis.observer.id`, `cratis.correlation_id`. **`cratis.event_source.id` is off by default** because an event source id can be sensitive; enable it with `config :cratis_chronicle, trace_event_source_id: true`.

A span is marked error when the operation returns `{:error, _}`, and a raised exception is recorded before re-raising. The status description is a fixed classification; messages, reasons and stacktraces are never put on a span because they can carry payloads.

## `:telemetry` events

For metrics built on `:telemetry`, the same operations emit spans of `:start`, `:stop` and `:exception` events:

- `[:cratis, :chronicle, :client, :append, ...]`
- `[:cratis, :chronicle, :client, :reactor, :handle, ...]`
- `[:cratis, :chronicle, :client, :reducer, :reduce, ...]`

The `:stop` metadata carries `error: true` when the operation returned `{:error, _}`. Observer lag is a separate event, `[:chronicle, :observer, :lag]`.

## Trace propagation

Every outbound gRPC call carries W3C `traceparent`/`tracestate` for the span active in the **calling process**, injected by `Chronicle.Connections.TracePropagationInterceptor` (always installed). A trace continues into the kernel only when the append or query runs inside the active span (for example inside `OpenTelemetry.Tracer.with_span/2`); a `Task` is a different process. The kernel does not store trace context on events, so an observer's span is not linked to the append that caused it. OpenTelemetry metrics instruments are not emitted, only `:telemetry` events.
