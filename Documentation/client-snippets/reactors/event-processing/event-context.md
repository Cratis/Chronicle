```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reactors;

[EventType]
public record ReactorAccountClosed(Guid AccountId);

public class AuditReactor : IReactor
{
    public void AccountClosed(ReactorAccountClosed @event, EventContext context)
    {
        WriteAudit(@event.AccountId, context.Occurred, context.EventSourceId);
    }

    void WriteAudit(Guid accountId, DateTimeOffset occurred, EventSourceId eventSourceId) { }
}
```
