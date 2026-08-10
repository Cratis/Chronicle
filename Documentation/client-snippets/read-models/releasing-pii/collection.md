```csharp
using Cratis.Chronicle;

public class ReleasingPiiSupportTicketBatchService(IEventStore eventStore)
{
    public Task<IEnumerable<ReleasingPiiSupportTicket>> ReleaseAll(IEnumerable<ReleasingPiiSupportTicket> tickets) =>
        eventStore.ReadModels.Release(tickets);
}
```
