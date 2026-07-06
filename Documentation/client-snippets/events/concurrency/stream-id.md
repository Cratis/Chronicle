```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;

[EventType]
public record ConcurrencyMonthlyReportGenerated(string Month);

public class ConcurrencyMonthlyReportService(IEventLog eventLog)
{
    public async Task GenerateMonthlyReport(EventSourceId accountId, DateTime month)
    {
        var monthKey = month.ToString("yyyy-MM");

        var concurrencyScope = new ConcurrencyScopeBuilder()
            .WithEventSourceId(accountId)
            .WithEventStreamType("Reporting")
            .WithEventStreamId(monthKey)
            .WithSequenceNumber(5)
            .Build();

        await eventLog.Append(
            accountId,
            new ConcurrencyMonthlyReportGenerated(monthKey),
            eventStreamType: "Reporting",
            eventStreamId: monthKey,
            concurrencyScope: concurrencyScope);
    }
}
```
