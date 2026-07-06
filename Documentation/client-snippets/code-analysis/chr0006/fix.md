```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reducers;

public record Chr0006ItemAddedFixed(string ProductId);

public record Chr0006ItemRemovedFixed(string ProductId);

public record Chr0006CartCleared;

public class Chr0006ShoppingCartFixed
{
    public int ItemCount { get; set; }
}

public class Chr0006ValidShoppingCartReducer : IReducerFor<Chr0006ShoppingCartFixed>
{
    // Valid signature
    public Task ItemAdded(Chr0006ItemAddedFixed @event) => Task.CompletedTask;

    // Valid signature with context
    public Task ItemRemoved(Chr0006ItemRemovedFixed @event, EventContext context) => Task.CompletedTask;

    // Valid synchronous signature
    public void CartCleared(Chr0006CartCleared @event)
    {
    }
}
```
