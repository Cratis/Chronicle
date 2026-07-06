```csharp
using Cratis.Chronicle.Events;

public interface ValidReactorMethodSignatures<TEvent, TReadModel, IService>
{
    void MethodName(TEvent @event);
    Task MethodNameAsync(TEvent @event);

    void MethodName(TEvent @event, EventContext context);
    Task MethodNameAsync(TEvent @event, EventContext context);

    Task MethodNameAsync(TEvent @event, EventContext context, TReadModel readModel, IService service);
    Task MethodNameAsync(TEvent @event, TReadModel readModel);
}
```
