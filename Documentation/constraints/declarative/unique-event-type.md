# Unique event type constraint

Use `builder.Unique<TEventType>()` inside an `IConstraint` implementation to enforce that **only one event of a specific type** can be appended per event source identifier. Use this when the event itself is a unique fact — for example, initializing a project can only happen once per project.

Chronicle discovers all `IConstraint` implementations automatically — no registration is needed.

## Defining a unique event type constraint

```csharp
using Cratis.Chronicle.Events.Constraints;

public class UniqueProjectInitialization : IConstraint
{
    public void Define(IConstraintBuilder builder) =>
        builder.Unique<ProjectInitialized>();
}
```

## Violation message

Provide a static message string:

```csharp
using Cratis.Chronicle.Events.Constraints;

public class UniqueProjectInitialization : IConstraint
{
    public void Define(IConstraintBuilder builder) =>
        builder.Unique<ProjectInitialized>(
            message: "A project can only be initialized once.");
}
```

Or use a callback to compose the message dynamically from violation context:

```csharp
using Cratis.Chronicle.Events.Constraints;

public class UniqueProjectInitialization : IConstraint
{
    public void Define(IConstraintBuilder builder) =>
        builder.Unique<ProjectInitialized>(
            messageCallback: violation => $"Project '{violation.EventSourceId}' has already been initialized.");
}
```

## Constraint name

An optional name can be provided to identify the constraint. When not provided, Chronicle uses the event type name as the default constraint name:

```csharp
using Cratis.Chronicle.Events.Constraints;

public class UniqueProjectInitialization : IConstraint
{
    public void Define(IConstraintBuilder builder) =>
        builder.Unique<ProjectInitialized>(
            name: "UniqueProjectInitialization",
            message: "A project can only be initialized once.");
}
```

## How constraints are enforced

When a constraint is registered, the Chronicle Kernel creates the indexes required to enforce it. Constraints are evaluated server-side during append, ensuring data integrity regardless of the client.
