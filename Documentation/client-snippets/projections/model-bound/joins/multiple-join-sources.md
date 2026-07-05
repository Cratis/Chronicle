```csharp
[EventType]
public record MbJoinsSourcesLineItemAdded(Guid ProductId);

[EventType]
public record MbJoinsSourcesProductCatalogUpdated(string Name, string Description);

[EventType]
public record MbJoinsSourcesPricingUpdated(decimal CurrentPrice);

public record MbJoinsSourcesOrder(
    [Key]
    Guid OrderId,

    [ChildrenFrom<MbJoinsSourcesLineItemAdded>(key: nameof(MbJoinsSourcesLineItemAdded.ProductId))]
    IEnumerable<MbJoinsSourcesOrderLine> Lines);

// Keyed by product id, so both joins below resolve implicitly through the child's own key.
public record MbJoinsSourcesOrderLine(
    [Key] Guid ProductId,

    [Join<MbJoinsSourcesProductCatalogUpdated>(eventPropertyName: nameof(MbJoinsSourcesProductCatalogUpdated.Name))]
    string ProductName,

    [Join<MbJoinsSourcesProductCatalogUpdated>(eventPropertyName: nameof(MbJoinsSourcesProductCatalogUpdated.Description))]
    string Description,

    [Join<MbJoinsSourcesPricingUpdated>(eventPropertyName: nameof(MbJoinsSourcesPricingUpdated.CurrentPrice))]
    decimal UnitPrice);
```
