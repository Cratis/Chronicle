---
title: Designing read models
description: Read models are cheap, disposable, and meant to be specialized — design them for the query, not for reuse.
---

If [modeling events well](./modeling-events.md) is about capturing truth, designing read models is about *presenting* it. The events are your single source of truth; a read model is just one view of them, built for one job. That reframing changes how you design.

The guiding principle: **specialize, don't share.** Build a focused read model per use case rather than one model stretched across conflicting screens.

## Read models are derived and disposable

A read model holds no truth of its own — it's computed from events by a [projection](./projection.md). That has a liberating consequence: you can create, change, and throw away read models freely. Got the shape wrong? Change the projection and [rebuild](../scenarios/rebuild-a-read-model.md). There's no precious state to migrate, because the events are the state.

## Shape it for the query, not the domain

A normalized, one-size-fits-all model is a relational-database habit. Here, design each read model around *how it will be read*:

- A list screen wants a flat, denormalized row per item.
- A detail screen wants one rich document with its children embedded.
- A dashboard wants pre-aggregated counts.

These can all be **separate read models built from the same events**. Denormalize without guilt — there's no update anomaly to fear, because you never update a read model by hand; the projection rebuilds it from the facts.

## Specialize over reuse

It's tempting to make one `Customer` read model serve every screen. Resist it. The moment two screens want conflicting shapes, a shared model becomes a compromise that serves neither well and breaks one when you change it for the other. A dedicated model per use case is easier to reason about, faster to query, and safe to change in isolation.

```text
❌ One CustomerReadModel feeding the list, the detail page, and the billing report
✅ CustomerListItem · CustomerDetail · CustomerBillingSummary — each from the same events
```

## Design for eventual consistency

A read model updates *after* the event is appended, so it's [eventually consistent](../read-models/). Design with that in mind: don't read a model back inside a command or reactor to make a decision (use a [constraint](../constraints/) for invariants instead), and prefer [observable queries](../scenarios/real-time-query.md) so the UI reflects changes the moment the projection catches up.

## Keep the read side ignorant of the write side

A projection only knows about events. It must not trigger commands, call external systems, or produce side effects — that's a [reactor's](../reactors/) job. Keeping projections pure is what makes them safe to replay at any time.

## Where this leads

- [Read Models](../read-models/) — retrieval, snapshots, and consistency in depth.
- [Projections, reducers, and reactors](./observer-patterns.md) — how the read side is built.
- [Modeling events well](./modeling-events.md) — the source of truth your read models derive from.
