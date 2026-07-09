```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reactors;

public class Chr0031OrderCounter : IReactor
{
    // Warning CHR0031: Reactor declares mutable state '_count'; reactors are re-created and
    // replayed by Chronicle, so instance state is unreliable and leaks context between
    // invocations. Use readonly, primary-constructor-injected dependencies.
    int _count;

    public Task OrderPlaced(Chr0031OrderPlaced @event, EventContext context)
    {
        _count++;
        return Task.CompletedTask;
    }
}

[EventType]
public record Chr0031OrderPlaced(string OrderNumber);
```
