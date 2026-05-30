---
title: Glossary
description: One definition for each core Chronicle term, so the same word means the same thing everywhere in the docs.
---

Event sourcing comes with its own vocabulary, and the same word can mean subtly different things in different products. This page is the single source of truth for what each term means in Chronicle. Where a term has a fuller treatment, the definition links to it.

## Aggregate

A consistency boundary around a single entity's stream of events — for example one bank account or one order. All decisions for that entity are made against its own events. When you don't need a whole-entity boundary, prefer a [Dynamic Consistency Boundary](../dynamic-consistency-boundary/).

## Constraint

A rule Chronicle enforces *as events are appended* — for example "this email address must be unique across all customers." A constraint can reject an append, which is how invariants are protected without reading a separate read model first. See [Constraints](./constraints.md).

## Dynamic Consistency Boundary (DCB)

A consistency boundary defined by the *decision being made* rather than by a fixed aggregate. Instead of loading an entire entity, you scope a decision to exactly the events that matter to it. See [Dynamic Consistency Boundary](../dynamic-consistency-boundary/).

## Event

An immutable record of something that *happened* — a fact, in the past tense (`OrderPlaced`, `AddressChanged`). Events are never updated or deleted; new facts supersede old ones. An event carries only the data that was true at the moment it occurred. See [Event](./event.md).

## Event Log

The default [event sequence](./event-sequence.md) in an [event store](./event-store.md). When you append without naming a sequence, the event goes to the event log.

## Event Metadata / Tags

Contextual information attached to an event that is not part of the domain fact itself — timestamps, correlation IDs, and user-defined **tags** used to filter or route events. See [Event Metadata Tags](./event-metadata-tags.md).

## Event Sequence

An ordered, append-only series of events. The [event log](#event-log) is the default sequence; you can define additional sequences for specialized streams. See [Event Sequence](./event-sequence.md).

## Event Source

The thing an event is *about* — the source of the change, such as a specific customer or order. Events that share an event source form that source's stream. See [Event Source](./event-source.md).

## Event Source ID

The strongly-typed identifier of an [event source](./event-source.md) (for example a `CustomerId`). It is the key Chronicle uses to group and replay an entity's events.

## Event Store

The top-level container for everything in a bounded context: event sequences, observers, projections, and read models. You obtain one from the client and work within it. See [Event Store](./event-store.md).

## Event Type

The schema and identity of a kind of event, declared with `[EventType]`. Chronicle validates appended events against their registered type and supports evolving them over time. See [Event Type](./event-type.md) and [Event Type Migrations](./event-type-migrations.md).

## Namespace

An isolation boundary *within* an event store, most often used for multi-tenancy — each tenant's events live in their own namespace while sharing the same model. See [Namespaces](./namespaces.md).

## Observer

The umbrella term for anything that *watches* an event sequence and reacts as events arrive. [Projections](./projection.md), [reducers](../reducers/), and [reactors](../reactors/) are all observers; they differ in what they do with the events. See [Observers](./observers.md).

## Projection

An [observer](./observers.md) that builds a [read model](../read-models/) by mapping and merging events into a document — declaratively, with no hand-written update code. Use a projection when the read side is shaped like data. See [Projection](./projection.md).

## Reactor

An [observer](./observers.md) that produces *side effects* — sending a notification, calling an external system, or triggering a command. Reactors do things; they do not build state. They must be idempotent because they may run more than once. See [Reactors](../reactors/).

## Read Model

A purpose-built, queryable view of state derived from events — the "read" side of CQRS. Read models are specialized per use case rather than shared, so they stay simple and fast. See [Read Models](../read-models/).

## Reducer

An [observer](./observers.md) that folds events into a value using imperative code you write, for cases a declarative [projection](./projection.md) can't express cleanly. See [Reducers](../reducers/).

## Replay

Re-running an [observer](./observers.md) over historical events to rebuild its [read model](../read-models/) from scratch — for example after changing a projection. Because events are the source of truth, read models are always disposable and rebuildable.

## Sink

The destination a [projection](./projection.md) writes its [read model](../read-models/) to — MongoDB by default, with other stores available through extensions. See [Sinks](../sinks/).

## Subject

An observable stream you can subscribe to for real-time updates — the mechanism behind reactive queries that push new data to clients as events arrive. See [Subject](./subject.md).

## Tail Sequence Number

The sequence number of the most recent event in an [event sequence](./event-sequence.md). `EventSequenceNumber.First` is `0`, so an empty log has no tail and the first appended event sits at `First`.

## Tenant

An isolated consumer of the system whose data must not mix with others'. In Chronicle, tenants are typically separated using [namespaces](./namespaces.md).
