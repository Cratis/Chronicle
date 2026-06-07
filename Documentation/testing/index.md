---
uid: Chronicle.Testing
---
# Testing

Chronicle provides lightweight, in-process test utilities that let you verify event appending behavior, read model projections, and reactor side-effects without a running server or database. Tests are fast, isolated, and require no infrastructure.

In Cratis applications, Chronicle tests usually sit inside [Cratis Specifications](/testing-with-cratis/). That makes the event-sourced shape explicit: `Establish()` or a scenario's `Given` builder seeds facts that already happened, `Because()` appends or provides the event under test, and each `[Fact]` asserts the new facts, read model state, or side effect.

## Packages

```xml
<PackageReference Include="Cratis.Specifications.XUnit" />
<PackageReference Include="Cratis.Chronicle.Testing" />
```

Use `Cratis.Testing` instead of `Cratis.Chronicle.Testing` when the same spec also tests Arc commands.

## Topics

| Guide | Description |
|-------|-------------|
| [Events](events/index.md) | Test event appends and constraint behavior in-process using `EventScenario` |
| [Read Models](read-models/index.md) | Test projections and reducers in-process using `ReadModelScenario<TReadModel>` |
| [Reactors](reactors/index.md) | Test reactor side-effects in-process using `ReactorScenario<TReactor>` |

## A specification-shaped example

```csharp
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

The projection runs in-process, but the spec still reads like the domain: given the `AuthorRegistered` fact, the `Author` read model should contain the name the screen needs.

For the bigger testing model across Specifications, Arc, Chronicle, and full stack slices, see [Testing with Cratis](/testing-with-cratis/).
