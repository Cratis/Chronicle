```csharp
using Cratis.Chronicle.Events;

public interface ReactorHandlerSignatures<TEvent, TResult>
{
    void MethodName(TEvent @event);
    void MethodName(TEvent @event, EventContext context);

    Task MethodNameAsync(TEvent @event);
    Task MethodNameAsync(TEvent @event, EventContext context);

    Task<TResult> MethodNameReturningAsync(TEvent @event);
    Task<TResult> MethodNameReturningAsync(TEvent @event, EventContext context);

    TResult MethodNameReturning(TEvent @event);
    TResult MethodNameReturning(TEvent @event, EventContext context);
}
```
