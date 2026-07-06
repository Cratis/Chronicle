```csharp
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Testing.EventSequences;
using Cratis.Specifications;
using Xunit;

public class and_the_isbn_already_exists : Specification, IDisposable
{
    EventScenario _scenario = null!;
    AppendResult _result = null!;

    async Task Establish()
    {
        _scenario = new EventScenario();
        await _scenario.Given
            .ForEventSource(TestSliceBookId.New())
            .Events(new TestSliceBookAdded("The Pragmatic Programmer", "978-0135957059"));
    }

    async Task Because() =>
        _result = await _scenario.EventLog.Append(
            TestSliceBookId.New(),
            new TestSliceBookAdded("The Pragmatic Programmer, 2nd ed.", "978-0135957059"));

    [Fact] void should_be_rejected() => _result.ShouldBeFailed();
    [Fact] void should_report_the_violated_constraint() => _result.ShouldHaveConstraintViolationFor("TestSliceUniqueIsbn");

    public void Dispose() => _scenario.Dispose();
}
```
