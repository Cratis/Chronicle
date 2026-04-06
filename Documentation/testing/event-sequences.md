---
uid: Chronicle.Testing.EventSequences
---
# Testing Event Sequences

`EventScenario` is a lightweight, in-process test utility for exercising code that appends events to an `IEventLog` or `IEventSequence`. It runs entirely in memory with no Chronicle server, database, or network required.

## Why use EventScenario

Chronicle integration tests against a live server are accurate but slow and require infrastructure. `EventScenario` lets you verify that your domain code appends the right events, handles constraint violations correctly, and reacts to pre-seeded state — all in a fast, isolated, and infrastructure-free way.

## Installation

`EventScenario` is in the `Cratis.Chronicle.Testing` NuGet package:

```bash
dotnet add package Cratis.Chronicle.Testing
```

## Basic usage

```csharp
var scenario = new EventScenario();
var result = await scenario.EventLog.Append(authorId, new AuthorRegistered("John Doe"));
result.ShouldBeSuccessful();
```

`EventLog` and `EventSequence` are backed by the same in-memory store. Use `EventLog` for domain tests (it maps to `EventSequenceId.Log`). Use `EventSequence` when you need a generic `IEventSequence` reference.

## Pre-seeding state with Given

Use the fluent `Given` builder to put the in-memory event log into a known state **before** running the act phase:

```csharp
var scenario = new EventScenario();

await scenario.Given
    .ForEventSource(authorId)
    .Events(new AuthorRegistered("John Doe"), new BookAdded("Clean Code"));

var result = await scenario.EventLog.Append(authorId, new BookAdded("The Pragmatic Programmer"));
result.ShouldBeSuccessful();
```

Chain multiple `ForEventSource` calls to seed events for different event sources in the same scenario:

```csharp
await scenario.Given
    .ForEventSource(author1Id)
    .Events(new AuthorRegistered("Jane Smith"));

await scenario.Given
    .ForEventSource(author2Id)
    .Events(new AuthorRegistered("John Doe"));
```

## Asserting on the result

Every `Append` and `AppendMany` call returns an `IAppendResult`. Use the built-in `Should*` extension methods to assert on it:

| Method | Asserts |
|--------|---------|
| `result.ShouldBeSuccessful()` | Operation succeeded with no violations or errors |
| `result.ShouldBeFailed()` | Operation failed (any violation or error) |
| `result.ShouldHaveConstraintViolations()` | At least one constraint violation is present |
| `result.ShouldNotHaveConstraintViolations()` | No constraint violations are present |
| `result.ShouldHaveConstraintViolationFor(name)` | A violation for the named constraint is present |
| `result.ShouldHaveConcurrencyViolations()` | At least one concurrency violation is present |
| `result.ShouldNotHaveConcurrencyViolations()` | No concurrency violations are present |
| `result.ShouldHaveErrors()` | At least one error is present |
| `result.ShouldNotHaveErrors()` | No errors are present |

All `Should*` methods throw `AppendResultAssertionException` on failure, including a description of all violations or errors found.

## Testing constraint violations

Seed the pre-existing state with `Given` and then assert that a second append is rejected:

```csharp
var scenario = new EventScenario();

// Pre-seed: this author name is already taken
await scenario.Given
    .ForEventSource(existingAuthorId)
    .Events(new AuthorRegistered("John Doe"));

// Act: attempt to register the same name for a different event source
var result = await scenario.EventLog.Append(newAuthorId, new AuthorRegistered("John Doe"));

result.ShouldBeFailed();
result.ShouldHaveConstraintViolationFor("UniqueAuthorName");
```

## Testing AppendMany

`AppendMany` appends a collection of events in one call and returns an `AppendManyResult`:

```csharp
var scenario = new EventScenario();
var result = await scenario.EventLog.AppendMany(cartId, [
    new ItemAddedToCart(itemId1),
    new ItemAddedToCart(itemId2),
    new ItemQuantityAdjusted(itemId1, 3)
]);
result.ShouldBeSuccessful();
```

## Isolation

Create a **new `EventScenario` instance per test** to keep tests isolated. The in-memory store accumulates state across calls on the same instance. Each instance starts with an empty event log and sequence number zero.

## Example: full test

```csharp
public class when_adding_a_book_to_an_author
{
    readonly EventScenario _scenario = new();

    [Fact]
    public async Task and_the_author_is_registered()
    {
        var authorId = AuthorId.New();

        await _scenario.Given
            .ForEventSource(authorId)
            .Events(new AuthorRegistered("Jane Smith"));

        var result = await _scenario.EventLog.Append(authorId, new BookAdded("Clean Code"));

        result.ShouldBeSuccessful();
    }
}
```
