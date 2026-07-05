```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Events.Constraints;
using Cratis.Chronicle.Testing.EventSequences;

[EventType]
public record TestingScenarioWhenAuthorRegistered([property: Unique(name: "TestingScenarioUniqueAuthorName")] string Name);

public static class TestingScenarioWhen
{
    public static async Task Run()
    {
        var existingAuthorId = EventSourceId.New();
        var newAuthorId = EventSourceId.New();
        var scenario = new EventScenario();

        // Given: an author with this name is already registered
        await scenario.Given
            .ForEventSource(existingAuthorId)
            .Events(new TestingScenarioWhenAuthorRegistered("John Doe"));

        // When: attempt to register the same name under a new event source — When returns the AppendResult
        var result = await scenario.When
            .ForEventSource(newAuthorId)
            .Events(new TestingScenarioWhenAuthorRegistered("John Doe"));

        // Then: assert on the returned result
        result.ShouldHaveConstraintViolation("TestingScenarioUniqueAuthorName");
    }
}
```
