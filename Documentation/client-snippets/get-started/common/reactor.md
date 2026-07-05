```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reactors;

public class GetStartedBookReturnedNotifier : IReactor
{
    public Task Returned(GetStartedBookReturned @event, EventContext context)
    {
        // context.EventSourceId is the BookId this happened to
        Console.WriteLine($"Book {context.EventSourceId} was returned — notify the next member in line.");
        return Task.CompletedTask;
    }
}
```
