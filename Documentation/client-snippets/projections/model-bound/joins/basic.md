```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Keys;
using Cratis.Chronicle.Projections.ModelBound;

[EventType]
public record MbJoinsOrderPlaced(Guid CustomerId, decimal Amount);

[EventType]
public record MbJoinsCustomerCreated(string Name);

public record MbJoinsOrderSummary(
    [Key]
    Guid OrderId,

    [SetFrom<MbJoinsOrderPlaced>]
    decimal Amount,

    [SetFrom<MbJoinsOrderPlaced>]
    Guid CustomerId,

    [Join<MbJoinsCustomerCreated>(
        on: nameof(CustomerId),
        eventPropertyName: nameof(MbJoinsCustomerCreated.Name))]
    string CustomerName);
```
