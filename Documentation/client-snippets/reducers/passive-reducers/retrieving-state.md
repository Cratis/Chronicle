```csharp
using Cratis.Chronicle;
using Cratis.Chronicle.ReadModels;

public class PassiveReducersReportingService(IEventStore eventStore)
{
    public async Task<PassiveReducersMonthlyRevenueReport> GenerateReport(Guid reportId) =>
        // This triggers the passive reducer to compute state from events
        await eventStore.ReadModels.GetInstanceById<PassiveReducersMonthlyRevenueReport>(reportId);
}
```
