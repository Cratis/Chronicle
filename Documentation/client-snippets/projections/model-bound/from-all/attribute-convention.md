```csharp title="Convention-based FromAll attribute"
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Keys;
using Cratis.Chronicle.Projections.ModelBound;

[EventType]
public record ProductRenamedFromAllConvention(string Name, int Version);

[EventType]
public record ProductPriceChangedFromAllConvention(decimal Price, int Version);

[FromEvent<ProductRenamedFromAllConvention>]
[FromEvent<ProductPriceChangedFromAllConvention>]
public record ProductVersionFromAllConvention
{
    [Key]
    public Guid Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public decimal Price { get; init; }

    [FromAll]
    public int Version { get; init; }
}
```
