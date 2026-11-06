---
title: When to use event sourcing with Cratis Chronicle
description: Use event sourcing when business history and transitions matter. Weigh modeling, replay, and consistency costs before choosing it over current-state storage.
---

Use event sourcing when the changes that led to the current state are part of the information your system needs to preserve. Cratis Chronicle stores those changes as events so you can explain past decisions, build different read models, and react to business facts. If only the latest value matters, current-state storage may be simpler.

Event sourcing is the Cratis default for information systems, not a requirement for every table or screen. [Why Event Sourcing](../why-event-sourcing.md) explains that default; the decision here is which parts of your system benefit enough to justify the modeling and operational work.

## Where it pays off by default

Consider an order whose price is disputed after the product catalog changes. The current catalog cannot explain the amount accepted at checkout. Events that record the accepted price, later adjustments, and cancellation preserve the facts needed to answer that question.

The same approach is useful when:

- A process has meaningful transitions, such as a claim being submitted, assessed, and settled.
- Support or reporting needs to explain how the system reached a state, not just display that state.
- Several screens or reports need different [read models](../read-models/) derived from the same history.
- Another part of the system needs to act on a specific business change.

An event log can support an audit trail, but it records the facts you choose to capture. It does not by itself establish regulatory compliance or prove that all relevant activity was recorded.

## Where to be skeptical

A table of display settings may need only its latest values. If there is no requirement to explain changes, rebuild views from history, or react to meaningful transitions, a relational or document store avoids work that brings little value to that slice.

Also consider who will maintain the system. Event names, schema evolution, projection lag, and replay become part of development and support. If the team cannot budget time to learn and operate them, choose a smaller event-sourced boundary rather than applying the approach everywhere at once.

## The trade-offs to weigh

Materialized read models update after an append, so an immediate query may return older state. Decide which screens can tolerate that lag and which decisions need a fresh, guarded read. Chronicle also supports on-demand reads; [Read Models](../read-models/) explains the consistency choices. Event sourcing does not require every read to be eventually consistent.

Events become contracts for their consumers. Their names and payloads need to preserve business meaning, and changes to an existing event shape need [event type migrations](./event-type-migrations). That is more deliberate work than overwriting a field in a row.

Replay makes it possible to rebuild derived views, but it consumes processing and storage work. Plan how readers use the model during a rebuild, and keep external effects out of the state-building path. [Choosing an observer](./observer-patterns.md) separates rebuilding a view from repeating an action.

Historical data also needs a privacy strategy. Decide which event values contain personal information and how they will be protected or erased before collecting them. [Compliance](../compliance/) covers Chronicle's mechanisms; choosing event sourcing does not remove those responsibilities.

## A middle path

You can event-source an order process while keeping display settings in current-state storage. Choose the boundary by the information each part must preserve, not by a rule that every persistence operation must use the same approach.

Keep ownership clear: an event-derived read model should be rebuilt from events, not maintained by a second CRUD write path. If a reference table's changes later become meaningful business history, revisit its boundary then.

## Next

- Convinced it fits? Start with [Get started](../get-started/) and the [tutorial](../tutorial/).
- Comparing with a CRUD/EF Core model? Read [CRUD, EF Core, and Chronicle](../coming-from-crud).
