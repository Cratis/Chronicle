```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reactors;

[EventType]
public record Chr0004FixedOrderUpdated;

public record Chr0004Order(int Count);

public class ValidOrderReactor : IReactor
{
    public Task OrderUpdated(Chr0004FixedOrderUpdated @event, Chr0004Order order) =>
        Task.CompletedTask;
}
```
