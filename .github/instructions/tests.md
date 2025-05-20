# 🧪 How to Write Tests

We call automated tests for specs or specifications based on Specification by Example related to BDD (Behavior Driven Development).
Keep tests focused, isolated, and descriptive!

## 1. Test Frameworks & Conventions

- **Frameworks:**
  - Uses [Xunit](https://xunit.net/) for test execution.
  - Uses [NSubstitute](https://nsubstitute.github.io/) for mocking.
  - Uses [Cratis Specifications](https://github.com/Cratis/Specifications/blob/main/README.md) for BDD style specification by example testing.

- **File/Folder Structure:**
  - Organize tests by feature/domain, e.g. `Events/Constraints/for_UniqueConstraintProvider/when_providing.cs`.
  - Use descriptive folder and file names:
    - `for_<TypeUnderTest>/<scenario>.cs`
    - Example: `for_UnitOfWork/when_committing/and_it_has_events_and_append_returns_constraints_and_errors.cs`
  - Tests are found in their own projects named the same as the thing being tested postfixed with .Specs. E.g. DotNET -> DotNET.Specs

## 2. Test Class Pattern

- Inherit from `Specification` or a relevant `given.*` base class for shared setup.
- Use BDD-style methods:
  - `void Establish()` for setup.
  - `void Because()` for the action under test.
  - `[Fact] void should_<expected_behavior>()` for assertions.

**Example:**

```csharp
public class when_adding : Specification
{
    EventsToAppend events;
    string @event;

    void Establish()
    {
        events = [];
        @event = "Forty Two";
    }

    void Because() => events.Add(@event);

    [Fact] void should_hold_the_added_event() => events.First().ShouldEqual(@event);
}
```

- Behavior that can have multiple angles to it will have the `when_*` statement as a folder name, and then a specific condition typically be called `and_*.cs`or similar.

## 3. Test Utilities

- Use `Substitute.For<T>()` for mocks.
- Use `.ShouldEqual()`, `.ShouldBeTrue()`, etc., for assertions.
- Use custom helpers from `XUnit.Integration` for integration/event-based assertions.

## 4. Global Usings

- Common usings are provided in `GlobalUsings.Specs.cs` (e.g., `using Xunit;`, `using NSubstitute;`).

## 5. Integration Tests

- Use fixtures like `OrleansFixture` for integration scenarios.
- Use extension methods in `XUnit.Integration/EventsShouldExtensions.cs` for event log assertions.

## 6. Naming

- Use clear, descriptive names for test classes and methods.
- Folder: `for_<Unit>` and possible a sub folder of `when_<action>` for multiple conditional specifications.
- Class: Single condition - `when_<action>[_and_<condition>]`, multiple conditional `and_<condition>`
- Method: `should_<expected_result>`
