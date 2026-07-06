```csharp
[Reactor(id: "order-notifications")]
public class NamedOrderNotificationsReactor : IReactor
{
    public Task Placed(ReactorOrderPlaced @event) => Task.CompletedTask;
}
```
