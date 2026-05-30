---
title: "1. Your first event"
description: Record the fact that a book arrived at the library, and meet Chronicle's event log.
---

Every library starts the same way: a book shows up. So that's where we'll start too — by recording that fact. By the end of this chapter you'll have written your first event to Chronicle and understood why we reach for an *event* instead of a database row.

## Think in facts, not rows

Your instinct, coming from most databases, is probably to create a `Book` row and `INSERT` it. Hold that thought — because it throws away the most interesting thing: *that the book arrived, and when*. In event sourcing we record what **happened**. The book arriving is a fact, and facts have a few properties: they're immutable (it happened; you can't un-happen it), and they're named in the past tense.

So let's name it. A `BookAdded` event, as a `record` marked with `[EventType]`:

```csharp
using Cratis.Chronicle.Events;

[EventType]
public record BookAdded(string Title, string Isbn);
```

A couple of things worth noticing here:

- `[EventType]` carries **no name** — Chronicle uses the type's name (`BookAdded`) as the event's identity. That's a small thing now, but it's why you'll never hand-maintain a string registry of event names.
- The properties aren't nullable. An event states what *was* true the moment it happened. If you ever find yourself reaching for a nullable property, that's Chronicle nudging you: you probably have a *second* fact hiding in there, and it deserves its own event.

## Give the book an identity

Every event is about *something* — here, a specific book. Chronicle calls that the **event source**, and the events that share a source form that book's own little stream of history. We'll identify a book with a strongly-typed id rather than a bare `Guid`, so the compiler stops us from ever mixing a book's id up with, say, a member's:

```csharp
public record BookId(Guid Value) : ConceptAs<Guid>(Value)
{
    public static BookId New() => new(Guid.NewGuid());
    public static implicit operator EventSourceId(BookId id) => new(id.Value.ToString());
}
```

That last line — the conversion to `EventSourceId` — is what lets you hand a `BookId` straight to Chronicle as the stream key. No ceremony.

## Append it

Now the moment itself. With a `ChronicleClient` connected to your event store, append the event against the book's id:

```csharp
var book = BookId.New();
await eventStore.EventLog.Append(book, new BookAdded("The Pragmatic Programmer", "978-0135957059"));
```

Run it. Nothing dramatic appears on screen — and that's exactly right. Behind that one line, Chronicle validated the event against its registered schema, assigned it the next sequence number, and committed it to the **event log** — permanently, and in order. The fact is now part of your system's history; nothing will ever quietly overwrite it.

:::note[Where did it go?]
The event log is the source of truth — an append-only sequence of everything that has happened. You just wrote position `0`. Every projection, reducer, and reactor you build from here on is, in the end, just a different way of *reading* this log.
:::

## What you did

- Modeled a real-world moment as an **immutable event** (`BookAdded`) instead of a row to be updated later.
- Gave the book a **strongly-typed event source id** so its events form one stream.
- **Appended** that event to the log — your first permanent fact.

A fact you can't query, though, isn't much use yet — right now the book exists only as history. In the [next chapter](./read-model.md) we'll fix that: we'll teach Chronicle to fold this stream of events into a `Books` read model you can actually query, and watch it update itself as more events arrive. [Onward →](./read-model.md)
