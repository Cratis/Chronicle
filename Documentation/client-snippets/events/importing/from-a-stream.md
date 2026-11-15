```csharp
using System.Runtime.CompilerServices;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Imports;

[EventType]
public record LegacyCustomerRegistered(string Name);

public record LegacyRow(string EventId, string CustomerId, string Name, DateTimeOffset At, ulong Position);

public class CustomerMigration(IEventLog eventLog)
{
    public Task<ImportResult> Run(IAsyncEnumerable<LegacyRow> rows, CancellationToken cancellationToken) =>
        eventLog.Import(
            ToEvents(rows, cancellationToken),
            new ImportOptions { OnInvalid = OnInvalidEvent.Reject },
            new Progress<ImportProgress>(progress => Console.WriteLine($"{progress.State}: {progress.EventsCommitted} committed")),
            cancellationToken);

    static async IAsyncEnumerable<EventForEventSourceId> ToEvents(IAsyncEnumerable<LegacyRow> rows, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await foreach (var row in rows.WithCancellation(cancellationToken))
        {
            yield return new EventForEventSourceId(row.CustomerId, new LegacyCustomerRegistered(row.Name))
            {
                Occurred = row.At,
                IdempotencyKey = new IdempotencyKey(row.EventId),
                SourceSequenceNumber = row.Position
            };
        }
    }
}
```
