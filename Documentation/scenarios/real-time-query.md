---
title: Get read models
description: Retrieve read model instances — one, all, or a page — and observe them change live, picking the right consistency for each read.
---

**Goal:** your events project into a read model — now you need it back out. One book, the whole catalog, a page for a grid, or a live stream that updates the UI as events arrive. `IReadModels` (on `IEventStore.ReadModels`) covers all of these; the choice is between **strongly consistent** reads computed from the event log and **eventually consistent** reads served from the materialized sink.

## Get one instance

`GetInstanceById` replays the key's events through the projection or reducer on demand — strongly consistent, reflecting everything appended up to this moment:

```csharp
var book = await eventStore.ReadModels.GetInstanceById<Book>(bookId.Value);
```

`ReadModelKey` converts implicitly from `string`, `Guid`, and `EventSourceId`, so you can pass the id you already have.

## Get all instances

`GetInstances` rebuilds every instance by replaying the event log — strongly consistent, suited to reads where accuracy beats latency. The cost grows with history; filter the result with LINQ:

```csharp
var books = await eventStore.ReadModels.GetInstances<Book>();
var onLoan = books.Where(b => b.OnLoan);
```

## Page through materialized state

For list views and grids, skip the replay: `Materialized` reads the instances the projection has already persisted to the sink — eventually consistent (typically milliseconds behind), fast regardless of history, and paged:

```csharp
var page = await eventStore.ReadModels.Materialized.GetInstances<Book>(skip: 0, take: 20);
```

## Watch every change

`Watch<T>()` returns an `IObservable<ReadModelChangeset<T>>` that emits whenever **any** instance of that read model type changes — the changeset carries the key, the new state, and whether the instance was removed:

```csharp
var subscription = eventStore.ReadModels.Watch<Book>()
    .Subscribe(changeset =>
        Console.WriteLine($"{changeset.ModelKey}: on loan = {changeset.ReadModel?.OnLoan}"));
```

Dispose the subscription when you're done. If you need explicit lifecycle control — for example awaiting the watcher's `Subscribed` before producing events — use `GetWatcherFor<Book>()` instead.

## Observe a live page

`Materialized.ObserveInstances` pushes a fresh page snapshot whenever the stored data changes — a live dashboard or table with no polling:

```csharp
var subscription = eventStore.ReadModels.Materialized
    .ObserveInstances<Book>(take: 50)
    .Subscribe(books => UpdateView(books));
```

When a command appends `BookBorrowed`, the projection updates the materialized `Book` and every subscriber receives the new page. The brief gap between append and push is the normal [eventual consistency](../read-models/consistency.md) window — usually imperceptible.

:::note
Serving these to a frontend? Arc turns read models into observable queries with generated TypeScript proxies — drop one straight into a live table with [Displaying data](/components/displaying-data/).
:::

## Pick by consistency

| You need | Use | Consistency |
|---|---|---|
| One instance, exact current state | `GetInstanceById<T>(key)` | Strong |
| All instances, exact current state | `GetInstances<T>()` | Strong |
| A page for a list or grid | `Materialized.GetInstances<T>(skip, take)` | Eventual |
| To react to every change of a type | `Watch<T>()` | Pushed as changes apply |
| A live-updating page | `Materialized.ObserveInstances<T>(skip, take)` | Eventual, pushed |

## See also

- [Consistency models](../read-models/consistency.md) — the strong-vs-eventual trade-off in depth.
- [Getting a single instance](../read-models/getting-single-instance.md) and [Getting a collection of instances](../read-models/getting-collection-instances.md) — caching, performance, and the non-generic forms.
- [Materialized read models](../read-models/materialized-pagination.md) — pagination details and when to query the sink directly.
- [Watching read models](../read-models/watching-read-models.md) — changesets, filtering, and error handling.
