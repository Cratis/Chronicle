```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Imports;

[EventType]
public record LegacyOrderPlaced(string OrderNumber);

public class OrderMigration(IEventLog eventLog)
{
    public async Task<ImportResult> Run(IEnumerable<IEnumerable<(string OrderId, string Number)>> batches)
    {
        await using var import = await eventLog.BeginImport(new ImportOptions { MaxChunksInFlight = 4 });
        Console.WriteLine($"Importing as {import.ImportId}");

        foreach (var batch in batches)
        {
            await import.Add(batch.Select(order => new EventForEventSourceId(order.OrderId, new LegacyOrderPlaced(order.Number))));
        }

        return await import.Complete();
    }
}
```
