```csharp
using Cratis.Chronicle;
using Cratis.Chronicle.Compliance.GDPR;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reducers;

[PII]
public record ReleasingPiiRequesterName(string Value) : ConceptAs<string>(Value)
{
    public static readonly ReleasingPiiRequesterName NotSet = new(string.Empty);
    public static implicit operator string(ReleasingPiiRequesterName name) => name.Value;
    public static implicit operator ReleasingPiiRequesterName(string value) => new(value);
}

[EventType]
public record ReleasingPiiSupportTicketOpened(string CustomerId, ReleasingPiiRequesterName RequesterName);

public record ReleasingPiiSupportTicket(string Id, [Subject] string CustomerId, [PII] string RequesterName);

public class ReleasingPiiSupportTicketReducer : IReducerFor<ReleasingPiiSupportTicket>
{
    public ReleasingPiiSupportTicket Opened(ReleasingPiiSupportTicketOpened @event, ReleasingPiiSupportTicket? current, EventContext context) =>
        new(context.EventSourceId.Value, @event.CustomerId, @event.RequesterName);
}
```
