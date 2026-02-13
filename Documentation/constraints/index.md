# Constraints

Constraints define server-side rules that must be satisfied before events are committed. They run inside the Chronicle Kernel and protect data integrity across event streams and event sources.

Use constraints to enforce rules like uniqueness, invariant checks, and cross-event validation. Because constraints are evaluated in the kernel, they are consistent and apply to every client.

## Why constraints matter

- Enforce invariants regardless of client behavior
- Provide consistent validation across all event sources
- Keep integrity logic close to the event store

## Topics

- [C# usage](dotnet-client.md)

