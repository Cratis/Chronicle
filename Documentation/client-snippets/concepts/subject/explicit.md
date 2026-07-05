```csharp
using Cratis.Chronicle;
using Cratis.Chronicle.Events;

[EventType]
public record SubjectShippingAddressChanged(string Street);

public class SubjectShippingService(IEventStore eventStore)
{
    public Task ChangeAddress(EventSourceId orderId, Subject customerId, string street) =>
        eventStore.EventLog.Append(
            eventSourceId: orderId,
            @event: new SubjectShippingAddressChanged(street),
            subject: customerId);
}
```
