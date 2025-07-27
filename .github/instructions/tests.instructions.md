---
applyTo: "**/*.Specs/**/*.cs"
---

# 🧪 How to Write Tests

We call automated tests for specs or specifications based on Specification by Example related to BDD (Behavior Driven Development).
Keep tests focused, isolated, and descriptive!

## Test Frameworks & Conventions

- **Frameworks:**
  - Uses [Xunit](https://xunit.net/) as test framework and execution.
  - Uses [NSubstitute](https://nsubstitute.github.io/) for mocking.
  - Uses [Cratis Specifications](https://github.com/Cratis/Specifications/blob/main/README.md) for BDD style specification by example testing.

- **File/Folder Structure:**
  - Organize tests by feature/domain, e.g. `Events/Constraints/for_UniqueConstraintProvider/when_providing.cs`.
  - Use descriptive folder and file names:
    - `for_<TypeUnderTest>/<scenario>.cs`
    - Example: `for_UnitOfWork/when_committing/and_it_has_events_and_append_returns_constraints_and_errors.cs`
  - Tests are found in their own projects named the same as the thing being tested postfixed with .Specs. E.g. DotNET -> DotNET.Specs

## Base class

- At the root of inheritance, `Specification` must be the base class.

## Test Class Pattern

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

- Establish method can also be async if it needs to perform tasks that are async.

## What to specify

- Write specs that verifies what is promised from the signature of methods, not based on its implementation - we want to catch problems with the implementation.

## Naming

- Use clear, descriptive names for test classes and methods.
- Folder: `for_<Unit>`.
- Class: Single condition - `when_<action>[_and_<aspect>]`. Not limited to `and`, use any preposition that makes sense (e.g. with, without)
- Method: `should_<expected_result>`

## Aspects of same behavior

- Behavior that can have multiple aspects or perspectives of a functionality should have the `when_<action>` statement as a folder name, then the specific aspect/perspective/condition would typically be called `and_*.cs` or similar.
- When multiple aspects, file names would be `and_<aspect>`, but not limited to - use any prepositions that makes sense as well as prefix

## Behaviors

- Capture the behavior in naming, not just necessarily use names that reflect the code or method name, it should be meaningful as to what the code is doing and trying to accomplish
- Every method should be tested separately, never write a single file for an entire unit under test.
- Keep a single behavior or aspect of a behavior in a single file.

## Namespaces

- The namespace should start with the same as the unit having specs for it.
- Do not include `.Specs` in the namespace.

## Reusable Context

- Context can be encapsulated into reusable contexts that can be leveraged between specs.
- Next to the specs, create a folder called `given` and add a specific context that describes what the context is, typically starting with `a_` or `an_`.
- Namespace should include `given` in the name.
- Members that are to be reused should be protected.
- Members are initializes in the same manner as specs without a shared context using the `Establish` method.
- When there is a need for a reusable context, specs should then inherit from the context by doing `given.a_specific_context`, example: `public class when_performing_a_behavior : given.a_specific_context`
- Remember the inheritance rule, `Specification` must be in the inheritance chain at the root.
- Contexts can share other contexts which.

## Substitutes

- Concrete classes that are not abstract can't be substituted using NSubstitute. Create proper instances of them.

## Test Utilities

- Use `Substitute.For<T>()` for mocks.
- Use `.ShouldEqual()`, `.ShouldBeTrue()`, etc., for assertions.
- Use custom helpers from `XUnit.Integration` for integration/event-based assertions.

## Using statements

- Common usings are provided in `GlobalUsings.Specs.cs` (e.g., `using Xunit;`, `using NSubstitute;`), don't add any using statements already in this file.
- Don't add using statement for namespace of the system under test.

## Integration Tests

- Use fixtures like `OrleansFixture` for integration scenarios.
- Use extension methods in `XUnit.Integration/EventsShouldExtensions.cs` for event log assertions.
