```csharp
using System.Linq;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Keys;
using Cratis.Chronicle.Projections.ModelBound;
using Cratis.Chronicle.Testing.ReadModels;
using Cratis.Specifications;

[EventType]
public record TestingReadModelScenarioChildrenTimesheetStarted(int Year);

[EventType]
public record TestingReadModelScenarioChildrenDayRecorded(DateOnly Day, decimal Hours);

public record TestingReadModelScenarioChildrenTimesheetDay(DateOnly Day, decimal Hours);

[FromEvent<TestingReadModelScenarioChildrenTimesheetStarted>]
public record TestingReadModelScenarioChildrenTimesheet(
    [Key] Guid Id,
    int Year,

    [ChildrenFrom<TestingReadModelScenarioChildrenDayRecorded>(key: nameof(TestingReadModelScenarioChildrenDayRecorded.Day))]
    IEnumerable<TestingReadModelScenarioChildrenTimesheetDay> Days);

public static class TestingReadModelScenarioChildren
{
    public static async Task Run()
    {
        var scenario = new ReadModelScenario<TestingReadModelScenarioChildrenTimesheet>();
        await scenario.Given
            .ForEventSource(Guid.NewGuid())
            .Events(
                new TestingReadModelScenarioChildrenTimesheetStarted(2026),
                new TestingReadModelScenarioChildrenDayRecorded(new DateOnly(2026, 6, 1), 7.5m),
                new TestingReadModelScenarioChildrenDayRecorded(new DateOnly(2026, 6, 2), 8m));

        scenario.Instance!.Days.Count().ShouldEqual(2);
        scenario.Instance!.Days.First().Day.ShouldEqual(new DateOnly(2026, 6, 1));
        scenario.Instance!.Days.First().Hours.ShouldEqual(7.5m);
    }
}
```
