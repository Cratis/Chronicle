<!-- cratis-ai-managed: skills/cratis-chronicle-client-elixir/references/constraints.md -->
<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

# Constraints

Source: `Cratis/Chronicle.Elixir`: `Source/chronicle/lib/chronicle/events/event_type.ex`, `events/constraint.ex`, `events/constraint_violation.ex`, and `Documentation/constraints.md`.

Requires `cratis_chronicle` 3.13.0 for standalone constraint modules, `remove_constraint/2`, `:scope` and `ConstraintViolation.details/1`.

A constraint is checked by the kernel at append time; a violation returns `{:error, {:constraint_violations, violations}}` (or `{:error, %AppendResult{}}` from `append_with_result/3`).

## On the event type

`unique/2`, `unique_event_type/1`, `remove_constraint/1,2` and `closes_stream/1` are imported by `use Chronicle.Events.EventType`.

| Option | Meaning |
| --- | --- |
| `:name` | Constraint name; defaults to the first field (`unique`) or the event type id (`unique_event_type`) |
| `:mode` | `:per_event_source` (default) keeps one value per event source and frees the old one when replaced; `:per_value` keeps every value until a removal event releases it |
| `:ignore_casing` | Case-insensitive comparison (`unique` only) |
| `:message` | A string or a `{Module, :function}` callback invoked with the violation |
| `:scope` | `:per_event_source_type`, `:per_event_stream_type`, `:per_event_stream_id`, or a list; narrows uniqueness to values seen along those dimensions. Unscoped is global |
| `:event_sequences` | Restrict to specific event sequences |

An unknown option or invalid `:scope` compiles with a warning naming the module and is ignored; an invalid `:mode` raises `ArgumentError` when the definition is built.

## Releasing a held value

In `:per_value` mode, `remove_constraint("Name")` releases every value the removal event's event source held. `remove_constraint("Name", properties: [:version_id])` releases only the value the removal event carries, in the order of the constrained properties.

## Standalone constraint modules

Use `use Chronicle.Events.Constraint` when one name spans several event types or lives apart from any one event type's module.

```elixir
# Copyright (c) Cratis. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.

defmodule MyApp.Constraints.UniqueEmail do
  use Chronicle.Events.Constraint

  unique name: "UniqueEmail" do
    on MyApp.Events.UserRegistered, [:email]
    on MyApp.Events.UserEmailChanged, [:new_email]
    removed_with MyApp.Events.UserRemoved
  end
end
```

- `name:` is **required** on a standalone declaration.
- `removed_with/1,2` inside `unique/2` releases the value (with `properties:` in `:per_value` mode). After a bare `unique_event_type/1,2` it releases that constraint; calling it before any `unique_event_type` is a compile error.
- Two `unique_event_type` declarations with the same `:name` merge into one mutually exclusive constraint: at most one event of the named set per event source (`unique_event_type MyApp.Events.PersonErased, name: "PersonTerminal"`).
- Modules are discovered like event types, or listed in the client's `:constraints` option (and in `start_supervised_client/1` when testing).

## Violation details

`Chronicle.Events.ConstraintViolation.details(violation)` maps the kernel's detail keys to atoms: `:property_name`, `:property_value`, `:closed_by`, `:closed_at_sequence_number`; any other key stays a string. A PII property's `:property_value` is its **encrypted** form. Use it in a `:message` callback: `"A project named '#{ConstraintViolation.details(violation)[:property_value]}' already exists."`.

## Closing events

`closes_stream event_stream_id_from: :period, reopened_by: [MyApp.Events.PeriodReopened]` on an event type closes that stream scope when the event is appended; later events in the scope are refused. Options: `:dimensions`, `:event_stream_id_from`, `:reopened_by`, `:name`, `:event_sequences`; same-named declarations merge and must agree on dimensions. Registration closes nothing in history. Inspect closed scopes with `Chronicle.EventSequences.Streams`. The in-memory test kernel does not model `closes_stream`.

Constraint drift is not detected: the kernel cannot list registered constraints.
