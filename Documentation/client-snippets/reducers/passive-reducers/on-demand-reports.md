```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.ReadModels;
using Cratis.Chronicle.Reducers;

[EventType]
public record PassiveReducersPaymentReceived(string Category, decimal Amount);

[Passive]
public record PassiveReducersMonthlyRevenueReport(
    decimal TotalRevenue,
    Dictionary<string, decimal> RevenueByCategory,
    int Month,
    int Year);

public class PassiveReducersMonthlyRevenueReportReducer : IReducerFor<PassiveReducersMonthlyRevenueReport>
{
    public PassiveReducersMonthlyRevenueReport Received(PassiveReducersPaymentReceived @event, PassiveReducersMonthlyRevenueReport? current, EventContext context)
    {
        var revenue = current?.TotalRevenue ?? 0m;
        var byCategory = current?.RevenueByCategory ?? new Dictionary<string, decimal>();

        if (!byCategory.ContainsKey(@event.Category))
            byCategory[@event.Category] = 0;

        byCategory[@event.Category] += @event.Amount;

        return new PassiveReducersMonthlyRevenueReport(
            revenue + @event.Amount,
            byCategory,
            context.Occurred.Month,
            context.Occurred.Year);
    }
}
```
