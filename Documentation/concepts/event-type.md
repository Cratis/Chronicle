---
description: A Chronicle event type gives every stored event an identity and generations, and clients must declare event types before appending events.
---
# Event Type

Every [event](./event.md) stored in the [event store](./event-store.md) must have
a type associated with it.

Any client connecting to Chronicle needs to communicate the event types before any events
can be appended to an [event sequence](./event-sequence.md).

## Identity survives a code rename only if you preserve it

The default event type id is the class name, not its full namespace-qualified name. Renaming the class changes that id unless you explicitly retain the previous value. Stored history stays under the old id; handlers for the new id do not see those events. Moving the class to another namespace without changing its name does not change its default id.

When refactoring an existing event, preserve its stored id. When changing its schema, use a new generation of that same identity and an [event type migration](./event-type-migrations.mdx), not a new id to bypass the old contract. The [.NET discovery diagnostics](../clients/dotnet/discovery-report.md) warn about likely event type renames based on schema shape, but cannot prove that two types represent the same fact.

## Generations

An event type can contain multiple generations. Every new event starts with generation 1
and any changes to the event should then become a new generation. This allows versioning
your event types so that multiple schema generations can coexist in the same event store.

The concept of generations is important when working with systems that evolve over time.
Each generation registers its own JSON schema, which Chronicle uses to validate and store
events correctly for that generation.

```mermaid
flowchart LR
    ET["Event type: PersonRegistered"] --> G1["Generation 1 + JSON schema"]
    G1 -->|migration| G2["Generation 2 + JSON schema"]
    G2 -->|migration| G3["Generation 3 + JSON schema"]
```

You keep the record for every past generation in your codebase alongside the current one. The
recommended way to declare a previous generation is `[EventTypeGenerationFor<T>]`, where `T` is
the current generation's type — Chronicle resolves the shared event type id from `T`'s own
`[EventType]`, so the previous generation never carries (and can never mistype) an id of its own.

For detailed information on how to define and use migrations — including this attribute, the
older explicit-id style it replaces, and the operations available for transforming events between
generations — see [Event Type Migrations](./event-type-migrations).
