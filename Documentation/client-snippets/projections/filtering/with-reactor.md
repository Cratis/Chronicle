```csharp
using Cratis.Chronicle;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Keys;
using Cratis.Chronicle.Projections.ModelBound;
using Cratis.Chronicle.Reactors;

[EventType]
public record FilteringWithReactorOrderPlaced(string CustomerId, decimal TotalAmount);

// --- Append call ---
// Carries the "premium" tag for orders that qualify
// eventLog.Append(orderId, new FilteringWithReactorOrderPlaced(customerId, total), tags: ["premium"]);

// --- Projection: receives every OrderPlaced ---
[FromEvent<FilteringWithReactorOrderPlaced>]
public record FilteringWithReactorOrderSummary(
    [Key] string CustomerId,
    decimal TotalAmount);

// --- Reactor: receives only premium-tagged OrderPlaced ---
[FilterEventsByTag("premium")]
public class FilteringWithReactorPremiumOrderNotifier : IReactor
{
    public Task Placed(FilteringWithReactorOrderPlaced @event, EventContext context) =>
        Task.CompletedTask;
}
```
