# Chronicle Integration Specs — Reference

Integration specs test a complete vertical slice end-to-end — from HTTP request through command handling, event appending, constraint checking, and projection — against a real Chronicle event store. If one passes, the entire stack works.

They live under `when_<behavior>/` **inside the slice folder** (not in a `for_<Type>/` unit folder — there's no isolated unit, the entire slice is under test).

---

## Structure

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

---

## Spec with preconditions (seed the event store)

Use `async Task Establish()` to append events before `Because()`:

```csharp
public class context(ChronicleOutOfProcessFixture fixture) : given.an_http_client(fixture)
{
    public const string ExistingName = "John Doe";
    public CommandResult<object>? Result;

    async Task Establish() =>
        await EventStore.EventLog.Append(AuthorId.New(), new AuthorRegistered(ExistingName));

    async Task Because() =>
        Result = await Client.ExecuteCommand<RegisterAuthor>(
            "/api/authors/register",
            new RegisterAuthor(ExistingName));
}

[Fact] void should_not_be_successful() => Context.Result!.IsSuccess.ShouldBeFalse();
[Fact] void should_not_have_appended_additional_events() =>
    Context.ShouldHaveTailSequenceNumber(EventSequenceNumber.First);
```

---

## ExecuteCommand overloads

```csharp
// Command with no typed response (returns CommandResult<object>)
Result = await Client.ExecuteCommand<RegisterAuthor>(url, command);

// Command with typed response (returns CommandResult<TResult>)
Result = await Client.ExecuteCommand<RegisterAuthor, AuthorId>(url, command);
```

---

## Integration assertion helpers

| Helper | What it verifies |
| --- | --- |
| `Context.Result!.IsSuccess.ShouldBeTrue()` | Command succeeded |
| `Context.Result!.IsSuccess.ShouldBeFalse()` | Command failed (validation, constraint, etc.) |
| `Context.ShouldHaveTailSequenceNumber(EventSequenceNumber.First)` | Event log has exactly one event (sequence 0) |
| `Context.ShouldHaveTailSequenceNumber(n)` | Event log tail is at sequence `n` |
| `Context.ShouldHaveAppendedEvent<TEvent>(seq, eventSourceId, validator)` | Specific event was appended at sequence with correct values |

---

## Key rules

- `context` is an **inner public class** inheriting from `given.an_http_client(fixture)`
- Always add `using context = <full.namespace>.context;` alias at the top
- `[Collection(ChronicleCollection.Name)]` on the outer class — required for test isolation
- `Establish` seeds preconditions; `Because` executes the command under test
- Declare `Result` as nullable, initialized to `null!` if needed for nullable analysis
- The outer class constructor receives `context` via xUnit constructor injection
- Never mix unit specs and integration specs in the same folder
