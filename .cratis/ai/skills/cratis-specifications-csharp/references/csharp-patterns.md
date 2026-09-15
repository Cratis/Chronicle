<!-- cratis-ai-managed: skills/cratis-specifications-csharp/references/csharp-patterns.md -->
# C# specification patterns

Detail for the plain `Cratis.Specifications` surface: phases, substitution,
assertions, and exception capture. Verified against `Cratis.Specifications`
`4.1.0` and NSubstitute as consumed by that package.

## Phases

| Method | Purpose | Notes |
| --- | --- | --- |
| `void Establish()` | Setup, before `Because()` | One per class in the chain, run base-first. Never call `base.Establish()` |
| `void Because()` | The single action under test | Only in a concrete specification, never in a `given/` context |
| `[Fact] void should_*()` | One assertion per fact | Expression-body form, no blank lines between facts |
| `void Destroy()` | Teardown after each specification | Optional |

Every phase may be `async Task`.

## Layered contexts

```csharp
// given/all_dependencies.cs — substitute every collaborator
public class all_dependencies : Specification
{
    protected <CollaboratorType> _<collaborator>;
    protected <OtherCollaboratorType> _<otherCollaborator>;

    void Establish()
    {
        _<collaborator> = Substitute.For<<CollaboratorType>>();
        _<otherCollaborator> = Substitute.For<<OtherCollaboratorType>>();
    }
}

// given/a_<system_under_test>.cs — build the system under test
public class a_<system_under_test> : all_dependencies
{
    protected <ClassName> _<sut>;

    void Establish() => _<sut> = new(_<collaborator>, _<otherCollaborator>);
}

// when_<behavior>/and_<condition>.cs — the concrete specification
public class and_<condition> : given.a_<system_under_test>
{
    void Because() => _<sut>.<Action>(<input>);

    [Fact] void should_<expected_outcome>() =>
        _<collaborator>.Received(1).<Method>(Arg.Any<<ArgumentType>>());
}
```

A context captures the world as it exists *before* the action. It never
contains the action itself.

## Substitution patterns

```csharp
// Create
_<collaborator> = Substitute.For<<CollaboratorType>>();

// Return values
_<collaborator>.<Method>(Arg.Any<string>()).Returns(<value>);
_<collaborator>.<AsyncMethod>(Arg.Any<int>()).Returns(Task.FromResult(<value>));

// Argument matchers
Arg.Is<<RequestType>>(request => request.Id == _expectedId)

// Verify
_<collaborator>.Received(1).<Method>(Arg.Any<string>());
_<collaborator>.DidNotReceive().<OtherMethod>(Arg.Any<int>());

// Capture an argument
_<collaborator>
    .When(collaborator => collaborator.<Method>(Arg.Any<<ArgumentType>>()))
    .Do(call => _captured = call.Arg<<ArgumentType>>());

// Throw from a substitute
_<collaborator>.<Method>(Arg.Any<<ArgumentType>>())
    .Throws(new <ExceptionType>());
```

## Assertion extension methods

From `Cratis.Specifications`:

| Method | Example |
| --- | --- |
| `.ShouldEqual(expected)` | `_result.ShouldEqual(<expected>)` |
| `.ShouldBeTrue()` / `.ShouldBeFalse()` | `_flag.ShouldBeTrue()` |
| `.ShouldBeNull()` / `.ShouldNotBeNull()` | `_error.ShouldBeNull()` |
| `.ShouldBeEmpty()` / `.ShouldNotBeEmpty()` | `_items.ShouldBeEmpty()` |
| `.ShouldContain(item)` / `.ShouldNotContain(item)` | `_items.ShouldContain(<expected>)` |
| `.ShouldContainOnly(items)` | `_items.ShouldContainOnly(<expectedItems>)` |
| `.ShouldBeOfExactType<T>()` | `_event.ShouldBeOfExactType<<EventType>>()` |
| `.ShouldBeGreaterThan(n)` / `.ShouldBeLessThan(n)` | `_count.ShouldBeGreaterThan(0)` |

Assert on values and types. Never assert on a presentation message string — it
is text, not behavior.

## Catching exceptions

```csharp
Exception? _error;

async Task Because() => _error = await Catch.Exception(_<sut>.<Action>);

[Fact] void should_fail() => _error.ShouldNotBeNull();
[Fact] void should_fail_with_<reason>() =>
    _error.ShouldBeOfExactType<<ExceptionType>>();
```

Use `Catch.Exception` rather than a `try`/`catch` in `Because()`; the captured
exception is the outcome the facts assert on.

## Usings

`GlobalUsings.Specs.cs` supplies `Xunit`, `NSubstitute`, and
`Cratis.Specifications`. Do not repeat them per file, and do not add a using for
the namespace of the system under test.

Order non-aliased namespaces first, then a blank line, then
`using <alias> = …` sorted by alias name. Alias a type whose short name collides
with a namespace segment, choosing a domain-meaningful alias rather than a
technical `Command`, `Event`, or `Component` suffix.

## One outcome per file

```
# Single outcome — one file
for_<ClassName>/when_<behavior>.cs

# Multiple outcomes — a folder
for_<ClassName>/when_<behavior>/
    and_<condition>.cs
    and_<other_condition>.cs
    with_<state>.cs
    without_<requirement>.cs
```

Allowed prefixes: `and_`, `with_`, `without_`, `having_`, `given_`. Each
distinct outcome is its own file, so a failure names the outcome that broke
without reading a multi-assertion file.
