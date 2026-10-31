```csharp
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Events;

public class ScopedClosingCustomerArchiver(IEventLog eventLog)
{
    public async Task ArchiveCustomer(EventSourceId customerId)
    {
        // Every stream of this event source is closed, including its default stream.
        var result = await eventLog.CompleteStream(ClosedStreamScope.ForEventSource(customerId));

        result.Switch(
            sequenceNumber => Console.WriteLine($"Customer archived at sequence number {sequenceNumber}"),
            error => Console.WriteLine($"Not archived: {error}"));
    }
}
```
