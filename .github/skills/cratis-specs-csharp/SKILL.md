---
name: cratis-specs-csharp
description: Step-by-step guidance for writing C# specs in Cratis using BDD-style Specification by Example — the Establish/Because/should_ pattern, for_/when_/and_ folder hierarchy, reusable given/ contexts, NSubstitute mocking, and Chronicle integration specs. Use when writing C# unit or integration specs, creating spec files or folders, structuring the for_<Type>/when_<behavior>/and_<condition> hierarchy, mocking with NSubstitute, writing given/ reusable contexts, or using ShouldEqual/ShouldBeTrue assertions. For integration specs specifically tied to a vertical slice command, write-specs offers a more focused workflow.
---

## Core philosophy

Specs are **executable documentation** — the folder tree reads like a spec sheet. Favor readability over DRY. Each spec file has:
- One action under test (`Because`)
- One setup (`Establish`)
- One or more focused assertions (`should_*`)

---

## Step 1 — Choose the spec type

| Scenario | Spec type |
| --- | --- |
| Isolated unit logic (no I/O) | Unit spec in `for_<ClassName>/` |
| A full vertical slice (command → event) | Chronicle integration spec in slice folder |
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

## Step 5 — Write a Chronicle integration spec

Integration specs live directly inside the slice's `when_<behavior>/` folder and test the full stack against a real Chronicle event store.

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
