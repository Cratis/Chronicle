```csharp
using Cratis.Chronicle;

public class ReleasingPiiSupportTicketService(IEventStore eventStore)
{
    public Task<ReleasingPiiSupportTicket> Release(ReleasingPiiSupportTicket ticket) =>
        eventStore.ReadModels.Release(ticket);
}
```
