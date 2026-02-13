# C# usage

Use the .NET client to define constraints by implementing `IConstraint`. Constraints are discovered automatically and registered with the Chronicle Kernel.

## Defining constraints

```csharp
using Cratis.Chronicle.Events.Constraints;

public class UniqueEventConstraint : IConstraint
{
    public void Define(IConstraintBuilder builder)
    {
        builder.Unique<MyEventType>(
            message: "Only one instance of MyEventType is allowed per event source.");
    }
}
```

## Unique constraints across events

You can enforce uniqueness across multiple event types with a shared property:

```csharp
using Cratis.Chronicle.Events.Constraints;

public class UniqueUserConstraint : IConstraint
{
    public void Define(IConstraintBuilder builder)
    {
        builder.Unique(unique => unique
            .On<UserAdded>(@event => @event.UserName)
            .On<UserNameChanged>(@event => @event.NewUserName)
            .RemovedWith<UserRemoved>());
    }
}
```

## How constraints are enforced

When a constraint is registered, the Chronicle Kernel creates the indexes required to enforce it. Constraints are evaluated server-side during append, ensuring data integrity regardless of the client used.

## Notes

- Constraints are enforced by the Chronicle Kernel, not by client code.
- Keep constraint logic focused on data integrity rules.

