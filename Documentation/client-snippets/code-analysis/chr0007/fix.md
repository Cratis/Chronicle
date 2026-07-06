```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reducers;

[EventType]
public record Chr0007InventoryAdjustedFixed(string ProductId, int Quantity);

public class Chr0007InventoryStateFixed
{
    public int Quantity { get; set; }
}

public class Chr0007InventoryReducerFixed : IReducerFor<Chr0007InventoryStateFixed>
{
    // Now valid
    public void Adjusted(Chr0007InventoryAdjustedFixed @event)
    {
    }
}
```
