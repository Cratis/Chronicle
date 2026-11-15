<!-- cratis-ai-managed: skills/cratis-chronicle-client-elixir/references/decision-reads.md -->
<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

# Decision reads and the dynamic consistency boundary

Requires Chronicle.Elixir 3.14.2 for the rejection/cleanup behavior below; check `mix deps`, or upgrade before relying on a rejected callback to discard buffered events.

A `Chronicle.decide/2` callback returning `{:error, reason}` rejects all buffered
appends and returns that tuple unchanged.
Migrate matches on `{:ok, {:error, _}}` to `{:error, _}`. A failed commit rolls
back the unit of work, and both `decide/2` and `with_unit_of_work/2` stop their
unit-of-work process on return, so later appends from long-lived GenServers or
Phoenix connections are not trapped in a failed buffer. Spec the rejection and
failed-commit paths, not only successful decisions.

Source: `Cratis/Chronicle.Elixir`: `Source/chronicle/lib/chronicle/read_models/decision_reads.ex`, `decision_read_refused.ex`, `decision_conflict.ex`, `transactions/unit_of_work.ex`, and `Documentation/dynamic-consistency-boundary.md`.

Requires `cratis_chronicle` 3.13.0.

Use a decision read when an append depends on state that a concurrent append could change (a seat can be booked once, a course has capacity). It folds a read model at a key in a fresh session and remembers the exact log position it stopped at; the commit is then rejected by the kernel if a matching event arrived since. An ordinary `Chronicle.read_model/3` followed by `append/3` is **not** a guard: it can silently act on stale state.

## Decide

```elixir
result =
  Chronicle.decide(fn _unit_of_work ->
    {:ok, read} = Chronicle.ReadModels.DecisionReads.get(MyApp.ReadModels.Seat, "seat-5")

    if read.instance == nil do
      Chronicle.EventSequences.EventLog.append("seat-5", %MyApp.Events.SeatBooked{by: "alice"})
    end
  end)

case result do
  {:ok, _} -> :booked
  {:error, %Chronicle.ReadModels.DecisionConflict{}} -> :retry
  {:error, reason} -> {:error, reason}
end
```

- `Chronicle.decide/2` runs the function in a unit of work and commits on return. It returns `{:ok, result}`, or `{:error, %DecisionConflict{}}` when an enrolled decision read conflicted with a concurrent append; nothing from that attempt was written, so re-read and decide again. Other commit failures are plain `{:error, reason}`.
- Unlike `with_unit_of_work/2`, `decide/2` never raises on a conflict. A concurrency violation without an enrolled decision read stays `{:error, {:concurrency_violations, _}}`.
- **`DecisionReads.get/2` enrolls the read only inside a unit of work** (`decide/2` or `UnitOfWork.with_unit_of_work/2`). Outside one it returns `{:error, :requires_unit_of_work}`.
- `DecisionReads.get_detached/2` folds the same way and enrolls nothing; use it for a read you do not intend to guard.
- A rule across two entities needs one decision read per event source. Enroll both inside the same `decide/2` and append one event to each; the kernel accepts or rejects the whole batch.
- `get/4` and `get_detached/4` take `(model, event_source_id, event_stream_type, event_stream_id)` to guard one named stream. The default stream (`""` or `"All"`) is refused with `:all_streams_not_supported`.

## Admission

`DecisionReads.admit/2` and `admit_for_stream/2` check a model's shape without any I/O and return `{:ok, event_types}` or `{:error, reason}`. `get` and `get_detached` **raise** `Chronicle.ReadModels.DecisionReadRefused` for a refused shape, because that is a programming error. Refusal reasons: `:reducer` (an active reducer), `:join`, `:hierarchy` (`children/2`), `:not_event_log`, `:open_ended_event_types` (`from_every/1`), `:no_event_types`, `:not_event_source_keyed`, `:unsupported_event_type_id`, `:all_streams_not_supported`, `:stream_scope_not_supported`, `:fold_incomplete`.

A model with `join/2`, `removed_with_join/2`, `children/2` or `from_every/1` cannot be guarded. A **passive** reducer (`use Chronicle.Reducers.Reducer, model: M, active: false`) can back a decision read because it is folded fresh on the client; an active reducer cannot.

## Explicit concurrency scopes

When a plain guard on the tail is enough, use `Chronicle.Events.ConcurrencyScope` (`:concurrency_scope` append option) instead of a read model.

| Need | Builder |
| --- | --- |
| Guard at the current tail in one call | `{:ok, scope} = ConcurrencyScope.at_tail("account-1", event_stream_type: "Transactions")`; narrowing options: `:event_stream_type`, `:event_stream_id`, `:event_source_type`, `:event_types` |
| "This can only happen once" | `ConcurrencyScope.expecting_no_matching_event(event_source_id: true, event_types: [MyApp.Events.UserRegistered])` |
| Build by hand | `12 \|> ConcurrencyScope.new() \|> with_event_source() \|> with_event_stream_type("Transactions") \|> with_event_type(Mod)`; also `with_sequence_number/2`, `with_event_stream_id/2`, `with_event_source_type/2`, `with_event_types/2` |
| No guard | `ConcurrencyScope.none()` |

**Leaving the expected sequence number unset turns the check off.** It does not mean "expect no event"; use `expecting_no_matching_event/1`, which sends the kernel's explicit condition (requires 3.11.0).

## Default optimistic strategy

```elixir
{Chronicle.Client, ..., concurrency: [default: :optimistic, check_first_append: false]}
```

By default an append with no explicit or definition-derived scope sends no guard. With `default: :optimistic` the client reads the tail, narrowed the way the append is routed, and expects nothing newer. `check_first_append: true` also guards the first append into an empty narrowing with `expecting_no_matching_event/1`. An explicit or definition-derived scope always wins.

Two **unequal** explicit scopes for one event source id in one batch or unit of work fail before sending with `{:error, {:duplicate_concurrency_scope, event_source_id}}`; equal scopes collapse to one.
