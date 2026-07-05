```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.ReadModels;
using Cratis.Chronicle.Reducers;

[EventType]
public record PassiveReducersTransactionCompleted(decimal Amount);

[Passive]
public record PassiveReducersAdHocReport(
    decimal TotalRevenue,
    int TransactionCount,
    DateTimeOffset GeneratedAt);

public class PassiveReducersAdHocReportReducer : IReducerFor<PassiveReducersAdHocReport>
{
    public PassiveReducersAdHocReport Completed(PassiveReducersTransactionCompleted @event, PassiveReducersAdHocReport? current, EventContext context)
    {
        var revenue = current?.TotalRevenue ?? 0m;
        var count = current?.TransactionCount ?? 0;

        return new PassiveReducersAdHocReport(
            revenue + @event.Amount,
            count + 1,
            context.Occurred);
    }
}
```
