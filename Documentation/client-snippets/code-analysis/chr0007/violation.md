```csharp
using Cratis.Chronicle.Reducers;

// Missing [EventType] attribute
public record Chr0007InventoryAdjusted(string ProductId, int Quantity);

public class Chr0007InventoryState
{
    public int Quantity { get; set; }
}

public class Chr0007InventoryReducer : IReducerFor<Chr0007InventoryState>
{
    // CHR0007: Type 'Chr0007InventoryAdjusted' must be marked with [EventType] attribute
    public void Adjusted(Chr0007InventoryAdjusted @event)
    {
    }
}
```
