---
applyTo: "**/for_*/**/*.cs, **/when_*/**/*.cs"
---

# How to Write C# Specs

Extends the base [Specs Instructions](./specs.instructions.md) with C#-specific conventions.

The `Cratis.Specifications` library was built to maintain the approach, structure, and syntax of Machine.Specifications (MSpec) — a BDD framework that makes specs read like a human-language specification. The `Establish → Because → should_` flow maps directly to "Given → When → Then" and keeps specs focused on *one action, one setup, one set of assertions*.

## Frameworks

- [xUnit](https://xunit.net/) for test execution.
- [NSubstitute](https://nsubstitute.github.io/) for mocking — chosen for its clean API that reads naturally.
- [Cratis.Specifications](https://github.com/Cratis/Specifications) for BDD-style specification by example.
- Spec projects are named `<Source>.Specs` (e.g. `Infrastructure.Specs`).

## Base Class

`Specification` from `Cratis.Specifications` must be at the root of every spec's inheritance chain.
It discovers `Establish`, `Because`, and `Destroy` methods by convention — no attributes needed.

## BDD Pattern

The three-phase pattern makes every spec self-explanatory: `Establish` sets up the world, `Because` performs the single action under test, and `should_*` facts verify individual outcomes. No test framework attributes are needed on `Establish` or `Because` — `Cratis.Specifications` discovers them by convention.

```csharp
public class when_combining_parts : Specification
{
    object[] _parts;
    string _result;

    void Establish() => _parts = ["First", "Second", "Third"];

    void Because() => _result = KeyHelper.Combine(_parts);

    [Fact] void should_combine_all_parts() => _result.ShouldEqual("First+Second+Third");
}
```

| Method | Purpose | Notes |
|---|---|---|
| `void Establish()` | Setup — called before `Because()` | Each class in the chain gets its own, called base-first |
| `void Because()` | The single action under test | Only in concrete spec files, never in reusable contexts |
| `[Fact] void should_*()` | One assertion per fact | Use `ShouldXxx()` extension methods |
| `void Destroy()` | Teardown after each test | |

All methods can be `async Task` when needed.

## Reusable Context Pattern

Layered contexts build on each other like building blocks. The base layer mocks all dependencies, the next layer creates the system under test, and each spec only adds what's unique to its scenario. This eliminates setup duplication while keeping every spec self-contained.

```csharp
// for_Changeset/given/a_changeset.cs
namespace MyApp.Changes.for_Changeset.given;

public class a_changeset : Specification
{
    protected IObjectComparer _comparer;
    protected Changeset<object, ExpandoObject> _changeset;

    void Establish()
    {
        _comparer = Substitute.For<IObjectComparer>();
        _changeset = new(_comparer, new object(), new ExpandoObject());
    }
}
```

```csharp
// for_Changeset/when_adding_changes/and_there_are_differences.cs
namespace MyApp.Changes.for_Changeset.when_adding_changes;

public class and_there_are_differences : given.a_changeset
{
    bool _result;

    void Establish() =>
        _comparer.Compare(Arg.Any<object>(), Arg.Any<ExpandoObject>(), out Arg.Any<IEnumerable<PropertyDifference>>())
            .Returns(true);

    void Because() => _result = _changeset.HasChanges;

    [Fact] void should_have_changes() => _result.ShouldBeTrue();
}
```

**Layered givens (chaining contexts):**

```csharp
// given/all_dependencies.cs — mocks all deps
public class all_dependencies : Specification
{
    protected IEventStore _eventStore;
    protected IReactorInvoker _reactorInvoker;
    void Establish() { _eventStore = Substitute.For<IEventStore>(); /* ... */ }
}

// given/a_reactor_handler.cs — builds the system under test
public class a_reactor_handler : all_dependencies
{
    protected ReactorHandler _handler;
    void Establish() => _handler = new(_eventStore, _reactorInvoker);
}
```

## NSubstitute Patterns

```csharp
// Create substitutes
_service = Substitute.For<IMyService>();

// Setup returns
_service.GetValue(Arg.Any<string>()).Returns("result");
_service.ProcessAsync(Arg.Any<int>()).Returns(Task.FromResult(42));

// Argument matchers
Arg.Is<Request>(r => r.Id == expectedId && r.Name == expectedName)

// Verify received calls
_service.Received(1).DoSomething(Arg.Any<string>());
_service.DidNotReceive().DoSomethingElse(Arg.Any<int>());

// Capture arguments
_service.When(s => s.Add(Arg.Any<IDictionary<string, string>>()))
    .Do(info => _captured = info.Arg<IDictionary<string, string>>());

// Throw exceptions
_handler.Handle(Arg.Any<CommandContext>()).Throws(new Exception("fail"));
```

## Assertion Extension Methods

From `Cratis.Specifications`:

| Method | Usage |
|---|---|
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
| `.ShouldBeOfExactType<T>()` | `_obj.ShouldBeOfExactType<PropertiesChanged<ExpandoObject>>()` |
| `.ShouldBeGreaterThan(n)` | `_count.ShouldBeGreaterThan(0)` |
| `.ShouldBeLessThan(n)` | `_count.ShouldBeLessThan(100)` |

**Catching exceptions:**

```csharp
async Task Because() => _error = await Catch.Exception(_sut.DoSomething);

[Fact] void should_not_fail() => _error.ShouldBeNull();
```

## Using Statements

- Common usings are provided globally in `GlobalUsings.Specs.cs` (`Xunit`, `NSubstitute`, `Cratis.Specifications`, etc.) — don't duplicate them.
- Don't add a using statement for the namespace of the system under test.

## Properties — What NOT to Spec

Simple properties are compiler-verified — the type system already guarantees they work. Writing specs for them adds maintenance cost without catching real bugs. Save spec effort for code where errors actually hide: business logic, coordination between dependencies, and complex transformations.

```csharp
// ❌ Do NOT write specs for these
public string TableName => tableName;                         // Returns constructor parameter
public Key Key => key;                                        // Returns field
public IEnumerable<Property> Properties => mapper.Properties; // Simple delegation

// ✅ Only write specs for complex business logic in properties
public decimal TotalCost => Items.Sum(i => i.Cost * i.Quantity * (1 + i.TaxRate));
```

---

## Chronicle Integration Specs

Integration specs are the highest-value specs in the project. They test a complete vertical slice — from HTTP request through command handling, event appending, constraint checking, and projection — against a real Chronicle event store. If one of these passes, the entire stack works.

They live under `when_<behavior>/` folders directly inside the slice folder — **no `for_` folder**, because there's no isolated unit under test. The "unit" is the entire slice.

### Structure

```csharp
using context = MyApp.Authors.Registration.when_registering.and_name_already_exists.context;

namespace MyApp.Authors.Registration.when_registering;

[Collection(ChronicleCollection.Name)]
public class and_name_already_exists(context context) : Given<context>(context)
{
    public class context(ChronicleOutOfProcessFixture fixture) : given.an_http_client(fixture)
    {
        public const string AuthorName = "John Doe";
        public CommandResult<object>? Result;

        async Task Establish() =>
            await EventStore.EventLog.Append(AuthorId.New(), new AuthorRegistered(AuthorName));

        async Task Because()
        {
            Result = await Client.ExecuteCommand<RegisterAuthor>(
                "/api/authors/register",
                new RegisterAuthor(AuthorName));
        }
    }

    [Fact] void should_not_be_successful() => Context.Result.IsSuccess.ShouldBeFalse();
    [Fact] void should_have_appended_only_one_event() => Context.ShouldHaveTailSequenceNumber(EventSequenceNumber.First);
}
```

### Key Rules

- `context` is an inner public class inheriting from `given.an_http_client(fixture)`.
- Use `async Task Establish()` to seed the event store with preconditions.
- Use `async Task Because()` to execute the command under test.
- `ExecuteCommand<TCommand>(url, cmd)` returns `CommandResult<object>?` (single type parameter).
- `ExecuteCommand<TCommand, TResult>(url, cmd)` returns `CommandResult<TResult>?` (two type parameters for typed response).
- Declare `Result` initialized with `null!` to satisfy nullable analysis.
- Add a `using context = ...` alias at the top of each spec file.

### Integration Assertion Helpers

| Assertion | Purpose |
|---|---|
| `Context.Result.IsSuccess.ShouldBeTrue()` | Command succeeded |
| `Context.Result.IsSuccess.ShouldBeFalse()` | Command failed (validation, constraint, etc.) |
| `Context.ShouldHaveTailSequenceNumber(n)` | Verify event log tail (First = sequence 0) |
| `Context.ShouldHaveAppendedEvent<TEvent>(seq, eventSourceId, validator)` | Verify specific event was appended |

## Formatting

- Don't break long `should_` method lines — prefer one-line lambda assertions.
- Don't add blank lines between multiple `should_` methods.
- Simulate failures by mocking `SaveChanges` to throw exceptions.
