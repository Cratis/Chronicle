```csharp
using Cratis.Chronicle.Commands;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reactors;

public class Chr0030OrderProcessor(ICommandPipeline commandPipeline) : IReactor
{
    // Warning CHR0030: OrderPlaced invokes ICommandPipeline.Execute but is not marked
    // [OnceOnly]; a replay (redaction, revision, observer rewind) runs the handler again
    // and re-executes the command, duplicating the side effect. Mark the method [OnceOnly].
    public Task OrderPlaced(Chr0030OrderPlaced @event, EventContext context) =>
        commandPipeline.Execute(new Chr0030ShipOrder(@event.OrderNumber));
}

[EventType]
public record Chr0030OrderPlaced(string OrderNumber);

[Command]
public record Chr0030ShipOrder(string OrderNumber)
{
    public void Handle() { }
}
```
