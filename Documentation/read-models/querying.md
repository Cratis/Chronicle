---
uid: Chronicle.ReadModels.Querying
title: Querying read models in Cratis Chronicle
description: Use keyed reads and materialized paging in Cratis Chronicle, or query the backing store when you need server-side filtering, sorting, or search.
---

## Querying Read Models

Cratis Chronicle's read model API retrieves instances by key; it does not provide server-side field filtering or search. Use a keyed read when the caller knows the instance, materialized paging for a stored list, and the backing store's query API for filtering or sorting that must happen on the server.

In the .NET client, `IReadModels` has no `IQueryable` surface. `Observe<TReadModel>(filter)` accepts a predicate, but applies it in the client after reading the whole materialized collection. It does not reduce what the server reads or sends. Passive models and models that target an event sequence cannot use this collection observation; see [Observing read models](observing-read-models.mdx).

For a page of stored instances, use `IReadModels.Materialized`. It supports paging without filtering or sorting; see [Materialized read models](materialized-pagination.mdx).

## Why there is no query surface

A projection builds a read model for a particular read task. Its key can make that task a direct lookup: for example, an account overview keyed by account ID needs no search when the request already carries that ID.

Chronicle maintains that state from events and exposes keyed access. A search screen has different needs, such as user-selected filters or ordering. Those belong to the query API of the store holding the materialized model, rather than to Chronicle's keyed read API.

## What to do instead

Start with the lookup the caller needs. If a stable, unique key can answer it, consider a read model keyed that way and use [Getting a Single Instance](getting-single-instance.mdx). Changing the key changes how events are grouped, so check that the projection still represents the state you intend.

For arbitrary field searches, ad-hoc reports, or user-chosen filters, query the materialized read model through its storage driver. That lets the store filter and sort before returning results instead of loading the whole collection into the application.

For a small diagnostic collection, reading every instance and filtering in memory may be enough. [Getting a Collection of Instances](getting-collection-instances.mdx) explains the stored and replay paths. A passive model, or an explicit event-count cap, requires replay work proportional to the history processed; a materialized collection read loads the stored instances.

## What this costs you

A storage-native query couples that query code to the store and its schema. Tests of its actual filtering and sorting need that store; the in-memory read model harness does not exercise the driver's query behavior. Weigh that dependency against maintaining a separate read model for a known lookup.

Querying storage also does not make materialized state fresher or guard a later append. If the result determines whether a command is allowed, use [Read model consistency](consistency.md) to choose the read path and [decision-consistent reads](decision-reads.mdx) to protect an admitted decision.
