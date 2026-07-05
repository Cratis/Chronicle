```csharp
using Cratis.Chronicle.EventSequences;

public static class TestingAssertionsErrors
{
    public static void AssertHasErrors(AppendResult result) => result.ShouldHaveErrors();

    public static void AssertNoErrors(AppendResult result) => result.ShouldNotHaveErrors();
}
```
