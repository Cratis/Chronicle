---
title: When to use event sourcing
description: An honest look at why event sourcing is the Cratis default for information systems, where it pays off, and where a current-state slice can stay simpler.
---

Event sourcing is the Cratis default for information systems, but it is not free. The useful question is not "can this be event-sourced?" It is "which parts of this system carry meaningful facts over time, and which parts are genuinely just current-state data?" [Why Event Sourcing](../why-event-sourcing.md) makes the case for the default; this page names the boundaries.

## Where it pays off by default

Event sourcing earns its keep when **history and change are part of the problem**, which is true for most business-facing information systems:

- **Audit and compliance** — you need a complete, tamper-evident record of what happened and when. The event log *is* the audit trail.
- **Process-heavy domains** — orders, claims, onboarding, logistics: things move through states and the transitions matter.
- **"How did we get here?" is a real question** — debugging, dispute resolution, or analytics that need the past, not just the present.
- **Multiple read shapes from the same facts** — you want several specialized [read models](../read-models/) over one stream of truth.
- **Integration and reactions** — other parts of the system need to act when something happens.

## Where to be skeptical

Be skeptical when:

- **The domain is genuinely CRUD.** A few forms over a table where the current state is the whole story and nobody will ever ask what changed — a relational or document database is simpler and you should use one.
- **There is no meaningful behavior.** If every "change" is just "overwrite this field," you're modeling state, not events.
- **The team has no appetite for the concepts.** Events, projections, and eventual consistency are a real learning curve. If the problem doesn't justify it, the curve is pure cost.

## The trade-offs to weigh

- **Eventual consistency.** Read models are built *after* events are appended, so a read immediately after a write may not reflect it yet. This is usually fine — but it's a shift if you expect read-after-write everywhere. See [Read Models](../read-models/).
- **Modeling discipline.** Events are immutable facts; getting their granularity and naming right takes thought, and changing them later means [event type migrations](./event-type-migrations.md), not an `ALTER TABLE`.
- **More moving parts.** Commands, events, projections, and reactors are more concepts than "save the row" — the framework removes the boilerplate, but the mental model is larger.
- **Privacy.** Immutable history and "the right to be forgotten" need a deliberate strategy — see [Compliance](../compliance/).

## A middle path

You don't have to choose globally. Event-source the core domain and the processes that define the system, and keep plain CRUD for parts that are genuinely just current-state data: reference tables, settings, or small admin surfaces. Cratis is happy alongside a relational or document store for those bits, and Arc can put the same CQRS boundary over them.

## Next

- Convinced it fits? Start with [Get started](../get-started/) and the [tutorial](../tutorial/).
- Comparing with a CRUD/EF Core model? Read [CRUD, EF Core, and Chronicle](../coming-from-crud.md).
