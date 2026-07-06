```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reducers;

public record Chr0006ItemAdded(string ProductId);

public record Chr0006ItemRemoved(string ProductId);

public class Chr0006ShoppingCart
{
    public int ItemCount { get; set; }
}

public class Chr0006InvalidShoppingCartReducer : IReducerFor<Chr0006ShoppingCart>
{
    // CHR0006: Invalid signature - returns Task<int> instead of Task
    public async Task<int> ItemAdded(Chr0006ItemAdded @event)
    {
        await Task.CompletedTask;
        return 1;
    }

    // CHR0006: Invalid signature - too many parameters
    public Task ItemRemoved(Chr0006ItemRemoved @event, EventContext context, bool validate) =>
        Task.CompletedTask;
}
```
