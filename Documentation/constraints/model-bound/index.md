# Model-bound constraints

Model-bound constraints let you declare uniqueness rules directly on event types using attributes. No separate constraint class is needed — adorn the event type or its properties with attributes, and Chronicle registers the constraints automatically when the client starts.

> [!NOTE]
> Elixir has a real equivalent: `unique(fields, opts)`, `unique_event_type(opts)`, and `remove_constraint(name)` macros declared directly on the event type module — it supports custom constraint names and grouping by name, but not a custom violation message. Kotlin, Java, and TypeScript have no attribute/macro-based constraint declaration at all — only the [declarative](../declarative/) `IConstraint` class style, and even that is narrower than C#'s (see its own coverage note).

Use model-bound constraints when:

- The uniqueness rule applies to a single event type
- You want constraint metadata co-located with the event type it protects
- You want the simplest possible syntax

## Constraint types

| Type | Description |
|---|---|
| [Unique property](unique) | Enforce that a property value is unique across one or more event types |
| [Unique event type](unique-event-type) | Enforce that only one event of a specific type can be appended per event source |

## Discovery

Chronicle scans all loaded assemblies for types adorned with `[Unique]` and `[RemoveConstraint]` and registers the constraints with the Kernel automatically when the client starts. No explicit registration is required.
