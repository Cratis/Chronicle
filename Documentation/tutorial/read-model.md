---
title: "2. Building a read model"
description: Turn the library's stream of events into a queryable Books read model — declaratively, with no update code.
---

We can record that a book arrived, but the librarian can't *see* the catalog yet — the books live only as history in the log. In this chapter we'll fix that: we'll build a `Books` read model that always reflects the current state of every book, and — here's the part that surprises people coming from CRUD — we'll do it **without writing a single line that updates anything**.

## First, a couple more facts

A book doesn't just arrive; it gets borrowed and brought back. Those are facts too, so they're events:

```csharp
[EventType]
public record BookBorrowed(string MemberName);

[EventType]
public record BookReturned;
```

Notice `BookReturned` has no data at all — and that's fine. The fact that it *happened*, on a particular book's stream, at a particular time, is the whole story. Not every event needs a payload.

## Declare what you want to read

Here's the shift. In a database you'd write code to keep a `Books` table in sync — insert on add, update a flag on borrow, update it back on return. In Chronicle you instead **declare the shape you want** and tell it which events feed it. Chronicle does the keeping-in-sync for you. That declaration is a [projection](/chronicle/concepts/projection/):

```csharp
[ReadModel]
public record Book(
    [property: Key] BookId Id,
    string Title,
    string Isbn,
    bool OnLoan,
    string? BorrowedBy)
{
    static Book On(BookAdded e) => /* a new book; OnLoan = false */ default!;
    static Book On(BookBorrowed e) => /* OnLoan = true, BorrowedBy = e.MemberName */ default!;
    static Book On(BookReturned e) => /* OnLoan = false, BorrowedBy = null */ default!;
}
```

Read those three `On` methods as a sentence: *when a book is added, it's a new, available book; when it's borrowed, mark it on loan to that member; when it's returned, it's available again.* You're describing how each fact changes the view — not writing imperative updates, not worrying about ordering. Chronicle replays the events in order and applies your mapping.

:::tip[Reach for the declarative path first]
For mappings like this — "events map onto fields" — the model-bound attributes (`[ReadModel]`, `[FromEvent<T>]`, AutoMap) express it with almost no code. Only when a view genuinely needs hand-written, imperative folding should you drop to a [reducer](/chronicle/reducers/). Try the projection first; you'll rarely need more.
:::

## Query it

The projection writes the `Book` read model to a store (MongoDB by default), so reading it is just a query — exactly what you're used to:

```csharp
public class Books(IMongoCollection<Book> collection)
{
    public IEnumerable<Book> OnLoan() => collection.Find(b => b.OnLoan).ToList();
}
```

Now exercise it. Append a `BookBorrowed` for your book and query again — `OnLoan` is `true`, and `BorrowedBy` has the member's name. Append a `BookReturned` and it flips back. You never wrote an `UPDATE`. The projection did it, by re-deriving the book from its events.

:::note[A heartbeat of delay]
The read model updates *just after* the event is appended, not in the same instant — it's [eventually consistent](/chronicle/read-models/). For a UI that's usually imperceptible (and you can subscribe to live updates). It only matters when you need to make a decision based on current state — which is what constraints and the next chapter's tools are for.
:::

## What you did

- Added the events that make up a book's life (`BookBorrowed`, `BookReturned`).
- **Declared** a `Books` read model and how events map onto it — no update code anywhere.
- **Queried** it like ordinary data, and watched it stay correct on its own.

You can now see the catalog. The last piece is to make the library *do* something when the world changes — when a book comes back, tell the next person waiting for it. That's a job for a reactor, and it's the [final chapter](./reacting.md). [Let's finish the tour →](./reacting.md)
