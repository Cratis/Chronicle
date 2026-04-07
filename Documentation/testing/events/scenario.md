---
uid: Chronicle.Testing.Events.Scenario
---
# EventScenario

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

**Rules:**
- Call `Given` before the act phase — seeded events get monotonically increasing sequence numbers.
- Do not put the act under test inside `Given`; only pre-existing state goes here.

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

For information on asserting the result of an append operation, see [Assertions](assertions.md).
