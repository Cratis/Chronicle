```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Events.Constraints;
using Cratis.Chronicle.Testing.EventSequences;

[EventType]
[ClosesStream(EventStreamIdFrom = nameof(Period), ReopenedBy = [typeof(TestingScenarioBooksReopened)])]
public record TestingScenarioBooksClosed(string Period);

[EventType]
public record TestingScenarioBooksReopened(string Period);

[EventType]
public record TestingScenarioEntryPosted(string Description);

public static class TestingScenarioClosingEvent
{
    public static async Task Run()
    {
        using var scenario = new EventScenario();

        var closing = await scenario.EventLog.Append("ledger-1", new TestingScenarioBooksClosed("2026-04"));
        closing.ShouldBeSuccessful();

        // Covered: same event source, same stream type, stream id taken from the closing event's Period.
        var late = await scenario.EventLog.Append(
            "ledger-1",
            new TestingScenarioEntryPosted("Late invoice"),
            "ledger",
            "2026-04");
        late.ShouldHaveConstraintViolation("closed-stream");

        // Not covered: another event source, and the next period.
        (await scenario.EventLog.Append("ledger-2", new TestingScenarioEntryPosted("Open"), "ledger", "2026-04")).ShouldBeSuccessful();
        (await scenario.EventLog.Append("ledger-1", new TestingScenarioEntryPosted("Next"), "ledger", "2026-05")).ShouldBeSuccessful();
    }
}
```
