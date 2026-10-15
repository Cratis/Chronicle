<!-- cratis-ai-managed: skills/cratis-chronicle-client-elixir/references/projections.md -->
<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

# Projection options and keys

Verified at `Cratis/Chronicle.Elixir v3.10.2`: `Source/chronicle/lib/chronicle/read_models/read_model.ex:35-120,183-212,230-332`, `projections/projection.ex`, and `registration/coordinator.ex:735-746`.

`use Chronicle.ReadModels.ReadModel` accepts `id:`, `passive: true`, `not_rewindable: true`, and `event_sequence: "sequence-name"`. Defaults are the last module segment as id, active, rewindable, and `"event-log"`. Standalone `Chronicle.Projections.Projection` also accepts sequence/rewindability options and requires `model:`.

AutoMap maps matching event/model properties by default. `no_auto_map()` disables it for the whole projection; `no_auto_map([:balance, :status])` excludes only those properties so explicit mappings can own them.

Since 3.5.1, multi-word atom references are encoded as camelCase correctly: `set: [owner_name: :owner_name]` is the supported shape. AutoMap also handles shared multi-word names; boolean/float constants work without a string workaround. `join/2` requires `:on`. `:count` increments the named field once per occurrence.

## Composite keys and event context (since 3.9.0)

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

## Dictionary counters (since 3.1.0)

On a read model with `defstruct event_counts: %{}`, this excerpt increments one entry per event type:

```elixir
from_every(increment: [event_counts: {:event_context, :type}])
```

`decrement:` supports the corresponding dictionary operation. Use typed defaults for numeric and boolean fields on every model: schemas are generated from those defaults, not static Elixir types.
