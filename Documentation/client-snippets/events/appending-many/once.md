```csharp
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Events;

[EventType]
public record ImportedItemAdded(string Sku, int Quantity);

public class CartImportService(IEventLog eventLog)
{
    public async Task<bool> Import(string cartId, IEnumerable<ImportedItemAdded> items, string importId)
    {
        // One key for the batch. Chronicle derives a distinct key for each event from its position.
        var result = await eventLog.AppendManyOnce(
            cartId,
            items.Cast<object>(),
            new IdempotencyKey($"import:{importId}"));

        if (result.HasIdempotencyConflicts)
        {
            return false;
        }

        // Outcomes has one entry per event, in the order the events were given.
        return result.Outcomes.All(outcome => outcome == AppendOutcome.Appended);
    }
}
```
