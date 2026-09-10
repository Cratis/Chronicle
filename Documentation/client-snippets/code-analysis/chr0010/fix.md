```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Projections.ModelBound;

[EventType("customer-registered")]
[EventStore("customers")]
public record Chr0010FixCustomerRegistered(string Name);

[EventType("customer-spent")]
[EventStore("customers")]
public record Chr0010FixCustomerSpent(decimal Amount);

// All event types belong to the same event store: "customers".
[FromEvent<Chr0010FixCustomerRegistered>]
[AddFrom<Chr0010FixCustomerSpent>]
public record Chr0010FixCustomerSummary(string Name, decimal TotalSpent);
```
