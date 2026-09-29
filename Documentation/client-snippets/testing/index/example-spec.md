```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Keys;
using Cratis.Chronicle.Projections.ModelBound;
using Cratis.Chronicle.Testing.ReadModels;
using Cratis.Specifications;
using Xunit;

[EventType]
public record AuthorRegistered(string Name);

[FromEvent<AuthorRegistered>]
public record Author([Key] Guid Id, string Name);

public class when_projecting_a_registered_author : Specification
{
    readonly EventSourceId _authorId = EventSourceId.New();
    readonly ReadModelScenario<Author> _scenario = new();

    Task Because() =>
        _scenario.Given
            .ForEventSource(_authorId)
            .Events(new AuthorRegistered("Jane Austen"));

    [Fact] void should_set_the_author_name() =>
        _scenario.Instance!.Name.ShouldEqual("Jane Austen");
}
```
