```csharp
using Cratis.Chronicle.Events;

public interface IReducersGettingStartedSyncSignatures<TReadModel, TEvent>
    where TReadModel : class
{
    // Without context
    TReadModel? WithoutContext(TEvent @event, TReadModel? current);

    // With context
    TReadModel? WithContext(TEvent @event, TReadModel? current, EventContext context);
}
```
