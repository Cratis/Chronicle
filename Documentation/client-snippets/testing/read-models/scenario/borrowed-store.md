```csharp
using var scenario = store.CreateReadModelScenario<OrderSummary>();
await scenario.ProcessAppendedEvents(selectedEvents);
var summary = scenario.Instance;
```
