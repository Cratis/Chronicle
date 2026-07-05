```csharp
using Cratis.Chronicle;

public static class TutorialFirstEventAppend
{
    public static async Task<BookId> AddBook(IEventStore eventStore)
    {
        var book = BookId.New();
        await eventStore.EventLog.Append(book, new BookAdded("The Pragmatic Programmer", "978-0135957059"));
        return book;
    }
}
```
