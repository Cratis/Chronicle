```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Projections;

[EventType]
public record DecDictionaryItemSold(string Category, decimal Amount);

[EventType]
public record DecDictionaryItemReturned(string Category, decimal Amount);

public record DecDictionarySales(
    IDictionary<string, decimal> RevenueByCategory,
    IDictionary<string, int> SalesByCategory,
    IDictionary<string, int> SalesByUser,
    IDictionary<string, DateTimeOffset> FirstSaleByCategory);

public class DecDictionarySalesProjection : IProjectionFor<DecDictionarySales>
{
    public void Define(IProjectionBuilderFor<DecDictionarySales> builder) => builder
        .From<DecDictionaryItemSold>(_ => _
            .Add(m => m.RevenueByCategory, e => e.Category).With(e => e.Amount)
            .Increment(m => m.SalesByCategory, e => e.Category)
            .Count(m => m.SalesByUser, c => c.CausedBy.Subject)
            .SetOnce(m => m.FirstSaleByCategory, e => e.Category).ToEventContextProperty(c => c.Occurred))
        .From<DecDictionaryItemReturned>(_ => _
            .Subtract(m => m.RevenueByCategory, e => e.Category).With(e => e.Amount)
            .Decrement(m => m.SalesByCategory, e => e.Category));
}
```
