```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Testing.Events;

public static class BorrowedReadModelScenarioExample
{
    public static async Task<OrderSummary?> Fold(
        EventStoreForTesting store,
        IEnumerable<AppendedEvent> selectedEvents)
    {
        using var scenario = store.CreateReadModelScenario<OrderSummary>();
        await scenario.ProcessAppendedEvents(selectedEvents);
        var summary = scenario.Instance;
        return summary;
    }
}
```
