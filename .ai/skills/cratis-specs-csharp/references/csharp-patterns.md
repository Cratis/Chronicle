# C# Spec Patterns — Reference

## BDD phases

| Method | Purpose | Notes |
| --- | --- | --- |
| `void Establish()` | Setup — runs before `Because()` | Base-class `Establish` runs first, then derived class |
| `void Because()` | The single action under test | Only in concrete spec files — never in `given/` contexts |
| `[Fact] void should_*()` | One assertion per fact | No blank lines between `should_` methods |
| `void Destroy()` | Teardown after each test | Optional |

All phases can be `async Task`.

---

## Minimal spec

```csharp
namespace MyApp.for_KeyHelper;

public class when_combining_parts : Specification
{
    object[] _parts;
    string _result;

    void Establish() => _parts = ["First", "Second", "Third"];
    void Because() => _result = KeyHelper.Combine(_parts);

    [Fact] void should_combine_all_parts() => _result.ShouldEqual("First+Second+Third");
    [Fact] void should_not_be_empty() => _result.ShouldNotBeEmpty();
}
```

---

## Reusable context (layered given)

```csharp
// given/all_dependencies.cs — mock all external deps
public class all_dependencies : Specification
{
    protected IEventStore _eventStore;
    protected IReactorInvoker _reactorInvoker;

    void Establish()
    {
        _eventStore = Substitute.For<IEventStore>();
        _reactorInvoker = Substitute.For<IReactorInvoker>();
    }
}

// given/a_reactor_handler.cs — build system under test
public class a_reactor_handler : all_dependencies
{
    protected ReactorHandler _handler;

    void Establish() => _handler = new(_eventStore, _reactorInvoker);
}

// when_handling/and_event_is_received.cs — concrete spec
public class and_event_is_received : given.a_reactor_handler
{
    void Because() => _handler.Handle(new SomeEvent());

    [Fact] void should_invoke_reactor() =>
        _reactorInvoker.Received(1).Invoke(Arg.Any<SomeEvent>());
}
```

---

## NSubstitute patterns

```csharp
// Create substitutes
_service = Substitute.For<IMyService>();

// Return values
_service.GetValue(Arg.Any<string>()).Returns("result");
_service.GetAsync(Arg.Any<int>()).Returns(Task.FromResult(42));

// Argument matchers
Arg.Is<Request>(r => r.Id == expectedId && r.Name == expectedName)

// Verify calls
_service.Received(1).DoSomething(Arg.Any<string>());
_service.DidNotReceive().DoSomethingElse();

// Capture arguments
_service.When(s => s.Process(Arg.Any<IDictionary<string, string>>()))
    .Do(info => _captured = info.Arg<IDictionary<string, string>>());

// Throw from substitute
_handler.Handle(Arg.Any<CommandContext>()).Throws(new MyException("fail"));
```

---

## Assertion extension methods (Cratis.Specifications)

| Method | Example |
| --- | --- |
| `.ShouldEqual(expected)` | `_result.ShouldEqual(42)` |
| `.ShouldBeTrue()` | `_flag.ShouldBeTrue()` |
| `.ShouldBeFalse()` | `_flag.ShouldBeFalse()` |
| `.ShouldBeNull()` | `_error.ShouldBeNull()` |
| `.ShouldNotBeNull()` | `_value.ShouldNotBeNull()` |
| `.ShouldBeEmpty()` | `_list.ShouldBeEmpty()` |
| `.ShouldNotBeEmpty()` | `_list.ShouldNotBeEmpty()` |
| `.ShouldContain(item)` | `_list.ShouldContain(expected)` |
| `.ShouldNotContain(item)` | `_list.ShouldNotContain(excluded)` |
| `.ShouldContainOnly(items)` | `_list.ShouldContainOnly(expectedItems)` |
| `.ShouldBeOfExactType<T>()` | `_obj.ShouldBeOfExactType<PropertiesChanged>()` |
| `.ShouldBeGreaterThan(n)` | `_count.ShouldBeGreaterThan(0)` |
| `.ShouldBeLessThan(n)` | `_count.ShouldBeLessThan(100)` |

---

## Catching exceptions

```csharp
Exception? _error;

async Task Because() => _error = await Catch.Exception(_sut.DoSomethingThatThrows);

[Fact] void should_throw() => _error.ShouldNotBeNull();
[Fact] void should_not_throw() => _error.ShouldBeNull();
[Fact] void should_throw_author_not_found() => _error.ShouldBeOfExactType<AuthorNotFound>();
```

---

## Using statements

Common usings are provided globally in `GlobalUsings.Specs.cs` — do **not** add them manually:
- `Xunit`
- `NSubstitute`
- `Cratis.Specifications`

Do **not** add a `using` for the namespace of the system under test.

---

## Multiple outcomes → folder

```
// Single outcome → single file
for_MyService/when_processing.cs

// Multiple outcomes → folder + files
for_MyService/when_processing/
    and_input_is_valid.cs
    and_input_is_null.cs
    with_empty_collection.cs
    without_required_field.cs
```

Allowed file name prefixes: `and_*`, `with_*`, `without_*`, `having_*`, `given_*`

---

## Folder naming read as sentences

`for_AuthorService / when_registering / and_name_already_exists`

→ "For AuthorService, when registering, and name already exists, it should..."
