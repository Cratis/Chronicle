<!-- cratis-ai-managed: skills/cratis-chronicle-client-elixir/references/append-api.md -->
<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

# Append API

Verified at `Cratis/Chronicle.Elixir v3.10.2`; paths below are under `Source/chronicle/lib/chronicle/`.

## Append functions and readers

| Function on `Chronicle.EventSequences.EventLog` | Arguments / result | Line in `event_log.ex` |
| --- | --- | --- |
| `append/3` | `(event_source_id, event, opts)` → `:ok \| {:error, reason}` | `:126` |
| `append_many/3` | `(event_source_id, events, opts)` → `:ok \| {:error, reason}` | `:147` |
| `append_many_for_event_sources/2` | `(EventForEventSourceId entries, opts)` → `:ok \| {:error, reason}` | `:184` |
| `append_and_wait_for_completion/3` | `(event_source_id, event, opts)` → `{:ok, %{success: _, failed_partitions: _}} \| {:error, reason}` | `:264` |
| `get_for_event_source/2` | `(event_source_id, opts)` | `:402` |
| `get_from_sequence_number/2` | `(sequence_number, opts)` | `:445` |
| `get_tail_sequence_number/2` | `(event_source_id_or_nil, opts)` | `:484` |
| `get_next_sequence_number/2` | `(event_source_id_or_nil, opts)` | `:502` |
| `get_tail_sequence_number_for_observer/2` | `(observer_module, opts)` | `:523` |
| `complete_stream/3` | `(event_stream_type, event_stream_id, opts)` | `:548` |
| `redact/3` | `(sequence_number, reason, opts)` | `:587` |
| `redact_for_event_source/4` | `(event_source_id, reason, event_types, opts)` | `:622` |
| `has_events_for?/2` | `(event_source_id, opts)` | `:658` |

Read operations observe state; completion and redaction mutate it and require authority for the specific target. `append_many` variants are atomic batches. The cross-source variant uses entries for per-event routing/metadata and accepts batch `:client`, `:namespace`, `:event_sequence_id` and source/stream defaults.

## Options

Append options (`event_log.ex:79-113`): `:client`, `:namespace`, `:event_sequence_id` (default `"event-log"`), `:event_source_type`, `:event_stream_type`, `:event_stream_id`, `:event_source`, `:event_stream`, `:tags`, `:subject`, `:correlation_id`, `:identity`, `:causation`, `:concurrency_scope`, `:occurred`.

Omitted, `nil`, or empty routing types/ids travel as empty strings; the **kernel**, not client-side defaults, resolves them to `"Default"` / `"All"` / `"Default"`. Nonempty values are exact. Concurrency filters remain independent of routing.

Read [event-sources.md](event-sources.md) for definitions, stream routing and derived guards. Definition concurrency applies only without an explicit scope. Incompatible derived guards for one id fail with `{:error, {:incompatible_concurrency_scopes, id, guards}}` before sending.

## Errors and completion

`append_response.ex:65-83` returns `{:error, {:constraint_violations, violations}}`, `{:error, {:concurrency_violations, violations}}` or `{:error, {:append_errors, errors}}` on rejection. Channel/preflight errors include `{:error, :not_connected}` and `{:error, {:incompatible_server, response}}`. A transport timeout does not prove that no event was stored.

`append_and_wait_for_completion/3` has **no many-event variant**. `{:ok, result}` does not imply every observer succeeded: inspect `result.success` and `failed_partitions`. Its wait happens **after** the append (`event_log.ex:267-275`), so `{:error, reason}` can follow a stored event if waiting fails. Do not retry the write blindly; reconcile the stored outcome first.
