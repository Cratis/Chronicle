```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reactors;

[EventType]
public record Chr0005CustomerRegisteredFixed(Guid CustomerId, string Email);

public class Chr0005CustomerReactorFixed : IReactor
{
    // Now valid
    public Task Registered(Chr0005CustomerRegisteredFixed @event) => Task.CompletedTask;
}
```
