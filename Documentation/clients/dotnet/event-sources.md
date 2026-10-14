---
title: Event sources in the .NET client
description: The .NET client types for event source definitions, with links to the guides in the event sources section.
---

This page covers the .NET client's API for event source definitions. The guides, the scenarios and the explanation of how they work are in [Event sources and streams](../../event-sources/index.mdx). This page lists the client types and where each is explained. An event source is optional: the existing append APIs remain valid and append events without a definition.

## Types

| Type | Purpose | Explained in |
|---|---|---|
| `IEventSource` | Marker interface. A class that implements it is a definition | [Define an event source](../../event-sources/defining.mdx) |
| `[EventSource]` | Names the definition and sets its description and default `Concurrency` | [Define an event source](../../event-sources/defining.mdx#declare-the-definition) |
| `[EventStream]` | Declares a stream, with its own description and `Concurrency`. Repeat it for each stream | [Define an event source](../../event-sources/defining.mdx#declare-the-definition) |
| `ConcurrencyDimensions` | Flags that choose which values take part in an append's concurrency check | [Concurrency](../../event-sources/concurrency.mdx) |
| `Append<TSource>`, `AppendMany<TSource>`, `AppendPrepared<TSource>` | Extension methods on `IEventSequence` | [Append through a definition](../../event-sources/appending.mdx) |
| `EventForEventSourceId.EventSource` and `.EventStream` | Route one event of a multi-source batch through a definition | [Append through a definition](../../event-sources/appending.mdx#append-for-several-event-sources) |
| `ConcurrencyScopeBuilder.ForEventSource<TSource>` | Builds an explicit scope from a definition's names | [Concurrency](../../event-sources/concurrency.mdx#build-an-explicit-scope) |
| `[FromEventSource<TSource>]` | Filters a reactor or reducer to one stream | [Filter observers](../../event-sources/filtering.mdx) |
| `IEventStore.EventSources` and `IEventSources` | `All`, `GetFor(Type)` and `GetFor(string)` return the discovered `EventSourceDefinition` records | [Define an event source](../../event-sources/defining.mdx#check-what-is-registered) |
| `IClientArtifactsProvider.EventSources` | The types the client treats as definitions | [How it works](../../event-sources/how-it-works.mdx#discovery-and-registration) |

## Exceptions

| Exception | Thrown when |
|---|---|
| `DuplicateEventSourceName` | Two definitions share a name |
| `DuplicateEventStreamName` | A definition declares a stream twice |
| `UnknownEventSource` | A type or name is not a discovered definition |
| `EventStreamDoesNotBelongToEventSource` | A stream name is not declared by the definition |
| `EventRoutingContradictsEventSource` | An explicit event source type or stream type contradicts the definition |
| `IncompatibleConcurrencyScopesForEventSource` | A batch needs different guards for one event source id. See [Concurrency](../../event-sources/concurrency.mdx#a-batch-that-needs-two-guards-for-one-event-source-id) |
| `EventSourcesNotSupported` | An `IEventStore` or `IEventSequence` implementation does not support event sources, such as a custom test double |

## Explicit registration

With [explicit artifact registration](./explicit-artifacts.md) you nominate the definitions in the composition root. A nominated event source keeps its declared streams and metadata, and an explicit name changes the registered identity only.

## Related

- [Dynamic Consistency Boundary in Chronicle](../../dynamic-consistency-boundary/chronicle.md): how definitions, decision reads and concurrency scopes relate to DCB.
- [Where the clients differ](../where-the-clients-differ.md)
