<!-- cratis-ai-managed: skills/cratis-chronicle-client-elixir/references/append-api.md -->
<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

# Append API

Source: `Cratis/Chronicle.Elixir`; paths below are under `Source/chronicle/lib/chronicle/`.

## Append functions and readers

| Function on `Chronicle.EventSequences.EventLog` | Arguments / result | Line in `event_log.ex` |
| --- | --- | --- |
| `append/3` | `(event_source_id, event, opts)` → `:ok \| {:error, reason}` | |
| `append_many/3` | `(event_source_id, events, opts)` → `:ok \| {:error, reason}` | |
| `append_many_for_event_sources/2` | `(EventForEventSourceId entries, opts)` → `:ok \| {:error, reason}` | |
| `append_and_wait_for_completion/3` | `(event_source_id, event, opts)` → `{:ok, %{success: _, failed_partitions: _}} \| {:error, reason}` | |
| `get_for_event_source/2` | `(event_source_id, opts)` | |
| `get_from_sequence_number/2` | `(sequence_number, opts)` | |
| `get_tail_sequence_number/2` | `(event_source_id_or_nil, opts)` | |
| `get_next_sequence_number/2` | `(event_source_id_or_nil, opts)` | |
| `get_tail_sequence_number_for_observer/2` | `(observer_module, opts)` | |
| `complete_stream/3` | `(event_stream_type, event_stream_id, opts)` | |
| `redact/3` | `(sequence_number, reason, opts)` | |
| `redact_for_event_source/4` | `(event_source_id, reason, event_types, opts)` | |
| `has_events_for?/2` | `(event_source_id, opts)` | |

Read operations observe state; completion and redaction mutate it and require authority for the specific target. `append_many` variants are atomic batches. The cross-source variant uses entries for per-event routing/metadata and accepts batch `:client`, `:namespace`, `:event_sequence_id` and source/stream defaults.

## Options

Append options (`event_log.ex`): `:client`, `:namespace`, `:event_sequence_id` (default `"event-log"`), `:event_source_type`, `:event_stream_type`, `:event_stream_id`, `:event_source`, `:event_stream`, `:tags`, `:subject`, `:correlation_id`, `:identity`, `:causation`, `:concurrency_scope`, `:occurred`.

Omitted, `nil`, or empty routing types/ids travel as empty strings; the **kernel**, not client-side defaults, resolves them to `"Default"` / `"All"` / `"Default"`. Nonempty values are exact. Concurrency filters remain independent of routing.

Read [event-sources.md](event-sources.md) for definitions, stream routing and derived guards. Definition concurrency applies only without an explicit scope. Incompatible derived guards for one id fail with `{:error, {:incompatible_concurrency_scopes, id, guards}}` before sending.

## Errors and completion

`append_response.ex` returns `{:error, {:constraint_violations, violations}}`, `{:error, {:concurrency_violations, violations}}` or `{:error, {:append_errors, errors}}` on rejection. Channel/preflight errors include `{:error, :not_connected}` and `{:error, {:incompatible_server, response}}`. A transport timeout does not prove that no event was stored.

`append_and_wait_for_completion/3` has **no many-event variant**. `{:ok, result}` does not imply every observer succeeded: inspect `result.success` and `failed_partitions`. Its wait happens **after** the append (`event_log.ex`), so `{:error, reason}` can follow a stored event if waiting fails. Do not retry the write blindly; reconcile the stored outcome first.

## Structured results, prepared events and unknown types

Requires `cratis_chronicle` 3.13.0 for everything in this section.

- `append_with_result/3` and `append_many_with_result/3` return `{:ok, %AppendResult{sequence_number: _}}` (batch: every assigned number, in order) or `{:error, %AppendResult{}}` carrying `:constraint_violations` (each with `:constraint_name`, `:message`, `:event_type_id`, `:sequence_number`, `:details`), `:concurrency_violations` (`:event_source_id`, `:expected_sequence_number`, `:actual_sequence_number`) and `:errors`. Use them when you need the assigned sequence number or the structured reason; transport failures stay plain `{:error, reason}`. `AppendResult.wait_for_completion/2` waits for observers.
- `EventLog.prepare/2` serializes an event once into a `PreparedEvent` bound to the client, namespace and event sequence that prepared it; reuse it with `EventLog.append_prepared/3` and `Streams.verify_content/3`. Appending it through another client, namespace or sequence returns `{:error, :prepared_event_belongs_to_another_sequence}`. Single use is not enforced.
- **A struct that does not `use Chronicle.Events.EventType` returns `{:error, {:unknown_event_type, module}}`** from every append path, `prepare/2` and `Streams.verify_content/3`; it does not raise. Match it like any other error.
- A payload over 4 MiB returns `{:error, {:payload_too_large, size, limit}}` before sending. Raise or disable the guard with `config :cratis_chronicle, max_payload_bytes: ...` (or `:infinity`) only when the kernel is configured for larger messages.
- Reads: `get_for_event_source/3` and `get_from_sequence_number/2` return raw wire events; pass `decode: true` for `Chronicle.Events.AppendedEvent` structs (`event: nil` and `:raw` for an unknown type, `redacted?: true` for a redacted event).
