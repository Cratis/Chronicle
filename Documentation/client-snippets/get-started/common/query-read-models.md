```csharp
using Cratis.Chronicle;

public class GetStartedBookQueryService(IEventStore eventStore)
{
    public async Task<(IEnumerable<GetStartedBook> Books, IEnumerable<GetStartedBorrowedBook> BorrowedBooks)> QueryBooks()
    {
        var books = await eventStore.ReadModels.GetInstances<GetStartedBook>();
        var borrowedBooks = await eventStore.ReadModels.GetInstances<GetStartedBorrowedBook>();

        return (books, borrowedBooks);
    }
}
```
