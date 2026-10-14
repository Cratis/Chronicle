```csharp
using Cratis.Chronicle;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Projections.ModelBound;
using Cratis.Chronicle.ReadModels;

[EventType]
public record ProductDefined(decimal Price);

[EventType]
public record ProductPriceChanged(decimal OldPrice, decimal NewPrice);

public record OrderedItem(string ProductId, decimal DisplayedPrice, int Quantity);

[EventType]
public record ProductsOrdered(IReadOnlyList<OrderedItem> Items);

// The decision model for one product: its current price, the one before and when it changed.
// A reduced variant: a price older than the previous one is never accepted, even inside the grace period.
[FromEvent<ProductDefined>]
public record ProductPrice(
    string Id,
    [SetFrom<ProductDefined>(nameof(ProductDefined.Price))]
    [SetFrom<ProductPriceChanged>(nameof(ProductPriceChanged.NewPrice))]
    decimal Current,
    [SetFrom<ProductPriceChanged>(nameof(ProductPriceChanged.OldPrice))]
    decimal Previous,
    [SetFromContext<ProductPriceChanged>(nameof(EventContext.Occurred))]
    DateTimeOffset ChangedAt);

public static class OrderProducts
{
    static readonly TimeSpan GracePeriod = TimeSpan.FromMinutes(10);

    public static async Task<bool> Handle(IEventStore eventStore, EventSourceId orderId, IReadOnlyList<OrderedItem> items, DateTimeOffset now)
    {
        var reads = eventStore.GetDecisionReads();
        var guards = new List<IDecisionRead>();

        // One decision read per product in the cart. The set is dynamic: it is whatever the order contains.
        foreach (var item in items)
        {
            var read = await reads.GetDetached<ProductPrice>(new ReadModelKey(item.ProductId));
            if (read.Instance is not { } price)
            {
                return false;
            }

            var isCurrent = price.Current == item.DisplayedPrice;
            var isRecentPrevious = price.Previous == item.DisplayedPrice && now - price.ChangedAt <= GracePeriod;
            if (!isCurrent && !isRecentPrevious)
            {
                return false;
            }

            guards.Add(read);
        }

        // One event on the order's own event source. The product keys are only read, never appended to,
        // and a price change after the reads rejects the append.
        var result = await eventStore.EventLog.AppendMany(
            [new EventForEventSourceId(orderId, new ProductsOrdered(items))],
            guardedBy: guards);

        return result.IsSuccess;
    }
}
```
