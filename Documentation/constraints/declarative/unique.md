# Unique property constraint

Use `builder.Unique(...)` inside an `IConstraint` implementation to enforce that a property value is unique across one or more event types. The constraint fires if any tracked event would introduce a duplicate value.

Chronicle discovers all `IConstraint` implementations automatically — no registration is needed.

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

## Uniqueness across multiple event types

Use multiple `.On` calls when several event types each contribute to the same logical uniqueness rule. The constraint fires if any of the tracked events would introduce a duplicate value:

```csharp
using Cratis.Chronicle.Events.Constraints;

public class UniqueEmail : IConstraint
{
    public void Define(IConstraintBuilder builder) =>
        builder.Unique(unique =>
            unique
                .WithName("UniqueEmail")
                .On<UserRegistered>(e => e.Email)
                .On<UserEmailChanged>(e => e.NewEmail)
                .RemovedWith<UserRemoved>());
}
```

## Constraint name

Use `.WithName(...)` to give the constraint an explicit name. When not provided, Chronicle uses the class name of the `IConstraint` implementation:

```csharp
public class UniqueEmail : IConstraint
{
    public void Define(IConstraintBuilder builder) =>
        builder.Unique(unique =>
            unique
                .WithName("UniqueEmail")
                .On<UserRegistered>(e => e.Email));
}
```

## Releasing a constraint

Call `.RemovedWith<T>()` to register the event type that releases the constraint. When that event is appended, the previously held value is freed and can be claimed again:

```csharp
public class UniqueOrderReference : IConstraint
{
    public void Define(IConstraintBuilder builder) =>
        builder.Unique(unique =>
            unique
                .On<OrderPlaced>(e => e.Reference)
                .RemovedWith<OrderCancelled>());
}
```

## Ignoring casing

Call `.IgnoreCasing()` to make the uniqueness check case-insensitive:

```csharp
public class UniqueEmail : IConstraint
{
    public void Define(IConstraintBuilder builder) =>
        builder.Unique(unique =>
            unique
                .On<UserRegistered>(e => e.Email)
                .IgnoreCasing());
}
```

## Violation message

Call `.WithMessage(...)` to provide a custom message when the constraint is violated:

```csharp
public class UniqueProjectName : IConstraint
{
    public void Define(IConstraintBuilder builder) =>
        builder.Unique(unique =>
            unique
                .On<ProjectCreated>(e => e.Name)
                .WithMessage("A project with this name already exists."));
}
```

Use a callback to compose the message dynamically from violation context:

```csharp
public class UniqueProjectName : IConstraint
{
    public void Define(IConstraintBuilder builder) =>
        builder.Unique(unique =>
            unique
                .On<ProjectCreated>(e => e.Name)
                .WithMessage(violation => $"A project named '{violation.Value}' already exists."));
}
```

## How constraints are enforced

When a constraint is registered, the Chronicle Kernel creates the indexes required to enforce it. Constraints are evaluated server-side during append, ensuring data integrity regardless of the client.
