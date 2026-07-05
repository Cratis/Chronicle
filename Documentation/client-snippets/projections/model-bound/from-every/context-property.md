```csharp title="Update an audit timestamp from every event"
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Keys;
using Cratis.Chronicle.Projections.ModelBound;

[EventType]
public record InventoryProductRegisteredForEvery(string ProductName);

[EventType]
public record InventoryItemsAdjustedForEvery(int Quantity);

[FromEvent<InventoryProductRegisteredForEvery>]
[FromEvent<InventoryItemsAdjustedForEvery>]
public record InventoryStatusFromEvery(
    [Key] Guid Id,
    string ProductName,
    [FromEvery(contextProperty: nameof(EventContext.Occurred))]
    DateTimeOffset LastUpdated);
```
