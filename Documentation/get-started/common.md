## Define some events

Everything in Chronicle starts with a fact. You model facts as `record` types marked with `[EventType]` — records because an event, once it happened, never changes. The attribute is how Chronicle discovers the type; it takes no name, the type name *is* the identity.

Here are the facts of a small library — a book arrives, gets borrowed, and comes back:

```csharp
using Cratis.Chronicle.Events;

[EventType]
public record BookAdded(string Title, string Isbn);

[EventType]
public record BookBorrowed(string MemberName);

[EventType]
public record BookReturned;
```

`BookReturned` carries no data at all — that it *happened*, on a particular book's stream, is the whole story. Not every fact needs a payload.

## Append them

You record a fact by **appending** it to an [event sequence](../concepts/event-sequence.md). Chronicle gives you one by default — the **event log**, the main sequence you'll use, much like the `main` branch of a Git repository. Reach it through the event store:

```csharp
var eventLog = eventStore.EventLog;

var bookId = Guid.NewGuid();
await eventLog.Append(bookId, new BookAdded("The Pragmatic Programmer", "978-0135957059"));
```

That first argument is the [event source id](../concepts/event-source.md) — the identity of the thing this fact is about, like a primary key. Every event you append against `bookId` becomes part of *that book's* stream of history.

> [!TIP]
> We use a raw `Guid` here to keep the quickstart short. In real code you'd wrap it in a strongly-typed `BookId` so the compiler can't confuse a book's id with a member's — the [tutorial](/chronicle/tutorial/) shows exactly that.

Run your app, then open the [workbench](http://localhost:8080), pick your event store, and select **Sequences** — your `BookAdded` is sitting there at sequence number `0`, permanent and in order.

![Chronicle Workbench showing events](workbench.png)

## Turn events into a read model

Events are the **write** side — the source of truth. To *read* current state you don't query the log directly; you let Chronicle fold the events into a **read model** for you. The declarative way to do that is a [projection](../concepts/projection.md): you declare the shape you want and which events feed each field, and Chronicle keeps it in sync — no update code, ever.

```csharp
using Cratis.Chronicle.Keys;
using Cratis.Chronicle.Projections.ModelBound;

public record Book(
    [Key]
    Guid Id,

    [SetFrom<BookAdded>(nameof(BookAdded.Title))]
    string Title,

    [SetFrom<BookAdded>(nameof(BookAdded.Isbn))]
    string Isbn,

    [SetValue<BookAdded>(false)]
    [SetValue<BookBorrowed>(true)]
    [SetValue<BookReturned>(false)]
    bool OnLoan,

    [SetFrom<BookBorrowed>(nameof(BookBorrowed.MemberName))]
    string? BorrowedBy);
```

Read the attributes as a sentence: a book's `Title` and `Isbn` come from `BookAdded`; `OnLoan` is `false` when it's added, `true` when borrowed, `false` again when returned; `BorrowedBy` is whoever borrowed it. You're *declaring* how facts map onto the view — Chronicle replays the events in order and applies the mapping.

By default Chronicle **materializes** every projection into the **sink** storage configured for the event store — MongoDB unless you change it — under a database named after the event store and a collection named after the read model. So the `Book` read model is an ordinary MongoDB collection; you read it with the driver:

```csharp
public class Books(IMongoCollection<Book> collection)
{
    public IEnumerable<Book> OnLoan() => collection.Find(b => b.OnLoan).ToList();
}
```

Append a `BookBorrowed` against the same `bookId`, query again, and `OnLoan` is `true` with `BorrowedBy` set; append a `BookReturned` and it flips back. You never wrote an `UPDATE`.

## React when something happens

Projections build *state*. When you need to *do something* the moment a fact lands — notify someone, call another system — you write a **reactor**. `IReactor` is a marker; you just add a method whose first parameter is the event you care about, and Chronicle routes matching events to it:

```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reactors;

public class BookReturnedNotifier : IReactor
{
    public Task BookReturned(BookReturned @event, EventContext context)
    {
        // context.EventSourceId is the BookId this happened to
        Console.WriteLine($"Book {context.EventSourceId} was returned — notify the next member in line.");
        return Task.CompletedTask;
    }
}
```

No registration, no wiring — drop the class in and every `BookReturned` flows to it. Reactors must be idempotent, because the same event may be delivered more than once (during a replay or a recovery). In a real app you'd inject a notification service here — the [tutorial](/chronicle/tutorial/) and the [Reactors](../reactors/index.md) guide show that, along with how reactors get their dependencies under a host.

That's the whole loop — **append → project → react**. The [tutorial](/chronicle/tutorial/) builds exactly this library one concept at a time and explains each as you go; the [Projections](../projections/index.md), [Reducers](../reducers/index.md), and [Reactors](../reactors/index.md) guides go deeper on each piece.
