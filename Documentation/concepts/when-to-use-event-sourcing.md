---
title: When to use event sourcing
description: An honest look at where event sourcing pays off, where it costs more than it returns, and the trade-offs to weigh before you commit.
---

Event sourcing is powerful, but it is not free. The most useful thing the docs can do is tell you *when not to reach for it* — so you adopt it where it pays off and avoid it where it just adds concepts. [Why Event Sourcing](../why-event-sourcing.md) makes the case *for* it; this page is the honest counterweight.

## Where it pays off

Event sourcing earns its keep when **history and change are part of the problem**, not an afterthought:

- **Audit and compliance** — you need a complete, tamper-evident record of what happened and when. The event log *is* the audit trail.
- **Process-heavy domains** — orders, claims, onboarding, logistics: things move through states and the transitions matter.
- **"How did we get here?" is a real question** — debugging, dispute resolution, or analytics that need the past, not just the present.
- **Multiple read shapes from the same facts** — you want several specialized [read models](../read-models/) over one stream of truth.
- **Integration and reactions** — other parts of the system need to act when something happens.

## Where it costs more than it returns

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

You don't have to choose globally. Event-source the part of the system where history matters (the core domain, the process), and keep plain CRUD for the parts that are genuinely just data (reference tables, settings). Cratis is happy alongside a relational or document store for the CRUD bits.

## Next

- Convinced it fits? Start with [Get started](../get-started/) and the [tutorial](../tutorial/).
- Comparing with a CRUD/EF Core model? Read [CRUD, EF Core, and Chronicle](../coming-from-crud.md).
