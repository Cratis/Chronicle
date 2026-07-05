```csharp
using Cratis.Chronicle.Reactors;

// Missing [EventType] attribute
public record Chr0005CustomerRegistered(Guid CustomerId, string Email);

public class Chr0005CustomerReactor : IReactor
{
    // CHR0005: Type 'Chr0005CustomerRegistered' must be marked with [EventType] attribute
    public Task Registered(Chr0005CustomerRegistered @event) => Task.CompletedTask;
}
```
