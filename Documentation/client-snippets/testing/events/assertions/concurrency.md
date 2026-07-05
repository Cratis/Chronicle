```csharp
using Cratis.Chronicle.EventSequences;

public static class TestingAssertionsConcurrency
{
    public static void AssertHasViolations(AppendResult result) => result.ShouldHaveConcurrencyViolations();

    public static void AssertNoViolations(AppendResult result) => result.ShouldNotHaveConcurrencyViolations();
}
```
