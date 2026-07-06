```csharp title="Fluent FromAll mapping"
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Keys;
using Cratis.Chronicle.Projections;

[EventType]
public record InventoryRegisteredFromAll(string ProductName);

[EventType]
public record InventoryAdjustedFromAll(int Quantity);

public record InventoryStatusFromAll(
    [property: Key] Guid Id,
    string ProductName,
    DateTimeOffset LastUpdated);

public class InventoryStatusFromAllProjection : IProjectionFor<InventoryStatusFromAll>
{
    public void Define(IProjectionBuilderFor<InventoryStatusFromAll> builder) => builder
        .From<InventoryRegisteredFromAll>()
        .From<InventoryAdjustedFromAll>()
        .FromAll(_ => _
            .Set(m => m.LastUpdated)
            .ToEventContextProperty(c => c.Occurred));
}
```
