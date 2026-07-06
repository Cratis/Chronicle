```csharp
using Cratis.Chronicle;

public class ComplianceReadModelsEmployeeService(IEventStore eventStore)
{
    public Task<ComplianceReadModelsEmployee> GetEmployee(Guid id) =>
        eventStore.ReadModels.GetInstanceById<ComplianceReadModelsEmployee>(id);
}
```
