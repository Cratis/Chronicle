---
uid: Chronicle.Testing.Events.EventSequenceAssertions
---
# Event Sequence Assertions

After appending events through an `EventScenario`, you often need to verify what ended up in the event sequence itself — not just the result of the append call. The `Cratis.Chronicle.Testing` package provides `Should*` extension methods on `IEventSequence` and `IEventLog` for these assertions, as well as `Should*` extension methods on `AppendedEventWithResult` for asserting on collected events.

## Event sequence assertion reference

These methods are available on any `IEventSequence` or `IEventLog` instance, including `scenario.EventLog` and `scenario.EventSequence`.

| Method | Asserts |
|--------|---------|
| `ShouldHaveTailSequenceNumber(expected)` | Tail sequence number matches the expected value |
| `ShouldHaveAppendedEvent<TEvent>()` | At least one event of the given type exists anywhere in the sequence |
| `ShouldHaveAppendedEvent<TEvent>(validator)` | At least one event of the given type exists and passes the validator |
| `ShouldHaveAppendedEvent<TEvent>(predicate)` | At least one event of the given type matches the predicate |
| `ShouldHaveAppendedEvent<TEvent>(eventSourceId)` | At least one event of the given type exists for the event source |
| `ShouldHaveAppendedEvent<TEvent>(eventSourceId, validator)` | At least one event of the given type exists for the event source and passes the validator |
| `ShouldHaveAppendedEvent<TEvent>(eventSourceId, predicate)` | At least one event of the given type matches the predicate for the event source |
| `ShouldHaveAppendedEvent<TEvent>(sequenceNumber)` | An event of the given type exists at the sequence number |
| `ShouldHaveAppendedEvent<TEvent>(sequenceNumber, validator)` | An event of the given type exists at the sequence number and passes the validator |
| `ShouldHaveAppendedEvent<TEvent>(sequenceNumber, predicate)` | An event of the given type at the sequence number matches the predicate |
| `ShouldHaveAppendedEvent<TEvent>(sequenceNumber, eventSourceId, validator)` | An event of the given type exists at the sequence number for the given event source and passes the validator |
| `ShouldHaveAppendedEvent<TEvent>(sequenceNumber, eventSourceId, predicate)` | An event of the given type at the sequence number for the given event source matches the predicate |

All methods are async and return `Task`. They throw `EventSequenceAssertionException` on failure with a descriptive message.

## Verifying the tail sequence number

The tail sequence number is the sequence number of the last event appended to the sequence. Use `ShouldHaveTailSequenceNumber` to verify the expected number of events were appended:

```csharp
var scenario = new EventScenario();

await scenario.EventLog.Append(authorId, new AuthorRegistered("Jane Smith"));
await scenario.EventLog.Append(authorId, new BookAdded("Clean Code"));

await scenario.EventLog.ShouldHaveTailSequenceNumber(1);
```

Sequence numbers are zero-based, so the first event has sequence number `0` and the second has `1`. The special value `EventSequenceNumber.Unavailable` indicates no events have been appended.

## Verifying an appended event by type

Use `ShouldHaveAppendedEvent<TEvent>` without a sequence number to verify that at least one event of the expected type was appended anywhere in the sequence:

```csharp
var scenario = new EventScenario();

await scenario.EventLog.Append(authorId, new AuthorRegistered("Jane Smith"));
await scenario.EventLog.Append(authorId, new BookAdded("Clean Code"));

await scenario.EventLog.ShouldHaveAppendedEvent<AuthorRegistered>();
await scenario.EventLog.ShouldHaveAppendedEvent<BookAdded>();
```

When you know the exact position, pass a sequence number to assert at a specific location:

```csharp
await scenario.EventLog.ShouldHaveAppendedEvent<AuthorRegistered>(0);
await scenario.EventLog.ShouldHaveAppendedEvent<BookAdded>(1);
```

## Verifying event content

Pass a validator action to inspect the event content. The assertion fails if the event is not of the expected type or if the validator throws:

```csharp
var scenario = new EventScenario();

await scenario.EventLog.Append(authorId, new AuthorRegistered("Jane Smith"));

await scenario.EventLog.ShouldHaveAppendedEvent<AuthorRegistered>(0, author =>
    author.Name.ShouldEqual("Jane Smith"));
```

Without a sequence number, the validator runs against the first event of the matching type:

```csharp
await scenario.EventLog.ShouldHaveAppendedEvent<AuthorRegistered>(author =>
    author.Name.ShouldEqual("Jane Smith"));
```

## Verifying with a predicate

Pass a `Func<TEvent, bool>` predicate when you only need to check whether the event satisfies a condition. The assertion fails if no event of the expected type returns `true` from the predicate:

```csharp
var scenario = new EventScenario();

await scenario.EventLog.Append(authorId, new AuthorRegistered("Jane Smith"));

// Without sequence number — finds any matching event
await scenario.EventLog.ShouldHaveAppendedEvent<AuthorRegistered>(
    author => author.Name == "Jane Smith");

// At a specific sequence number
await scenario.EventLog.ShouldHaveAppendedEvent<AuthorRegistered>(0,
    author => author.Name == "Jane Smith");
```

## Verifying event content for a specific event source

When events for multiple event sources exist in the same sequence, filter by event source to avoid ambiguity. All event source overloads support both `Action<TEvent>` validators and `Func<TEvent, bool>` predicates:

```csharp
var scenario = new EventScenario();
var author1 = AuthorId.New();
var author2 = AuthorId.New();

await scenario.EventLog.Append(author1, new AuthorRegistered("Jane Smith"));
await scenario.EventLog.Append(author2, new AuthorRegistered("John Doe"));

// With sequence number and validator
await scenario.EventLog.ShouldHaveAppendedEvent<AuthorRegistered>(0, author1, author =>
    author.Name.ShouldEqual("Jane Smith"));

// Without sequence number — finds any matching event for the event source
await scenario.EventLog.ShouldHaveAppendedEvent<AuthorRegistered>(author2, author =>
    author.Name.ShouldEqual("John Doe"));

// With a predicate instead of a validator
await scenario.EventLog.ShouldHaveAppendedEvent<AuthorRegistered>(
    author1, author => author.Name == "Jane Smith");
```

## AppendedEventWithResult assertions

When using `IEventAppendCollection` to collect events (see [Event Append Collection](../event-append-collection.md)), each captured entry is an `AppendedEventWithResult`. The testing package provides `Should*` extensions for asserting directly on these entries.

### Result assertions

All [append result assertions](assertions.md) are available directly on `AppendedEventWithResult` — they delegate to the inner `Result`:

| Method | Asserts |
|--------|---------|
| `ShouldBeSuccessful()` | Operation succeeded with no violations or errors |
| `ShouldBeFailed()` | Operation failed (any violation or error) |
| `ShouldHaveConstraintViolations()` | At least one constraint violation is present |
| `ShouldNotHaveConstraintViolations()` | No constraint violations are present |
| `ShouldHaveConstraintViolationFor(name)` | A violation for the named constraint is present |
| `ShouldHaveConcurrencyViolations()` | At least one concurrency violation is present |
| `ShouldNotHaveConcurrencyViolations()` | No concurrency violations are present |
| `ShouldHaveErrors()` | At least one error is present |
| `ShouldNotHaveErrors()` | No errors are present |

### Event assertions

| Method | Asserts |
|--------|---------|
| `ShouldHaveEvent<TEvent>(validate?)` | Event content is of the given type; optional validator inspects the content |
| `ShouldBeForEventSource(eventSourceId)` | Event was appended for the given event source |

### Example: asserting on a collected event

```csharp
var collected = Context._appendedEventsCollector.Last;

collected.ShouldBeSuccessful();
collected.ShouldHaveEvent<AuthorRegistered>(author =>
    author.Name.ShouldEqual("Jane Smith"));
collected.ShouldBeForEventSource(authorId);
```

## Full example

The following test pre-seeds state, appends two events, and then verifies both the tail sequence number and individual event content:

```csharp
public class when_registering_an_author_and_adding_a_book
{
    readonly EventScenario _scenario = new();

    [Fact]
    public async Task should_append_both_events_in_order()
    {
        var authorId = AuthorId.New();

        await _scenario.Given
            .ForEventSource(authorId)
            .Events(new LibraryCreated("Main Library"));

        await _scenario.EventLog.Append(authorId, new AuthorRegistered("Jane Smith"));
        await _scenario.EventLog.Append(authorId, new BookAdded("Clean Code"));

        // Tail includes the seeded event (0) plus the two appended events (1, 2)
        await _scenario.EventLog.ShouldHaveTailSequenceNumber(2);

        await _scenario.EventLog.ShouldHaveAppendedEvent<AuthorRegistered>(1, author =>
            author.Name.ShouldEqual("Jane Smith"));
        await _scenario.EventLog.ShouldHaveAppendedEvent<BookAdded>(2, book =>
            book.Title.ShouldEqual("Clean Code"));
    }
}
```

For asserting on the result of an individual append operation, see [Append Assertions](assertions.md).
