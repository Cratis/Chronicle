```csharp
using Cratis.Chronicle;
using Cratis.Chronicle.Events;

public record ScenariosQueryBook(string Title, bool OnLoan);

public class ScenariosQueryBookService(IEventStore eventStore)
{
    public Task<ScenariosQueryBook> GetBook(EventSourceId bookId) =>
        eventStore.ReadModels.GetInstanceById<ScenariosQueryBook>(bookId.Value);
}
```
