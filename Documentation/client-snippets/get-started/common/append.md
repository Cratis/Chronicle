```csharp
using Cratis.Chronicle;

public class GetStartedBookService(IEventStore eventStore)
{
    public async Task<Guid> AddBook()
    {
        var eventLog = eventStore.EventLog;

        var bookId = Guid.NewGuid();
        await eventLog.Append(bookId, new GetStartedBookAdded("The Pragmatic Programmer", "978-0135957059"));

        return bookId;
    }
}
```
