```csharp
using Cratis.Chronicle.EventSequences;

public class ClosingStreamsInvoiceCloser(IEventLog eventLog)
{
    public async Task CloseInvoiceStream(EventStreamId invoiceStreamId)
    {
        var result = await eventLog.CompleteStream(new EventStreamType("invoices"), invoiceStreamId);

        result.Switch(
            sequenceNumber => Console.WriteLine($"Stream closed at sequence number {sequenceNumber}"),
            error => Console.WriteLine($"Failed to close stream: {error}"));
    }
}
```
