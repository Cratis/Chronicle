---
title: Show data that updates live
description: Serve a read model as an observable so the UI updates by itself as new events arrive.
---

**Goal:** a screen should reflect new data the moment it changes — a dashboard count, a list of open items — without the user refreshing or you writing polling code.

## Make the query observable

A [read model](../read-models/) can be served as an **observable** stream. Instead of returning a snapshot, the query returns an `ISubject<T>` that pushes a new value whenever the underlying read model changes:

```csharp
[ReadModel]
[FromEvent<BookBorrowed>]
[FromEvent<BookReturned>]
public record OnLoan([property: Key] BookId Id, string Title)
{
    public static ISubject<IEnumerable<OnLoan>> AllOnLoan(IMongoCollection<OnLoan> collection) =>
        collection.Observe();
}
```

`Observe()` turns the collection into a live subject. As events update the read model, subscribers receive the new set.

## Consume it on the frontend

The generated proxy for an observable query exposes a `.use()` hook (and the Components data table that wraps it). It re-renders on every push — no polling:

```tsx
const [onLoan] = AllOnLoan.use();
// onLoan.data is always the latest set
```

Or drop it straight into a live table — see [Displaying data](/components/displaying-data/).

## How it works

When a command appends `BookBorrowed`, the projection updates the `OnLoan` read model, and the observable pushes the new value to every subscriber. The brief gap between the append and the push is the normal [eventual consistency](../read-models/) window — usually imperceptible.

## See also

- [Read Models](../read-models/) — consistency and watching read models.
- [Displaying data](/components/displaying-data/) — rendering an observable query in Components.
