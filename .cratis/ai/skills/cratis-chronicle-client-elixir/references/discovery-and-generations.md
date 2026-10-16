<!-- cratis-ai-managed: skills/cratis-chronicle-client-elixir/references/discovery-and-generations.md -->
<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

# Discovery, artifact identity and event generations

Source: `Cratis/Chronicle.Elixir`: `Source/chronicle/lib/chronicle/discovery/`, `artifacts.ex`, `client.ex`, `events/event_type.ex`, `events/event_types.ex`, `reactors/reactor.ex`, `reducers/reducer.ex`, `lib/mix/tasks/chronicle.discover.ex`, and `Documentation/discovery.md`, `events.md`, `reactors.md`, `reducers.md`.

Requires `cratis_chronicle` 3.13.0.

## The discovery report

`Chronicle.Client` builds a `%Chronicle.Discovery.Report{}` at start, logs a one-line summary at `:info` and the whole report at `:debug`. Read it with `Chronicle.Client.discovery_report()` and print it with `Chronicle.Discovery.Report.format/1`. `Chronicle.Artifacts.report(:my_app)` builds the same report without starting a client; `Chronicle.Artifacts.discover/1` still returns its plain map.

Run it from the shell with `mix chronicle.discover --otp-app my_app`; add `--connection-string "chronicle://..."` to compare against a kernel (event store `"default"`, namespace `"Default"`) and `--fail-on-drift` to exit `1` on a warning-severity finding. Exit codes: `0` clean, `1` drift found, `2` could not run (no `--otp-app`, or kernel unreachable). `--fail-on-drift` has no effect without `--connection-string`.

## Pin ids that default to a module name

A reactor, reducer or declarative projection with no `:id` defaults it from its module name (reactors and reducers: the full module name; projections: the last segment). **Renaming or moving the module then creates a different artifact and silently resets its observation position.** Pin it:

```elixir
use Chronicle.Reactors.Reactor, id: "notification-reactor"
use Chronicle.Projections.Projection, id: "account-summary", model: MyApp.ReadModels.AccountSummary
```

Discovery logs a warning once per module per BEAM node for a defaulted id. `warn_defaulted_ids: false` on `Chronicle.Client` suppresses it; prefer pinning. In-memory test clients skip it.

## Drift policy

Before registering, the client compares what it would register with the kernel. The `:drift_policy` client option:

| Value | Behavior |
| --- | --- |
| `:warn` (default) | Log findings and register anyway |
| `:fail` | Log findings and refuse to register on a `:warning`-severity finding |
| `:ignore` | Skip the check and its kernel calls |

Findings: `:schema_changed_without_generation` (an event type generation's coarse shape changed: added, removed or retyped field), `:observer_missing_from_code` (always `:info`, never blocks: other clients may own the observer), `:observer_event_types_changed` (a reactor's handled types differ; `:warning`). An indeterminate check (kernel unreachable, exception, 5 s deadline) continues under `:warn` and stops under `:fail`. Read the current findings with `Chronicle.Client.identity_drift()`. The kernel rejects a schema change without a bumped generation itself, regardless of policy.

## Event type generations

A module for an **older** generation uses `generation_for:` instead of its own `:id`, so the ids cannot drift apart:

```elixir
defmodule MyApp.Events.CustomerRegisteredV1 do
  use Chronicle.Events.EventType, generation_for: MyApp.Events.CustomerRegistered, generation: 1
  defstruct customer_id: "", owner_name: ""
end
```

`generation_for:` requires an explicit `generation:` and cannot be combined with `:id`. Bridge the generations with `Chronicle.Events.Migration` (exactly one migrator per pair).

Mistakes such as two modules for one id and generation, two migrators for a pair, or `generation_for:` naming a module that is not an event type are logged and the client continues. Set `strict_validation: true` on `Chronicle.Client` to raise instead; `Chronicle.Events.EventTypes.validate/2` returns `:ok | {:error, exception}` and `validate!/2` raises.

## Pinned generation delivery

By default (`:compatibility`) every event is upgraded to the generation of the module a reactor or reducer handles. For a handler written against an **older** generation, `use Chronicle.Reactors.Reactor, generation_delivery: :pinned` (or the same on a reducer) delivers the event as appended. `context.appended_generation` reports the appended generation (`0` means unknown). The client-wide default is `event_generation_delivery: :pinned`; an artifact's own option wins. A pinned observer that handles two generations of one event type raises `Chronicle.Observers.MultipleGenerationsOfEventTypeInObserver` on start.
