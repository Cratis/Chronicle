```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Projections.ModelBound;

[EventType("customer-registered")]
[EventStore("customers")]
public record Chr0010ViolationCustomerRegistered(string Name);

[EventType("order-placed")]
[EventStore("orders")]
public record Chr0010ViolationOrderPlaced(decimal Amount);

// Error CHR0010: Projection 'Chr0010ViolationCustomerSummary' references event types from
// multiple event stores: "customers", "orders". All event types in a projection must
// originate from the same event store.
[FromEvent<Chr0010ViolationCustomerRegistered>]
[SetFrom<Chr0010ViolationOrderPlaced>]
public record Chr0010ViolationCustomerSummary(string Name, decimal TotalSpent);
```
