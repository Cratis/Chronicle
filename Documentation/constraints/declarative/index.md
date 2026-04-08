# Declarative constraints

The declarative API lets you define constraints in a dedicated class by implementing `IConstraint`. Chronicle discovers all `IConstraint` implementations automatically — no registration is needed.

Use declarative constraints when you need:

- Uniqueness spanning multiple event types with different property names
- A callback to compose dynamic violation messages
- More explicit control over constraint names

## Constraint types

| Type | Description |
|---|---|
| [Unique property](unique.md) | Enforce that a property value is unique across one or more event types |
| [Unique event type](unique-event-type.md) | Enforce that only one event of a specific type can be appended per event source |

## Defining a constraint

Implement `IConstraint` and call the builder in `Define`:

```csharp
using Cratis.Chronicle.Events.Constraints;

public class UniqueProjectName : IConstraint
{
    public void Define(IConstraintBuilder builder) =>
        builder.Unique(unique =>
            unique
                .On<ProjectCreated>(e => e.Name)
                .RemovedWith<ProjectRemoved>());
}
```

## Discovery

Chronicle scans all loaded assemblies for types that implement `IConstraint` and registers them with the Kernel automatically when the client starts. No explicit registration is required.
