```csharp
using Cratis.Chronicle;
using Cratis.Chronicle.ReadModels;

public record DesigningReadModelsCustomerDetail(Guid Id, string Name);

public class DesigningReadModelsCustomerDetailService(IEventStore eventStore)
{
    public Task<DesigningReadModelsCustomerDetail> GetDetail(Guid customerId) =>
        eventStore.ReadModels.GetInstanceById<DesigningReadModelsCustomerDetail>(customerId);
}
```
