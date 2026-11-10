```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Imports;

[EventType]
public record LegacyInvoiceIssued(string Number);

public class InvoiceMigration(IEventLog eventLog)
{
    public async Task<ImportResult> Resume(ImportId importId, IReadOnlyList<(string InvoiceId, string Number)> source)
    {
        await using var import = await eventLog.BeginImport(importId);

        // The kernel already holds the first EventsAcknowledged events; continue with the rest.
        var remaining = source
            .Skip((int)import.EventsAcknowledged)
            .Select(invoice => new EventForEventSourceId(invoice.InvoiceId, new LegacyInvoiceIssued(invoice.Number)));
        await import.Add(remaining);
        return await import.Complete();
    }
}
```
