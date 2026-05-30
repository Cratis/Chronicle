---
title: "2. Building a read model"
description: Project a stream of events into a queryable read model — declaratively, with no update code.
---

**What you'll do:** turn `BookAdded` (and a couple of new events) into a `Books` read model that always reflects the current state of every book — without writing a single "update this row" statement.

## The idea

An event log is the source of truth, but it's not convenient to query ("is this book on loan?" shouldn't replay every event each time). A **read model** is a purpose-built view derived from events. You don't update it by hand — you *declare* how events map onto it, and Chronicle keeps it current as events arrive. This is a [projection](/chronicle/concepts/projection/).

## Add the events that change a book's state

A book can be borrowed and returned. Those are facts too:

```csharp
[EventType]
public record BookBorrowed(string MemberName);

[EventType]
public record BookReturned;
```

## Declare the read model

Define the shape you want to query, mark it `[ReadModel]`, and tell it which events feed each property. AutoMap matches event properties to read-model properties by name, so `BookAdded.Title` flows into `Title` with no extra code:

```csharp
[ReadModel]
public record Book(
    [property: Key] BookId Id,
    string Title,
    string Isbn,
    bool OnLoan,
    string? BorrowedBy)
{
    static Book On(BookAdded @event) => /* mapped from BookAdded */ default!;
    static Book On(BookBorrowed @event) => /* sets OnLoan = true, BorrowedBy = MemberName */ default!;
    static Book On(BookReturned @event) => /* sets OnLoan = false, BorrowedBy = null */ default!;
}
```

:::tip
Prefer the model-bound attributes (`[ReadModel]`, `[FromEvent<T>]`, `[Key]`) over writing imperative update code. When a mapping is genuinely too complex to express declaratively, reach for a [reducer](/chronicle/reducers/) instead — but try the projection first.
:::

## Query it

The projection writes to a store (MongoDB by default), so you query it like any collection:

```csharp
public class Books(IMongoCollection<Book> collection)
{
    public IEnumerable<Book> OnLoan() =>
        collection.Find(b => b.OnLoan).ToList();
}
```

Append a `BookBorrowed` for a book and query again — `OnLoan` is now `true`, with no update code anywhere. That's the projection doing its job.

## What you did

- Added the events that represent a book's lifecycle (`BookBorrowed`, `BookReturned`).
- **Declared** a `Books` read model and how events map onto it — no hand-written updates.
- **Queried** the read model as ordinary data.

The read side is now live. In the [final chapter](./reacting.md) you'll *do something* when a book is returned — send a notification — using a reactor.
