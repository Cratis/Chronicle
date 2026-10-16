<!-- cratis-ai-managed: skills/cratis-chronicle-client-elixir/references/projections.md -->
<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

# Projection options and keys

Source: `Cratis/Chronicle.Elixir`: `Source/chronicle/lib/chronicle/read_models/read_model.ex`, `projections/projection.ex`, and `registration/coordinator.ex`.

`use Chronicle.ReadModels.ReadModel` accepts `id:`, `passive: true`, `not_rewindable: true`, and `event_sequence: "sequence-name"`. Defaults are the last module segment as id, active, rewindable, and `"event-log"`. Standalone `Chronicle.Projections.Projection` also accepts sequence/rewindability options and requires `model:`.

AutoMap maps matching event/model properties by default. `no_auto_map()` disables it for the whole projection; `no_auto_map([:balance, :status])` excludes only those properties so explicit mappings can own them.

Multi-word atom references are encoded as camelCase correctly: `set: [owner_name: :owner_name]` is the supported shape. AutoMap also handles shared multi-word names; boolean/float constants work without a string workaround. `join/2` requires `:on`. `:count` increments the named field once per occurrence.

## Composite keys and event context

Requires `cratis_chronicle` 3.9.0.

This module excerpt assumes a registered `MyApp.Events.AccountOpened` event. The key names and part order are preserved:

```elixir
# Copyright (c) Cratis. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.

defmodule MyApp.ReadModels.WeeklyAccountOpenings do
  use Chronicle.ReadModels.ReadModel
  defstruct transaction_count: 0

  from(MyApp.Events.AccountOpened,
    key: {:composite, [
      {"Year", {:event_context, "Occurred.Year"}},
      {"Week", {:event_context, "Occurred.Week"}}
    ]},
    count: :transaction_count
  )
end
```

Composite parts accept event-field atoms, explicit expression strings and literals. They must form a nonempty ordered list of `{name, expression}` pairs; nested composites are not supported. Event-context paths contain letters, dots and parentheses and are passed unchanged. Keys also apply to `join` and `removed_with`.

**`Occurred.Year` is the calendar year, not the ISO week-year.** Do not assume that pairing it with `Occurred.Week` gives an ISO week identity at a year boundary; settle the required business key first.

## Dictionary counters

Requires `cratis_chronicle` 3.1.0.

On a read model with `defstruct event_counts: %{}`, this excerpt increments one entry per event type:

```elixir
from_every(increment: [event_counts: {:event_context, :type}])
```

`decrement:` supports the corresponding dictionary operation. Use typed defaults for numeric and boolean fields on every model: schemas are generated from those defaults, not static Elixir types.

## Child collections

Requires `cratis_chronicle` 3.13.0 for the one-line form, `removed_with_join/2`, `clear_with/2` and the compile-time checks below; `children/2,3` itself requires 3.11.0.

`children/2,3` declares a list on the parent that grows, changes and shrinks with events. It works in `use Chronicle.ReadModels.ReadModel` and `use Chronicle.Projections.Projection`; declare the list field's element type with `types: [items: {:list, MyApp.ReadModels.BasketItem}]`.

```elixir
children :items, identified_by: :item_id do
  from MyApp.Events.ItemAdded,
    key: :item_id,
    parent_key: :event_source_id,
    set: [item_id: :item_id, name: :name]

  removed_with MyApp.Events.ItemRemoved, key: :item_id, parent_key: :event_source_id
end
```

A collection that only grows from one event needs no block: `children :entries, from: MyApp.Events.EntryAdded, identified_by: :entry_id, key: :entry_id, set: [entry_id: :entry_id, name: :name]`.

- Inside the block `from/2`, `join/2`, `removed_with/2`, `removed_with_join/2`, `clear_with/2`, `from_every/1` and a nested `children/3` take the options they take on the read model. `key` selects the child an event applies to; `parent_key` selects its parent.
- A `join` in a children block defaults `:on` to `identified_by`; an explicit disagreeing `:on` compiles with a warning. A root `join/2` still requires `:on`.
- `removed_with_join event, key: :archived_entry_id` removes a child when an event from a **different** event source matches it by property instead of the collection key.
- `clear_with event, [:field]` (or `clear: [:field]` in a mapping) sets a scalar back to no value. The field must default to `nil`; a field defaulting to `""` is not nullable to the kernel, so clearing writes the type default.
- **Give nested child collections globally unique identifiers (UUIDs).** In a nested `children/3`, `parent_key` names a child of the collection one level up, and the kernel finds it across every instance of the read model; two instances that both hold `"e-1"` can receive each other's nested events.
- Compile-time errors cover a `children` without `:identified_by`, a root `join/2` without `:on`, one target property assigned twice by one event, and a `from`/`join` target or `children` name that is not a struct field.

A read model document is limited to 4 MiB; give a child collection that can grow without bound its own read model.
