```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Events.Constraints;
using Cratis.Chronicle.EventSequences;
using System.Linq;

[EventType]
public record ClosingStreamsInvoiceLineAdded(string Description, decimal Amount);

public class ClosingStreamsInvoiceLineAppender(IEventLog eventLog)
{
    public async Task<bool> TryAppendLine(EventSourceId invoiceId)
    {
        var appendResult = await eventLog.Append(
            invoiceId,
            new ClosingStreamsInvoiceLineAdded("Consulting", 500m),
            new EventStreamType("invoices"),
            new EventStreamId("invoice-42"));

        if (!appendResult.IsSuccess)
        {
            var violation = appendResult.ConstraintViolations
                .FirstOrDefault(v => v.ConstraintType == ConstraintType.StreamClosed);
            return violation is null;
        }

        return true;
    }
}
```
