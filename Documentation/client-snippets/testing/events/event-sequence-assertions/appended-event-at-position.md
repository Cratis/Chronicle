```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Testing.EventSequences;

public static class TestingSeqAssertAppendedEventAtPosition
{
    public static async Task Run(EventScenario scenario)
    {
        await scenario.EventLog.ShouldHaveAppendedEvent<TestingSeqAssertAuthorRegistered>(0);
        await scenario.EventLog.ShouldHaveAppendedEvent<TestingSeqAssertBookAdded>(1);
    }
}
```
