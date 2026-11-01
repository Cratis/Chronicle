```csharp
using Cratis.Chronicle;
using Cratis.Chronicle.Connections;

public static class TutorialReadModelProgram
{
    public static async Task Main()
    {
        using var client = new ChronicleClient(ChronicleConnectionString.Development);
        var eventStore = await client.GetEventStore("LibraryTutorial");
        var bookId = await TutorialFirstEventAppend.AddBook(eventStore);
        Console.WriteLine($"Appended BookAdded for {bookId}");
        var book = await WaitForBook(eventStore, bookId);
        Console.WriteLine($"Projected {book.Id}: {book.Title} | {book.Isbn} | OnLoan={book.OnLoan}");
    }

    static async Task<Book> WaitForBook(IEventStore eventStore, BookId bookId)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(60));
        using var poll = new PeriodicTimer(TimeSpan.FromMilliseconds(250));
        var lastObserved = "no query completed";
        try
        {
            while (true)
            {
                var books = await eventStore.ReadModels.GetInstances<Book>().WaitAsync(timeout.Token);
                var book = books.SingleOrDefault(book => book.Id == bookId);
                lastObserved = book is null ? "book not present" : book.ToString();
                if (book is { Title: "The Pragmatic Programmer", Isbn: "978-0135957059", OnLoan: false, BorrowedBy: null })
                {
                    return book;
                }
                await poll.WaitForNextTickAsync(timeout.Token);
            }
        }
        catch (OperationCanceledException) when (timeout.IsCancellationRequested)
        {
            throw new TimeoutException(
                $"Expected newly added book {bookId} in LibraryTutorial/Books within 60 seconds. " +
                $"Last observed: {lastObserved}. Check the event log and projection in Workbench; append success does not imply projection readiness.");
        }
    }
}
```
