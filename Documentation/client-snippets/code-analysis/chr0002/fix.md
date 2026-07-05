```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Projections;

[EventType]
public record Chr0002OrderCreatedFixed(Guid OrderId, decimal Amount);

public class Chr0002OrderReadModelFixed
{
    public Guid Id { get; set; }
    public decimal Amount { get; set; }
}

public class Chr0002OrderProjectionFixed : IProjectionFor<Chr0002OrderReadModelFixed>
{
    public void Define(IProjectionBuilderFor<Chr0002OrderReadModelFixed> builder) =>
        // Now valid
        builder.From<Chr0002OrderCreatedFixed>(_ => _
            .Set(m => m.Id).To(e => e.OrderId)
            .Set(m => m.Amount).To(e => e.Amount));
}
```
