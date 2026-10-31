```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Events.Constraints;
using Cratis.Chronicle.Testing.EventSequences;

[EventType]
[ClosesStream(EventStreamIdFrom = nameof(Period), ReopenedBy = [typeof(TestingScenarioReopeningBooksReopened)])]
public record TestingScenarioReopeningBooksClosed(string Period);

[EventType]
public record TestingScenarioReopeningBooksReopened(string Period);

[EventType]
public record TestingScenarioReopeningEntryPosted(string Description);

public static class TestingScenarioReopeningEvent
{
    public static async Task Run()
    {
        using var scenario = new EventScenario();

        await scenario.Given
            .ForEventSource("ledger-1")
            .Events(new TestingScenarioReopeningBooksClosed("2026-04"));

        // The reopening fact is accepted inside the scope it reopens.
        var reopened = await scenario.When
            .ForEventSource("ledger-1")
            .Events(new TestingScenarioReopeningBooksReopened("2026-04"));
        reopened.ShouldBeSuccessful();

        var posted = await scenario.EventLog.Append(
            "ledger-1",
            new TestingScenarioReopeningEntryPosted("Correction"),
            "ledger",
            "2026-04");
        posted.ShouldBeSuccessful();
    }
}
```
