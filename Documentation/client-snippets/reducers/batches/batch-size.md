```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Observation;

[EventType]
public record BatchStockReceived(int Quantity);

[EventType]
public record BatchStockShipped(int Quantity);

[BatchSize(MaxEvents = 50, MaxConcurrency = 2)]
public record BatchStockLevel(int Quantity)
{
    [Observes<BatchStockReceived>]
    [Observes<BatchStockShipped>]
    public static BatchStockLevel? Reduce(EventBatch events, BatchStockLevel? current, BatchContext context) =>
        events.Aggregate(current ?? new BatchStockLevel(0), (stock, e) => e.Event switch
        {
            BatchStockReceived received => stock with { Quantity = stock.Quantity + received.Quantity },
            BatchStockShipped shipped => stock with { Quantity = stock.Quantity - shipped.Quantity },
            _ => stock
        });
}
```
