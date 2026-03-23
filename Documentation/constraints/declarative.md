# Declarative constraints

The declarative API lets you define uniqueness constraints in a dedicated class by implementing `IConstraint`. Use this approach when you need finer control: uniqueness that spans multiple event types with different property names, or a `RemovedWith` event that releases a held constraint.

For most single-event uniqueness scenarios, the [`[Unique]` attribute](attribute.md) is simpler and preferred.

## Defining a constraint

Implement `IConstraint` and call the builder in `Define`. Chronicle discovers all `IConstraint` implementations automatically — no registration is needed.

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

Use multiple `.On` calls when several event types each contribute to the same logical uniqueness rule. The constraint fires if any of the tracked events would introduce a duplicate value.

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

## Releasing a constraint

Call `.RemovedWith<T>()` to register the event type that releases the constraint. When that event is appended, the previously held value is freed and can be claimed again.

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

## How constraints are enforced

When a constraint is registered, the Chronicle Kernel creates the indexes required to enforce it. Constraints are evaluated server-side during append, ensuring data integrity regardless of the client.

## When to prefer the attribute

Reach for `IConstraint` only when:

- The uniqueness rule spans event types with **different property names** that cannot share a constraint name via `[Unique(name: "...")]`.

In all other cases, use the [`[Unique]`](attribute.md) and [`[RemoveConstraint]`](attribute.md#releasing-a-constraint-with-removeconstraint) attributes — they require no additional file and keep constraint definitions co-located with the event types they protect.
