```csharp title="Use the read model property name by convention"
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Keys;
using Cratis.Chronicle.Projections.ModelBound;

[EventType]
public record ProductRenamedForEveryConvention(string Name, int Version);

[EventType]
public record ProductPriceChangedForEveryConvention(decimal Price, int Version);

[FromEvent<ProductRenamedForEveryConvention>]
[FromEvent<ProductPriceChangedForEveryConvention>]
public record ProductVersionFromEveryConvention(
    [Key] Guid Id,
    string Name,
    decimal Price,
    [FromEvery]
    int Version);
```
