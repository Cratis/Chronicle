```csharp
using Cratis.Chronicle.EventSequences;

public static class TestingAssertionsConstraintViolation
{
    public static void Assert(AppendResult result)
    {
        result.ShouldHaveConstraintViolations();
        result.ShouldHaveConstraintViolationFor("TestingAssertionsUniqueAuthorName");
    }
}
```
