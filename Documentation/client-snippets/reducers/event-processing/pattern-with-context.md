```csharp
using Cratis.Chronicle.Events;

public interface IEventProcessingPatternWithContext<TReadModel, TEvent>
    where TReadModel : class
{
    // Access occurred time, correlation ID, etc.
    TReadModel Process(TEvent @event, TReadModel? current, EventContext context);
}
```
