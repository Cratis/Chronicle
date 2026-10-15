```csharp
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Testing.EventSequences;

[EventType]
public record TestingScenarioManualCloseReportEdited(string Text);

public static class TestingScenarioManualClose
{
    public static async Task Run()
    {
        using var scenario = new EventScenario();
        var scope = new ClosedStreamScope(EventSourceId: "report-1", EventStreamId: "2026-04");

        var closed = await scenario.EventSequence.CompleteStream(scope);
        Console.WriteLine($"Closed: {closed.IsSuccess}");

        var edit = await scenario.EventLog.Append(
            "report-1",
            new TestingScenarioManualCloseReportEdited("Late edit"),
            "monthly-report",
            "2026-04");
        edit.ShouldHaveConstraintViolation("closed-stream");

        var closedStreams = await scenario.EventSequence.GetClosedStreams();
        Console.WriteLine($"Closed scopes: {closedStreams.Count}");
    }
}
```
