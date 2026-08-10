```csharp
using Cratis.Chronicle;

public class ReleasingPiiSupportTicketWatcher(IEventStore eventStore)
{
    public IDisposable Start() =>
        eventStore.ReadModels.Watch<ReleasingPiiSupportTicket>().Subscribe(async changeset =>
        {
            if (changeset.Removed || changeset.ReadModel is null)
            {
                return;
            }

            var ticket = await eventStore.ReadModels.Release(changeset.ReadModel);
            Console.WriteLine($"{changeset.ModelKey}: {ticket.RequesterName}");
        });
}
```
