```csharp
using Cratis.Chronicle;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reactors;

public class ScenariosReactStockCouldNotBeDecreased(string isbn) : Exception($"Stock could not be decreased for ISBN {isbn}");

public class ScenariosReactStockKeepingExplicit(IEventStore eventStore) : IReactor
{
    public async Task BookReserved(ScenariosReactBookReserved @event, EventContext context)
    {
        var result = await eventStore.EventLog.Append(
            context.EventSourceId, new ScenariosReactStockDecreased(@event.Isbn, 1));
        if (!result.IsSuccess)
        {
            throw new ScenariosReactStockCouldNotBeDecreased(@event.Isbn);
        }
    }
}
```
