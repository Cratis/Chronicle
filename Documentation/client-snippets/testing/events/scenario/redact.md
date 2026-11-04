```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Testing.EventSequences;

[EventType]
public record TestingScenarioRedactNoteWritten(string Text);

public static class TestingScenarioRedact
{
    public static async Task Run()
    {
        using var scenario = new EventScenario();

        var appended = await scenario.EventLog.Append("member-1", new TestingScenarioRedactNoteWritten("Call me at 555-0100"));
        await scenario.EventSequence.Redact(appended.SequenceNumber, "Subject asked for erasure");

        var events = await scenario.EventLog.GetFromSequenceNumber(EventSequenceNumber.First);
        Console.WriteLine(events[0].Context.EventType.Id);
    }
}
```
