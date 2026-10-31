<!-- cratis-ai-managed: skills/cratis-chronicle-client-elixir/references/read-models.md -->
<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

# Querying, observing and previewing read models

Source: `Cratis/Chronicle.Elixir`: `Source/chronicle/lib/chronicle/read_models.ex`, `read_models/watchers.ex`, `read_models/reactor.ex`, `projections.ex`, and `Documentation/read-models.md`, `projections.md`.

Requires `cratis_chronicle` 3.13.0 for everything below except `watch/2`.

A read model is eventually consistent: `Chronicle.read_model/3` returns `{:ok, nil}` both for an absent key and for one not projected yet. To read your own write, append with `append_and_wait_for_completion/3` or call `Chronicle.Projections.wait_for/3`.

## Wait for a projection

```elixir
{:ok, %{sequence_number: sequence}} = Chronicle.EventSequences.EventLog.append_with_result(id, event)
:ok = Chronicle.Projections.wait_for(MyApp.ReadModels.Account, sequence)
```

`wait_for/3` polls until the projection has handled that sequence number or returns `{:error, :timeout}`.

## Filter and sort

`Chronicle.ReadModels.query/2` accepts `:filter` (all conditions must match; a value means equality, or `{:gt, v}`, `{:gte, v}`, `{:lt, v}`, `{:lte, v}`, `{:ne, v}`, `{:in, [v]}`, `{:contains, v}`) and `:sort` (`{:amount, :desc}`). An unknown field returns `{:error, {:unknown_field, field}}`.

**The client applies the filter** by paging through the whole read model, because the kernel contract cannot filter. A read model with more than `:max_scan` instances (default `10_000`) returns `{:error, {:scan_limit_exceeded, max_scan}}` rather than a partial answer. For large data, give the question its own projection.

## Live changes: pick by what you need

| Need | Function | Messages the process receives |
| --- | --- | --- |
| Changes to a read model, one stream per caller | `watch/2` / `unwatch/1` | `{:chronicle_read_model_changed, module, %Changeset{}}`, `{:chronicle_read_model_watch_error, module, reason}` (the watch has ended; call again) |
| Same, shared across callers (default) | `subscribe/2` / `unsubscribe/1` | the two above plus `{:chronicle_read_model_watch_ended, module, reason}` and `{:chronicle_read_model_watch_resumed, module}` |
| The current page of instances, re-sent on change | `observe/2` / `unobserve/1`, or `observe_stream/2` as an Elixir `Stream` of `%QueryResult{}` | `{:chronicle_read_model_instances, module, %{instances: _}}`, `{:chronicle_read_model_observe_error, module, reason}` (observation ended) |

`subscribe/2` shares one kernel stream per read model, client, namespace and event sequence and reconnects with backoff (500 ms doubling to 30 s). **Changes made while the stream was down are not replayed**; query again after `:chronicle_read_model_watch_resumed`. `observe/2` accepts `:page_size`, `:filter` and `:sort`; with a filter, `total_count` counts matches within the delivered page only.

A `use Chronicle.ReadModels.Reactor, model: M` module reacts to read model changes through optional `on_added/2`, `on_modified/2` and `on_removed/2`, and is registered with the client's `:read_model_reactors` option. It subscribes through `subscribe/2` and re-subscribes after a reconnect.

## Ad-hoc queries and previews

`Chronicle.Projections.query/2` sends a Projection Declaration Language string to the kernel and returns the projected entries without registering a projection or defining a module; entries are plain maps unless `:read_model` is given. Bad syntax returns `{:error, {:invalid_declaration, [%{message:, line:, column:}]}}`. `Chronicle.Projections.preview/2` runs an **already registered** projection's declaration over the stored event sequence; previewing over events you supply is not possible.
