<!-- cratis-ai-managed: skills/cratis-chronicle-client-elixir/references/event-sources.md -->
<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

# Event sources and streams

Source: `Cratis/Chronicle.Elixir`: `Source/chronicle/lib/chronicle/event_sources/event_source.ex`, `event_sources/routing.ex`, `event_sequences/definition_routing.ex`, and `Documentation/event-sources.md`.

Requires `cratis_chronicle` 3.10.0 and Chronicle kernel 19.30.0 for definitions.

An event source definition gives appends a stable name, optional streams and default concurrency dimensions. It belongs to the append, not the event type; the same event type can go through different definitions. Appends without a definition keep their existing routing and have no registered-source metadata.

```elixir
# Copyright (c) Cratis. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.

defmodule MyApp.AccountEventSource do
  use Chronicle.EventSources.EventSource,
    name: "Account",
    description: "A bank account",
    concurrency: [:event_source_id]

  stream("Transactions",
    description: "Money movements",
    concurrency: [:event_source_id, :event_stream_id]
  )
end
```

`:name` defaults to the last module segment without a trailing `EventSource`. Dimensions are `:event_source_id`, `:event_source_type`, `:event_stream_type`, `:event_stream_id`. A stream with no dimensions inherits its source's dimensions. Duplicate stream names fail compilation; duplicate source names raise `Chronicle.EventSources.DuplicateEventSourceName` at client startup.

## Register and append

Set `otp_app:` so discovery finds the definition, or pass `event_sources: [MyApp.AccountEventSource]` to `Chronicle.Client`. Definitions register after event types and before observers on every reconnect. Registration is an upsert; omitting a definition later does not delete it from the kernel.

With the client registered and the `MyApp.Events.FundsDeposited` event from the skill declared:

```elixir
case Chronicle.append("account-1", %MyApp.Events.FundsDeposited{amount: 50},
       event_source: MyApp.AccountEventSource,
       event_stream: "Transactions",
       event_stream_id: "2024-05"
     ) do
  :ok -> :ok
  {:error, reason} -> {:error, reason}
end
```

The source name becomes the event source type and event `EventSource` metadata; the stream name becomes the event stream type. Sources can be modules or names. Without `:event_stream`, the stream type is `"All"`. `append_many/3` and units of work accept the same source/stream options.

Each `Chronicle.EventSequences.EventForEventSourceId` can carry `event_source` and `event_stream`. In `append_many_for_event_sources/2`, entry-level routing wins; batch defaults fill only entries with no source. A routing rejection prevents the whole batch from being sent.

| Reason inside `{:error, reason}` | Cause |
| --- | --- |
| `{:unknown_event_source, source}` | Source not registered with this client |
| `{:event_stream_does_not_belong_to_event_source, source, stream}` | Stream not declared by that source |
| `{:event_routing_contradicts_event_source, source, dimension, expected, actual}` | Explicit source/stream type contradicts the definition |
| `{:event_stream_without_event_source, stream}` | Stream given without a source |
| `{:incompatible_concurrency_scopes, id, guards}` | Derived guards disagree for one event source id in a batch |

## Default concurrency is not an explicit decision guard

Without an explicit `:concurrency_scope`, the client reads the tail narrowed by the effective definition dimensions and uses that sequence number as a scope. With no dimensions, it sends no scope. Explicit scopes, including `Chronicle.Events.ConcurrencyScope.none()`, win and avoid that read. Do not disable a guard just to silence a rejection; choose a scope that protects the business decision's reads.

The wire carries one scope per event source id. Guarded entries of that id must agree on dimensions and selected source type, stream type and stream id. Unguarded entries never suppress a later guarded entry. Incompatible derived guards fail before any tail read or append; use separate batches, or deliberately supply one explicit scope on an entry for that id.

Reactor and reducer contexts expose `:event_source` as the registered name, or `nil` for events appended without a definition. Only reactor contexts carry `:delivery_id`.
