```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.ReadModels;

[EventType]
public record EcBookCreated(string Title, string Author);

public record EcBookInventory(Guid Id, string Title, string Author);

public class EcBookService(IEventLog eventLog, IReadModels readModels)
{
    // Good — fire and forget: don't wait for the projection before returning
    public async Task<EventSourceId> CreateBook(string title, string author)
    {
        var bookId = EventSourceId.New();
        await eventLog.Append(bookId, new EcBookCreated(title, author));
        return bookId;
    }

    // Problematic — expecting immediate consistency
    public async Task<EcBookInventory> CreateBookAndReturn(string title, string author)
    {
        var bookId = EventSourceId.New();
        await eventLog.Append(bookId, new EcBookCreated(title, author));

        // The projection may not have run yet — this can return a stale or default instance
        return await readModels.GetInstanceById<EcBookInventory>(bookId);
    }
}
```
