```csharp
using Cratis.Chronicle.Testing.EventSequences;
using Cratis.Specifications;

public static class TestingSeqAssertValidatorNoSequence
{
    public static Task Run(EventScenario scenario) =>
        scenario.EventLog.ShouldHaveAppendedEvent<TestingSeqAssertAuthorRegistered>(author =>
            author.Name.ShouldEqual("Jane Smith"));
}
```
