---
title: "1. Your first event"
description: Define an event as an immutable fact and append it to the event log.
---

**What you'll do:** model "a book was added to the library" as an event, and append it. By the end you'll have written your first fact to Chronicle.

## Think in facts, not state

In a CRUD system you'd insert a `Book` row. In event sourcing you record what *happened*: a book was added. That fact is an **event** — immutable, named in the past tense, carrying only what was true at the moment it occurred.

Define it as a `record` marked with `[EventType]`:

```csharp
using Cratis.Chronicle.Events;

[EventType]
public record BookAdded(string Title, string Isbn);
```

:::note
`[EventType]` takes no arguments — Chronicle uses the type name as the identity. The properties are never nullable: an event states what *is* true. If something is optional, that's a sign you need a *second* event, not a nullable field.
:::

## Identify the thing it happened to

Every event is about an **event source** — here, a specific book. The event source id is the key Chronicle uses to group a book's events into its own stream. Use a strongly-typed id rather than a raw `Guid`:

```csharp
public record BookId(Guid Value) : ConceptAs<Guid>(Value)
{
    public static BookId New() => new(Guid.NewGuid());
    public static implicit operator EventSourceId(BookId id) => new(id.Value.ToString());
}
```

## Append it

With a `ChronicleClient` connected to your event store, append the event to the event log against the book's id:

```csharp
var book = BookId.New();
await eventStore.EventLog.Append(book, new BookAdded("The Pragmatic Programmer", "978-0135957059"));
```

That's it — the fact is stored, permanently and in order. Run your app; the event is now in the log.

## What you did

- Modeled a change as an **immutable event** (`BookAdded`) instead of a row update.
- Gave the book a **strongly-typed event source id**.
- **Appended** the event to its stream in the event log.

Right now the event just sits there. In the [next chapter](./read-model.md) you'll turn a stream of these events into a `Books` read model you can actually query.
