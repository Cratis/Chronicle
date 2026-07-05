```csharp
public class ReactorRegistration
{
    public Task Register(IEventStore eventStore) => eventStore.Reactors.Register();
}
```
