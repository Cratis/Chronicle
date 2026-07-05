```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Events.Constraints;

[EventType]
public record Chr0020OrderPlaced(string OrderId);

public class Chr0020OrderConstraint : IConstraint
{
    public void Define(IConstraintBuilder builder) =>
        builder.Unique(u => u.On<Chr0020OrderPlaced>(e => e.OrderId.ToLower())); // CHR0020: method call - never executed
}
```
