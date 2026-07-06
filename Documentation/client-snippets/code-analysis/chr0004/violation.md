```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reactors;

[EventType]
public record Chr0004OrderUpdated;

public class InvalidOrderReactor : IReactor
{
    public Task OrderUpdated(Chr0004OrderUpdated @event, int count) =>
        Task.CompletedTask;
}
```
