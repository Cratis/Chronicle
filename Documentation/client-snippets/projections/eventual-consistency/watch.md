```csharp
using System.Reactive.Linq;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.ReadModels;

[EventType]
public record EcWatchBookCreated(string Title, string Author);

public record EcWatchBookInventory(Guid Id, string Title, string Author);

public class EcWatchBookService(IEventLog eventLog, IReadModels readModels)
{
    public IObservable<ReadModelChangeset<EcWatchBookInventory>> WatchBookChanges() =>
        readModels.Watch<EcWatchBookInventory>();

    public async Task CreateBookAndWatch(string title, string author)
    {
        var bookId = EventSourceId.New();

        // Subscribe before appending; this example does not await subscription readiness.
        // Awaiting Append does not wait for a matching changeset.
        // The subscription is disposed on method exit, possibly before the update arrives.
        using var subscription = readModels.Watch<EcWatchBookInventory>()
            .Where(changeset => changeset.ModelKey.Value == bookId.Value)
            .Subscribe(changeset => Console.WriteLine($"Book projection updated: {changeset.ReadModel?.Title}"));

        await eventLog.Append(bookId, new EcWatchBookCreated(title, author));
    }
}
```
