```csharp title="Declarative projection with every-event metadata"
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Keys;
using Cratis.Chronicle.Projections;

[EventType]
public record InventoryRegisteredDeclarativeForEvery(string ProductName);

[EventType]
public record InventoryAdjustedDeclarativeForEvery(int Quantity);

public record InventoryStatusDeclarativeFromEvery(
    [property: Key] Guid Id,
    string ProductName,
    DateTimeOffset LastUpdated);

public class InventoryStatusDeclarativeProjection : IProjectionFor<InventoryStatusDeclarativeFromEvery>
{
    public void Define(IProjectionBuilderFor<InventoryStatusDeclarativeFromEvery> builder) => builder
        .From<InventoryRegisteredDeclarativeForEvery>()
        .From<InventoryAdjustedDeclarativeForEvery>()
        .FromEvery(_ => _
            .Set(m => m.LastUpdated)
            .ToEventContextProperty(c => c.Occurred));
}
```
