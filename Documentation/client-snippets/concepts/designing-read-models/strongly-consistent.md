```csharp
using Cratis.Chronicle;
using Cratis.Chronicle.ReadModels;

// Passive: no observer materializes this, so Chronicle computes it from the events on every read.
[Passive]
public record DesigningReadModelsCustomerDetail(Guid Id, string Name);

public class DesigningReadModelsCustomerDetailService(IEventStore eventStore)
{
    public Task<DesigningReadModelsCustomerDetail> GetDetail(Guid customerId) =>
        eventStore.ReadModels.GetInstanceById<DesigningReadModelsCustomerDetail>(customerId);
}
```
