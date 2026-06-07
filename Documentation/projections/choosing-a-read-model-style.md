---
title: Choose a read-model style
description: Compare model-bound projections, declarative projections, and reducers by building the same Chronicle read model three ways.
---

Every read model answers the same question: "given the events so far, what should this screen or
workflow read now?" Chronicle gives you more than one way to express that answer because different
problems read better in different shapes.

Let's keep one library read model up to date. A book is registered, borrowed, and returned:

```csharp
using Cratis.Chronicle.Events;

[EventType]
public record BookRegistered(string Title, string Isbn);

[EventType]
public record BookBorrowed(string MemberName);

[EventType]
public record BookReturned();
```

The read model we want is deliberately small:

```csharp
public record BookStatus(
    string Id,
    string Title,
    string Isbn,
    bool IsBorrowed,
    string? BorrowedBy);
```

## Model-bound projection: put the mapping on the model

For simple property mapping, model-bound projections are the shortest route. The read model declares how
events populate it:

```csharp
using Cratis.Chronicle.Keys;
using Cratis.Chronicle.Projections.ModelBound;

public record BookStatus(
    [Key] string Id,

    [SetFrom<BookRegistered>] string Title,
    [SetFrom<BookRegistered>] string Isbn,

    [SetValue<BookBorrowed>(true)]
    [SetValue<BookReturned>(false)]
    bool IsBorrowed,

    [SetFrom<BookBorrowed>(nameof(BookBorrowed.MemberName))]
    [SetValue<BookReturned>(null)]
    string? BorrowedBy);
```

Chronicle discovers the projection from the attributes. There is no separate projection class, and the
mapping sits directly next to the read model the UI or query will read. Reach for this first when the
events set, add, subtract, count, or clear properties in a way the attributes can express.

## Declarative projection: separate the mapping from the model

When the mapping needs to be more explicit, keep the read model clean and define the projection with
`IProjectionFor<T>`:

```csharp
using Cratis.Chronicle.Projections;

public class BookStatusProjection : IProjectionFor<BookStatus>
{
    public void Define(IProjectionBuilderFor<BookStatus> builder) => builder
        .From<BookRegistered>(_ => _
            .Set(m => m.Id).ToEventSourceId()
            .Set(m => m.Title).To(e => e.Title)
            .Set(m => m.Isbn).To(e => e.Isbn)
            .Set(m => m.IsBorrowed).ToValue(false)
            .Set(m => m.BorrowedBy).ToValue(null))
        .From<BookBorrowed>(_ => _
            .Set(m => m.IsBorrowed).ToValue(true)
            .Set(m => m.BorrowedBy).To(e => e.MemberName))
        .From<BookReturned>(_ => _
            .Set(m => m.IsBorrowed).ToValue(false)
            .Set(m => m.BorrowedBy).ToValue(null));
}
```

This produces the same `BookStatus` documents as the model-bound version, but the trade-off is different:
more code, more room for explicit mapping, and the read model stays free of Chronicle attributes. Use it
when a projection needs joins, nested child mapping, event-context mapping, or naming/transformation
logic that would make attributes hard to scan.

## Reducer: fold the events as code

Reducers build read models too, but the shape is imperative: Chronicle passes the event, the current
state, and the event context into a method, and you return the next state.

```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reducers;

public class BookStatusReducer : IReducerFor<BookStatus>
{
    public BookStatus OnBookRegistered(
        BookRegistered @event,
        BookStatus? current,
        EventContext context) =>
        new(
            Id: context.EventSourceId.Value,
            Title: @event.Title,
            Isbn: @event.Isbn,
            IsBorrowed: false,
            BorrowedBy: null);

    public BookStatus OnBookBorrowed(
        BookBorrowed @event,
        BookStatus? current,
        EventContext context) =>
        current! with
        {
            IsBorrowed = true,
            BorrowedBy = @event.MemberName
        };

    public BookStatus OnBookReturned(
        BookReturned @event,
        BookStatus? current,
        EventContext context) =>
        current! with
        {
            IsBorrowed = false,
            BorrowedBy = null
        };
}
```

This is still the same read model, but the expression is a fold over state rather than a projection
definition. Reach for a reducer when the logic is easier to read as C#: branching, derived values, totals
with guard logic, temporal state, or calculations that span several previous events.

## How they differ

| Style | Mental model | Strength | Cost |
| --- | --- | --- | --- |
| [Model-bound projection](model-bound/index.md) | The read model declares how events fill its properties. | Least code; best default for straightforward screen models. | Attributes can get dense when the mapping becomes complex. |
| [Declarative projection](declarative/index.md) | A separate projection definition maps events to the read model. | Explicit mapping, joins, nested structures, transformations, and a clean read model type. | More ceremony than attributes. |
| [Reducer](/chronicle/reducers/) | Event plus current state returns next state. | Complex calculations and temporal logic read naturally as code. | More responsibility: handle missing current state and keep the reducer pure. |

Use the simplest style that keeps the intent obvious. Model-bound first, declarative when the mapping
needs structure, reducer when the calculation reads better as a state transition.

## Testing the choice

You can exercise all three styles with the same
[read-model scenario](/chronicle/testing/read-models/scenario/). The scenario discovers a model-bound
projection, an `IProjectionFor<T>`, or an `IReducerFor<T>` for the read model type and runs the events in
memory. That makes style changes cheap: rewrite the projection as a reducer, keep the same events and
expected `BookStatus`, and the test tells you whether the behavior stayed the same.
