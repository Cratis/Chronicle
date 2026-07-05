```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Events.Constraints;

[EventType]
public record ConstraintsUniqueOrderPlaced(string Reference);

[EventType]
public record ConstraintsUniqueOrderCancelled;

public class ConstraintsUniqueOrderReference : IConstraint
{
    public void Define(IConstraintBuilder builder) =>
        builder.Unique(unique =>
            unique
                .On<ConstraintsUniqueOrderPlaced>(e => e.Reference)
                .RemovedWith<ConstraintsUniqueOrderCancelled>());
}
```
