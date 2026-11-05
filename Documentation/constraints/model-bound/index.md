---
title: Cratis Chronicle model-bound constraints
description: Define Cratis Chronicle constraints on event models to reserve unique values, limit event occurrences, or close a stream after a domain event.
---
Cratis Chronicle model-bound constraints keep an append rule on the event property or type it protects. Use them to reserve an email address across event sources, allow an initialization event once per source, or close a stream when a domain event ends its lifecycle. An event source is the identity whose events you append, such as a user or project.

You do not need a separate constraint class. The .NET client uses attributes, TypeScript uses decorators, and Kotlin and Java use annotations. Elixir uses `unique(fields, opts)`, `unique_event_type(opts)`, and `remove_constraint(name)` on the event type module, with options including `:message` and `:scope`. The uniqueness guides below show each client's syntax. Options differ between clients; a setting shown in one tab does not imply that every client exposes it.

Choose the [declarative](../declarative/) approach when the rule needs configuration beyond your client's model-bound declarations. It also lets you keep a rule that spans several event types in one place.

## Constraint types

| Type | Description |
|---|---|
| [Unique property](unique) | Prevent different event sources from claiming the same property value |
| [Unique event type](unique-event-type) | Allow an event type once per event source until a removal event releases the constraint |
| [Closing stream](closing-stream) | Stop further appends within a closed scope using the .NET attribute workflow |

## Discovery

In .NET, the default artifact discovery finds model-bound constraints on registered event types and registers their definitions with the Kernel when the client starts. This includes `[Unique]`, `[RemoveConstraint]`, and `[ClosesStream]` declarations.

If you supply a custom `IClientArtifactsProvider`, return the relevant event types through `UniqueConstraints`, `UniqueEventTypeConstraints`, `RemoveConstraintEventTypes`, and `ClosesStreamEventTypes`. The provider determines which declarations the client discovers.
