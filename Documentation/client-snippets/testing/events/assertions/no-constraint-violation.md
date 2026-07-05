```csharp
using Cratis.Chronicle.EventSequences;

public static class TestingAssertionsNoConstraintViolation
{
    public static void Assert(AppendResult result) => result.ShouldNotHaveConstraintViolations();
}
```
