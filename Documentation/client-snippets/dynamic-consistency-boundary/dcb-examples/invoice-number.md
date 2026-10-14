```csharp
using Cratis.Chronicle;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Projections.ModelBound;
using Cratis.Chronicle.ReadModels;

[EventType]
public record InvoiceCreated(int Number, string Customer);

// The decision model: the last number issued. Every invoice is created on one numbering event source.
[FromEvent<InvoiceCreated>]
public record InvoiceNumbering(string Id, [SetFrom<InvoiceCreated>(nameof(InvoiceCreated.Number))] int Last);

public static class CreateInvoice
{
    const string NumberingSource = "invoice-numbering";

    public static async Task<int?> Handle(IEventStore eventStore, string customer)
    {
        var numbering = await eventStore.GetDecisionReads().GetDetached<InvoiceNumbering>(new ReadModelKey(NumberingSource));
        var next = (numbering.Instance?.Last ?? 0) + 1;

        // If another invoice was created after the read, the append is rejected and nothing is written.
        var result = await eventStore.EventLog.AppendMany(
            [new EventForEventSourceId(NumberingSource, new InvoiceCreated(next, customer))],
            guardedBy: [numbering]);

        return result.IsSuccess ? next : null;
    }
}
```
