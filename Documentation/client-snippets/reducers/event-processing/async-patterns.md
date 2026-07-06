```csharp
using Cratis.Chronicle.Events;

public interface IEventProcessingAsyncPatterns<TReadModel, TEvent>
    where TReadModel : class
{
    // Async without context
    Task<TReadModel> ProcessAsync(TEvent @event, TReadModel? current);

    // Async with context
    Task<TReadModel> ProcessWithContextAsync(TEvent @event, TReadModel? current, EventContext context);
}
```
