# Constraints

Constraints define server-side rules that must be satisfied before events are committed. They run inside the Chronicle Kernel and protect data integrity across event streams and event sources.

Use constraints to enforce rules like uniqueness across event types and event sources. Because constraints are evaluated in the kernel, they are consistent and apply to every client.

## Why constraints matter

- Enforce invariants regardless of client behavior
- Provide consistent validation across all event sources
- Keep integrity logic close to the event store

## Constraint types

Chronicle supports two types of uniqueness constraints:

| Type | Description |
|---|---|
| **Unique property** | Enforces that a property value is unique across all events of one or more types |
| **Unique event type** | Enforces that only one event of a specific type can ever be appended per event source |

## Defining constraints

Chronicle provides two ways to define constraints:

| Approach | When to use |
|---|---|
| [Model-bound](model-bound/index.md) | Declaring constraints directly on event types using attributes — no separate class needed. **Preferred for most scenarios.** |
| [Declarative](declarative/index.md) | Complex rules spanning multiple event types with different property names, or when you need a callback for constraint violation messages. |

## Topics

- [Declarative](declarative/toc.yml)
- [Model-bound](model-bound/toc.yml)

