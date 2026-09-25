```csharp
using System.Text.Json.Nodes;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;

public interface IOrdersBridgeForwarder
{
    Task Forward(EventType eventType, JsonObject content, IReadOnlyDictionary<int, string> generations, CancellationToken cancellationToken);
}

public class OrdersBridgeRegistration
{
    public async Task Register(IEventStore eventStore, IOrdersBridgeForwarder forwarder)
    {
        var handler = await eventStore.Reactors.Register(
            "orders-bridge",
            reactor => reactor
                .WithEventType(new EventType("OrderPlaced", 1))
                .WithEventType(new EventType("OrderPlaced", 2))
                .OnEventSequence(EventSequenceId.Log)
                .NotReplayable(),
            async (@event, cancellationToken) =>
            {
                // Content is JsonObject; generational content contains raw JSON.
                await forwarder.Forward(@event.Context.EventType, @event.Content, @event.GenerationalContent, cancellationToken);
            });

        var state = await handler.GetState();
        var failedPartitions = await handler.GetFailedPartitions();
        eventStore.Reactors.Unregister("orders-bridge");
    }
}
```
