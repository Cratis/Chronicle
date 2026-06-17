---
name: cratis-specs-csharp
description: Step-by-step guidance for writing C# specs in Cratis with BDD Specification by Example — the Establish/Because/should_ pattern, for_/when_/and_ folder hierarchy, reusable given/ contexts, NSubstitute mocking, and the in-process scenario family. Use when writing C# unit or integration specs or structuring the for_/when_/and_ hierarchy. For specs tied to a specific vertical-slice command, write-specs is the focused workflow.
---

## Core philosophy

Specs are **executable documentation** — the folder tree reads like a spec sheet. Favor readability over DRY. Each spec file has:
- One action under test (`Because`)
- One setup (`Establish`)
- One or more focused assertions (`should_*`)

---

## Step 1 — Choose the spec type

Lead with the in-process **scenario family** (fast, infrastructure-free — the default for slice behavior); reserve out-of-process Chronicle integration specs for host/transport boundaries they can't reach. **Every spec file is wrapped in `#if DEBUG … #endif`.** Full reference: the universal base in [specs.csharp.md](../../rules/specs.csharp.md) and the application `*Scenario` family in [specs.scenarios.csharp.md](../../rules/specs.scenarios.csharp.md).

| Scenario | Spec type |
| --- | --- |
| State Change slice (command → events) | `CommandScenario<TCommand>` — runs validators + `Provide()` + `Handle()` + appended events |
| State View slice (projection / reducer) | `ReadModelScenario<TReadModel>` |
| Constraints / raw append & concurrency semantics | `EventScenario` |
| Automation / Translation (reactor) | `ReactorScenario<TReactor>` |
| Isolated unit logic (no I/O) | Unit spec in `for_<ClassName>/` |
| Host / transport / real-infra boundary (advanced) | out-of-process Chronicle integration spec |
| Complex setup shared across many specs | Reusable context in `given/` |

---

## Step 2 — Create the folder structure

```
for_<ClassName>/
├── given/
│   ├── all_dependencies.cs       ← mocks all deps, inherits Specification
│   └── a_<system_under_test>.cs  ← creates SUT, inherits all_dependencies
├── when_<behavior>/              ← behavior with multiple outcomes
│   ├── and_<condition>.cs
│   └── with_<state>.cs
└── when_<simple_behavior>.cs     ← single outcome = single file
```

Folder/file names read as English sentences:
- `for_Changeset / when_adding_changes / and_there_are_differences`
- `for_AuthorService / when_registering / and_name_already_exists`

---

## Step 3 — Write a spec

```csharp
// for_KeyHelper/when_combining_parts.cs
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

Rules:
- Inherit `Specification` (from `Cratis.Specifications`)
- `void Establish()` — setup before the action
- `void Because()` — **one** action under test (the thing being specified)
- `[Fact] void should_*()` — one assertion per fact, no blank lines between them
- All fields: `private` (or `protected` in `given/` contexts), `_camelCase`
- All methods can be `async Task` when needed

---

## Step 4 — Add a reusable context (`given/`)

When multiple specs share the same setup, extract it into a `given/` class:

```csharp
// for_AuthorService/given/all_dependencies.cs
namespace MyApp.Authors.for_AuthorService.given;

public class all_dependencies : Specification
{
    protected IEventLog _eventLog;
    protected ILogger<AuthorService> _logger;

    void Establish()
    {
        _eventLog = Substitute.For<IEventLog>();
        _logger = Substitute.For<ILogger<AuthorService>>();
    }
}
```

```csharp
// for_AuthorService/given/an_author_service.cs
namespace MyApp.Authors.for_AuthorService.given;

public class an_author_service : all_dependencies
{
    protected AuthorService _service;

    void Establish() => _service = new(_eventLog, _logger);
}
```

```csharp
// for_AuthorService/when_registering/and_name_is_valid.cs
namespace MyApp.Authors.for_AuthorService.when_registering;

public class and_name_is_valid : given.an_author_service
{
    void Because() => _service.Register(new AuthorName("John"));

    [Fact] void should_append_event() =>
        _eventLog.Received(1).Append(Arg.Any<AuthorId>(), Arg.Any<AuthorRegistered>());
}
```

See `references/csharp-patterns.md` for full NSubstitute patterns and assertion methods.

---

## Step 5 — In-process scenario specs (the default)

For slice behavior, use the scenario family — it runs the real Arc/Chronicle pipeline in-process. Wrap every file in `#if DEBUG`.

```csharp
// Authors/Registration/when_registering_an_author/and_all_information_is_valid.cs
#if DEBUG
namespace MyApp.Authors.Registration.when_registering_an_author;

public class and_all_information_is_valid : Specification
{
    readonly CommandScenario<RegisterAuthor> _scenario = new();
    readonly AuthorId _id = AuthorId.New();
    CommandResult _result;

    async Task Because() => _result = await _scenario.Execute(new RegisterAuthor(_id, new AuthorName("Jane Austen")));

    [Fact] void should_succeed() => _result.ShouldBeSuccessful();
    [Fact] async Task should_have_appended_registered_event() =>
        await _scenario.ShouldHaveAppendedEvent<RegisterAuthor, AuthorRegistered>(_id, e => e.Name == "Jane Austen");
}
#endif
```

`CommandScenario<TCommand>` exposes only `Services`, `Context`, `Execute`, and `Validate` — event assertions are the extension methods `await _scenario.ShouldHaveAppendedEvent<TCommand, TEvent>(eventSourceId[, predicate])` and `ShouldHaveTailSequenceNumber<TCommand>(...)`. Unhappy-path specs assert **both** `ShouldNotBeSuccessful()` and `ShouldHaveValidationErrors()` (authorization uses `ShouldNotBeAuthorized()`). Seed DCB read-model state by registering it into `_scenario.Services` (substitute `IReadModels`/`GetInstanceById`, or `AddReadModels(...)`) — there is no `Given`/`Events` on a command scenario. See [specs.scenarios.csharp.md](../../rules/specs.scenarios.csharp.md) for `EventScenario`, `ReadModelScenario<T>`, and `ReactorScenario<T>` (which *do* use `Given.ForEventSource(...).Events(...)`).

## Step 6 — Out-of-process Chronicle integration spec (advanced)

Reserve this for the host/transport boundary the scenario helpers can't reach. Integration specs live directly inside the slice's `when_<behavior>/` folder and test the full stack against a real Chronicle event store.

```csharp
// Authors/Registration/when_registering/and_there_are_no_authors.cs
using context = MyApp.Authors.Registration.when_registering.and_there_are_no_authors.context;

namespace MyApp.Authors.Registration.when_registering;

[Collection(ChronicleCollection.Name)]
public class and_there_are_no_authors(context context) : Given<context>(context)
{
    public class context(ChronicleOutOfProcessFixture fixture) : given.an_http_client(fixture)
    {
        public CommandResult<AuthorId>? Result;

        async Task Because() =>
            Result = await Client.ExecuteCommand<RegisterAuthor, AuthorId>(
                "/api/authors/register",
                new RegisterAuthor(new AuthorName("John Doe")));
    }

    [Fact] void should_be_successful() => Context.Result!.IsSuccess.ShouldBeTrue();
    [Fact] void should_have_appended_one_event() =>
        Context.ShouldHaveTailSequenceNumber(EventSequenceNumber.First);
    [Fact] void should_append_author_registered_event() =>
        Context.ShouldHaveAppendedEvent<AuthorRegistered>(
            EventSequenceNumber.First, Context.Result!.Response,
            evt => evt.Name.Value.ShouldEqual("John Doe"));
}
```

See `references/integration-specs.md` for the full integration spec guide.

---

## What NOT to spec

- Simple auto-properties (`public AuthorId Id { get; }`)
- Properties returning constructor parameters
- Simple delegation (`public IEnumerable<Author> All => _list;`)
- Logging calls
- Trivial null checks

---

## Reference files

- `references/csharp-patterns.md` — BDD pattern detail, NSubstitute, assertions, exception catching
- `references/integration-specs.md` — Chronicle integration spec structure, helpers, Given<context>
