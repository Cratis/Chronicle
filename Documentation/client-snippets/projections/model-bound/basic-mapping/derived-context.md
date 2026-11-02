```csharp title="Map derived event context"
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Keys;
using Cratis.Chronicle.Projections.ModelBound;

[EventType]
public record AccountOpenedForWeeklyAudit(string Name);

public record WeeklyAccountAudit(
    [Key] Guid Id,

    [SetFromContext<AccountOpenedForWeeklyAudit>("Occurred.Week")]
    int OpenedWeek,

    [SetFromContext<AccountOpenedForWeeklyAudit>("Occurred.WeekYear")]
    int OpenedWeekYear,

    [FromEvery(contextProperty: "Occurred.Week()")]
    int LastUpdatedWeek);
```
