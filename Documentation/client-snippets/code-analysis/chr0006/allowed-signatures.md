```csharp
using Cratis.Chronicle.Events;

public interface Chr0006ValidReducerMethodSignatures<TEvent>
{
    // Async with event only
    Task MethodNameAsync(TEvent @event);

    // Async with event and context
    Task MethodNameAsync(TEvent @event, EventContext context);

    // Synchronous with event only
    void MethodName(TEvent @event);

    // Synchronous with event and context
    void MethodName(TEvent @event, EventContext context);
}
```
