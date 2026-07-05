```csharp
public interface IEventProcessingBasicSyncPattern<TReadModel, TEvent>
    where TReadModel : class
{
    // Process event and return new state
    TReadModel Process(TEvent @event, TReadModel? current);
}
```
