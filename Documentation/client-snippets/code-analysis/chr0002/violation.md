```csharp
using Cratis.Chronicle.Projections;

// Missing [EventType] attribute
public record Chr0002OrderCreated(Guid OrderId, decimal Amount);

public class Chr0002OrderReadModel
{
    public Guid Id { get; set; }
    public decimal Amount { get; set; }
}

public class Chr0002OrderProjection : IProjectionFor<Chr0002OrderReadModel>
{
    public void Define(IProjectionBuilderFor<Chr0002OrderReadModel> builder) =>
        // CHR0002: Type 'Chr0002OrderCreated' must be marked with [EventType] attribute
        builder.From<Chr0002OrderCreated>(_ => _
            .Set(m => m.Id).To(e => e.OrderId)
            .Set(m => m.Amount).To(e => e.Amount));
}
```
