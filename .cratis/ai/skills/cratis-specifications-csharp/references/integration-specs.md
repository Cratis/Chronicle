<!-- cratis-ai-managed: skills/cratis-specifications-csharp/references/integration-specs.md -->
# Out-of-process Chronicle integration specifications

Reserve these for the host, transport, serialization, or real-infrastructure
boundary the in-process scenario helpers cannot reach. They exercise a complete
behavior — HTTP request through command handling, append, constraint checking,
and projection — against a real Chronicle event store.

They live under `when_<behavior>/` **inside the behavior's own folder**, not in a
`for_<Type>/` unit folder: there is no isolated unit, the whole slice is under
test. Never mix unit and integration specifications in one folder.

Verified against `Cratis.Chronicle.Testing` `17.0.0`.

## Structure

```csharp
using context = <RootNamespace>.<Feature>.when_<behavior>.and_<condition>.context;

namespace <RootNamespace>.<Feature>.when_<behavior>;

[Collection(ChronicleCollection.Name)]
public class and_<condition>(context context) : Given<context>(context)
{
    public class context(ChronicleOutOfProcessFixture fixture)
        : given.an_http_client(fixture)
    {
        public CommandResult<<ResponseType>>? Result;

        async Task Because() =>
            Result = await Client.ExecuteCommand<<CommandType>, <ResponseType>>(
                "<route>", new <CommandType>(<arguments>));
    }

    [Fact] void should_succeed() => Context.Result!.IsSuccess.ShouldBeTrue();

    [Fact] void should_append_one_event() =>
        Context.ShouldHaveTailSequenceNumber(EventSequenceNumber.First);

    [Fact] void should_append_<event>() =>
        Context.ShouldHaveAppendedEvent<<EventType>>(
            EventSequenceNumber.First,
            Context.Result!.Response,
            appended => appended.<Property>.Value.ShouldEqual(<expected>));
}
```

## Seeding preconditions

Append events in `async Task Establish()` before `Because()` runs:

```csharp
public class context(ChronicleOutOfProcessFixture fixture)
    : given.an_http_client(fixture)
{
    public const string <ExistingValue> = "<value>";
    public CommandResult<object>? Result;

    async Task Establish() =>
        await EventStore.EventLog.Append(
            <IdentityType>.New(), new <EventType>(<ExistingValue>));

    async Task Because() =>
        Result = await Client.ExecuteCommand<<CommandType>>(
            "<route>", new <CommandType>(<ExistingValue>));
}
```

```csharp
[Fact] void should_not_succeed() => Context.Result!.IsSuccess.ShouldBeFalse();

[Fact] void should_not_append_further_events() =>
    Context.ShouldHaveTailSequenceNumber(EventSequenceNumber.First);
```

## Command execution overloads

```csharp
// No typed response — returns CommandResult<object>?
Result = await Client.ExecuteCommand<<CommandType>>(route, command);

// Typed response — returns CommandResult<TResult>?
Result = await Client.ExecuteCommand<<CommandType>, <ResponseType>>(route, command);
```

## Assertion helpers

| Helper | Verifies |
| --- | --- |
| `Context.Result!.IsSuccess.ShouldBeTrue()` | The command succeeded |
| `Context.Result!.IsSuccess.ShouldBeFalse()` | The command failed |
| `Context.ShouldHaveTailSequenceNumber(EventSequenceNumber.First)` | The log holds exactly one event |
| `Context.ShouldHaveTailSequenceNumber(n)` | The log tail is at sequence `n` |
| `Context.ShouldHaveAppendedEvent<TEvent>(seq, eventSourceId, validator)` | A specific event was appended with the expected values |

Sequence numbers are zero-based: `EventSequenceNumber.First` is `0`, so the tail
after a single append is `0`.

## Asynchronous follow-ups

When a reactor fires after the command and appends further events, collect them
rather than sleeping. Start the collector **before** the triggering action:

```csharp
_collector = StartCollectingAppends();
// … perform the action …
await _collector.WaitForCount(2, TimeSpan.FromSeconds(10));

[Fact] void should_append_<event>() =>
    _collector.ShouldHaveEvent<<EventType>>(appended => appended.<Property> == <expected>);

void Destroy() => _collector.Dispose();
```

`IEventAppendCollection` is part of the Chronicle testing API. The timeout is a
deadline, not a sleep — it turns a hang into a named failure.

## Rules

- `context` is an inner `public class` inheriting `given.an_http_client(fixture)`.
- Add the `using context = <full.namespace>.context;` alias at the top of the
  file.
- `[Collection(ChronicleCollection.Name)]` goes on the outer class and is
  required for isolation.
- `Establish` seeds preconditions; `Because` performs the action under test.
- Declare `Result` nullable.
- The outer class receives `context` through xUnit constructor injection.
