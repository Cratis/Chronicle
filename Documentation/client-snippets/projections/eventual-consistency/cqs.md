```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.ReadModels;

[EventType]
public record EcCqsBookCreated(string Title);

public record EcCqsBook(Guid Id, string Title);

// Commands — fire and forget, never return projected state
public class EcCqsBookCommandHandler(IEventLog eventLog)
{
    public Task Create(EventSourceId bookId, string title) =>
        eventLog.Append(bookId, new EcCqsBookCreated(title));
}

// Queries — always read from projections
public class EcCqsBookQueryHandler(IReadModels readModels)
{
    public Task<EcCqsBook> GetBook(EventSourceId bookId) =>
        readModels.GetInstanceById<EcCqsBook>(bookId);
}
```
