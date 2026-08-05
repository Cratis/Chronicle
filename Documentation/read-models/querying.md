---
uid: Chronicle.ReadModels.Querying
---

# Querying Read Models

Chronicle's read model API is keyed. You can get an instance by its id, get snapshots of it, watch it, and replay the whole collection. What you cannot do through `IReadModels` is filter, page, or search by a field that is not the key — there is no predicate-taking read and no `IQueryable`.

That is deliberate, and this page says so plainly, because from the outside a missing method looks the same whether it was left out or never intended.

## Why there is no query surface

A read model is not a table you query. It is a shape you decided on in advance, built by a projection from the events that feed it, so that one read case is answered by one direct lookup. Deciding what a read model contains, how it is keyed, and which events maintain it *is* the design work. A read model that has to be searched by arbitrary fields is usually a read model that has not been shaped for the read it is serving.

So the boundary is drawn on purpose: Chronicle takes responsibility for building read models from events and handing them back by key. It does not take on being a query engine, because the storage you already have is one.

## What to do instead

**First, ask whether the read model is the right shape.** If you are searching by a field, that field is what the read case is really keyed on. A projection keyed that way turns the search into a lookup — and unlike a query, it stays correct as events arrive.

**When the shape genuinely cannot be known in advance**, go to the storage directly. Searching an arbitrary field, ad-hoc reporting, an admin screen with user-chosen filters — these are query problems, and a query engine solves them better than an abstraction over one would. Reaching for the driver here is not a workaround; it is the supported answer.

**For a small collection, replay and filter in memory.** See [Getting a Collection of Instances](getting-collection-instances.mdx). This is proportional to event history, so keep it to result sets you know are small.

## What this costs you

Going to storage directly means that code knows which store the read model lives in, and a test for it needs that store rather than the in-memory read model harness. That is a real cost and worth weighing against reshaping the read model — which is why reshaping is the first thing to consider, not the last.

The trade is deliberate: Chronicle would have to leak a query language into its own API to avoid it, and the abstraction that resulted would be worse than the one you already have underneath.
