# Constraints

Constraints define server-side rules that must be satisfied before events are committed. They run inside the Chronicle Kernel and protect data integrity across event streams and event sources.

Use constraints to enforce rules like uniqueness and cross-event validation. Because constraints are evaluated in the kernel, they are consistent and apply to every client.

## Why constraints matter

- Enforce invariants regardless of client behavior
- Provide consistent validation across all event sources
- Keep integrity logic close to the event store

## Choosing an approach

Chronicle provides two ways to define uniqueness constraints:

| Approach | When to use |
|---|---|
| [`[Unique]` attribute](attribute.md) | Declaring uniqueness directly on the event type — no separate class needed. **Preferred for most scenarios.** |
| [Declarative (`IConstraint`)](declarative.md) | Complex rules spanning multiple event types, or when a `RemovedWith` event must release the constraint. |

## Topics

- [`[Unique]` attribute](attribute.md)
- [Declarative (`IConstraint`)](declarative.md)

