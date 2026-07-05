```csharp
using Cratis.Chronicle;

public class DecPassiveUserService(IEventStore eventStore)
{
    public Task<DecPassiveUserSummary> GetUserSummary(string userId) =>
        eventStore.ReadModels.GetInstanceById<DecPassiveUserSummary>(userId);
}
```
