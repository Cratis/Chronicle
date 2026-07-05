```csharp
using Cratis.Chronicle.Reducers;

public record ReducersGettingStartedAttributeOrderSummary(Guid OrderId);

[Reducer(id: "order-summary", eventSequence: "order-events")]
public class ReducersGettingStartedAttributeOrderSummaryReducer : IReducerFor<ReducersGettingStartedAttributeOrderSummary>;
```
