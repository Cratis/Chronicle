```csharp title="Defaults for fields events do not set"
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Projections;

[EventType]
public record InitialValuesStockReceived(int Quantity);

[EventType]
public record InitialValuesStockReserved(int Quantity);

public record InitialValuesInventoryItem(
    int CurrentStock,
    int ReservedStock,
    DateTimeOffset LastUpdated,
    int MinimumLevel,
    int MaximumLevel,
    int ReorderPoint);

public class InitialValuesInventoryProjection : IProjectionFor<InitialValuesInventoryItem>
{
    public void Define(IProjectionBuilderFor<InitialValuesInventoryItem> builder) => builder
        .WithInitialValues(() => new InitialValuesInventoryItem(
            CurrentStock: 0,
            ReservedStock: 0,
            LastUpdated: DateTimeOffset.UnixEpoch,
            MinimumLevel: 10,
            MaximumLevel: 1000,
            ReorderPoint: 20))
        .From<InitialValuesStockReceived>(_ => _
            .Add(m => m.CurrentStock).With(e => e.Quantity)
            .Set(m => m.LastUpdated).ToEventContextProperty(c => c.Occurred))
        .From<InitialValuesStockReserved>(_ => _
            .Add(m => m.ReservedStock).With(e => e.Quantity)
            .Set(m => m.LastUpdated).ToEventContextProperty(c => c.Occurred));
}
```
