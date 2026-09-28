```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reactors;

public interface IAuditTrail
{
    Task Record(EventSourceId eventSourceId, object @event);
}

public class AuditReactorRegistration
{
    public async Task<IReactorHandler> Register(IEventStore eventStore, IAuditTrail auditTrail)
    {
        var definition = eventStore.Reactors.Define(
            "audit-trail",
            reactor => reactor.Subscribe((@event, context) => auditTrail.Record(context.EventSourceId, @event)));

        // The definition is inspectable before it is registered.
        Console.WriteLine($"{definition.Id} subscribes to all events: {definition.SubscribesToAllEvents}");
        Console.WriteLine($"Event types: {string.Join(", ", definition.EventTypes.Select(_ => _.Id))}");

        return await eventStore.Reactors.Register(definition);
    }
}
```
