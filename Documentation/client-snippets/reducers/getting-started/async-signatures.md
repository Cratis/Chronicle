```csharp
using Cratis.Chronicle.Events;

public interface IReducersGettingStartedAsyncSignatures<TReadModel, TEvent>
    where TReadModel : class
{
    // Without context
    Task<TReadModel?> WithoutContext(TEvent @event, TReadModel? current);

    // With context
    Task<TReadModel?> WithContext(TEvent @event, TReadModel? current, EventContext context);
}
```
