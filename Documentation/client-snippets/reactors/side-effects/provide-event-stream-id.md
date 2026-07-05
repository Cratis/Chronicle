```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reactors;

public class TenantReactor(string tenantId) : IReactor, ICanProvideEventStreamId
{
    public EventStreamId GetEventStreamId() => tenantId;
}
```
