```csharp title="Identify a weekly summary by ISO week-year and week"
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Projections;
using Cratis.Chronicle.Properties;

public record WeeklyKey(int WeekYear, int Week);
public record WeeklySummary(WeeklyKey Id);

[EventType]
public record WeeklyActivityRecorded;

public class WeeklySummaryProjection : IProjectionFor<WeeklySummary>
{
    public void Define(IProjectionBuilderFor<WeeklySummary> builder) => builder
        .From<WeeklyActivityRecorded>(activity => activity
            .UsingCompositeKey<WeeklyKey>(key => key
                .Set(k => k.WeekYear).ToEventContextProperty(c => c.Occurred.WeekYear())
                .Set(k => k.Week).ToEventContextProperty(c => c.Occurred.Week())));
}
```
