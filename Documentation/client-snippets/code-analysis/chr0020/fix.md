```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Events.Constraints;

[EventType]
public record Chr0020OrderPlacedFixed(string OrderId);

public class Chr0020OrderConstraintFixed : IConstraint
{
    public void Define(IConstraintBuilder builder) =>
        builder.Unique(u => u.On<Chr0020OrderPlacedFixed>(e => e.OrderId)); // Now valid - simple member access
}
```
