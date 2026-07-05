```csharp title="Product promotion projection"
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Projections;

[EventType]
public record ProductListedWithNestedPromotion(string Name, decimal BasePrice);

[EventType]
public record PromotionAppliedWithNestedPromotion(string Label, int DiscountPercent, DateTimeOffset ValidUntil);

[EventType]
public record PromotionRemovedWithNestedPromotion;

public record ProductWithNestedPromotion(
    string Name,
    decimal BasePrice,
    PromotionForNestedProduct? Promotion);

public record PromotionForNestedProduct(
    string Label,
    int DiscountPercent,
    DateTimeOffset ValidUntil);

public class ProductProjectionWithNestedPromotion : IProjectionFor<ProductWithNestedPromotion>
{
    public void Define(IProjectionBuilderFor<ProductWithNestedPromotion> builder) => builder
        .From<ProductListedWithNestedPromotion>()
        .Nested(m => m.Promotion, promotion => promotion
            .From<PromotionAppliedWithNestedPromotion>()
            .ClearWith<PromotionRemovedWithNestedPromotion>());
}
```
