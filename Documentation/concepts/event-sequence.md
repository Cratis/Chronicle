---
title: "Cratis Chronicle event sequences and observer offsets"
description: "A Cratis Chronicle event sequence orders appended events by sequence number. Observers use those numbers to track progress and resume reading."
---

An event sequence in Cratis Chronicle is an ordered collection of events. Each appended event receives an increasing sequence number, unique within that sequence. [Observers](./observers.mdx) use these numbers as offsets to track how far they have processed and where to resume.

Sequence order is append order, not necessarily the order in which events happened. An imported event can carry an earlier occurrence time while receiving the next sequence number. Chronicle stores that time and other context as metadata alongside the event.

In this sequence, an observer at offset `#2` reads `#3` next:

```mermaid
flowchart LR
    E0["#0 BookAdded"] --> E1["#1 BookBorrowed"] --> E2["#2 BookReturned"] --> E3["#3 BookBorrowed"]
    O["Observer (offset: #2)"] -.->|reads next| E3
```

The observer keeps its offset as new events arrive, so it can continue from its last processed position.

Most applications append to the event log:

| Type | Description |
| ---- | ----------- |
| Event Log | The main sequence you typically append to |

## Event Type generations

An [event type](./event-type.md) can evolve through generations, each with its own schema. The `content` property stores event content per generation so observers can receive the representation they expect. Use [event type migrations](./event-type-migrations.mdx) to define how content moves between those schemas.

## Revisions

Appending records a new event. If an existing event contains incorrect data, revision records corrected content alongside the original instead of appending a different domain fact. Chronicle retains revision metadata, including when the correction occurred and who caused it.

A revision corrects an individual event; a generation changes an event type's schema. For the correction workflow, see [Event revision](../events/revision.md). If something new happened in the domain, record it as a new event.
