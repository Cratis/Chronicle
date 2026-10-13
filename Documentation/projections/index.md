---
title: Projections
description: Build specialized, queryable read models from events and choose the right projection style and consistency model.
---

Your events are the source of truth, but events are a poor thing to *read* — nobody wants to replay a
thousand `MoneyDeposited` facts to show an account balance. A **projection** does that folding for you:
it watches a stream of events and continuously builds a **read model** — a shaped, queryable view that's
ready to serve to a UI.

The win is specialization. The same events can feed *many* projections, each tailored to one screen or
question: a balance, a transaction list, a monthly summary. You never reuse one bloated model across
conflicting needs — you build a focused read model per view, and each stays simple, fast, and
independent.

```mermaid
flowchart LR
    E1[Event] --> P[Projection]
    E2[Event] --> P
    E3[Event] --> P
    P -->|maps & merges| RM[(Read model)]
    RM --> Q[Query]
```

## How you'll define one

Start with model-bound attributes, move to fluent `IProjectionFor<T>` when the mapping needs structure,
and use a reducer when the logic reads better as state transitions; a self-reducing model is the default
for new reducer code. [Choose how to build a read model](choosing-a-read-model-style.mdx) compares the
forms on the same event history and explains when separation or dependency injection warrants an
`IReducerFor<T>`, when to nominate a plain class, and when tooling-authored PDL fits.

Projections join **events** (never other read models). Fluent mappings use AutoMap by default;
model-bound projections opt into event mappings with attributes such as `[FromEvent<T>]`.

## Choose your consistency

The one decision worth making up front is *when* the read model has to be correct:

| | [Materialized (eventual)](eventual-consistency) | [Passive (on demand)](declarative/passive.mdx) |
| --- | --- | --- |
| **When it updates** | Shortly after the event is appended (the default) | Computed on demand, without a persistent sink |
| **Cost** | Cheap reads, scales freely | First read folds the history; cached projection state can apply only newer events |
| **Use it for** | Almost everything — lists, dashboards, history | A value you must read back correctly *right now*, such as state a command decides on |

Start materialized. Make a read model passive only when a workflow genuinely can't tolerate it being a
moment behind. When one caller occasionally needs its own write back, it can instead
[wait for the append's observers](../read-models/consistency.md#read-after-write--waiting-for-a-materialized-read-model).
No projection updates synchronously inside the append, and a read cannot enforce uniqueness — use a
[constraint](../constraints/index.md) for that.

## Topics

| Topic | Description |
| ------ | ----------- |
| [Architecture](architecture) | How the projection engine turns events into read models |
| [Choose how to build a read model](choosing-a-read-model-style.mdx) | One decision path, worked examples, capabilities, and switching costs |
| [Model-Bound Projections](model-bound/) | Build read models with attributes — the default style |
| [Declarative Projections](declarative/) | Build read models with the fluent `IProjectionFor<T>` API |
| [Immediate Projections](immediate-projections.md) | Why there is no synchronous projection mode, and what to use instead |
| [Eventual Consistency](eventual-consistency) | The default — how and when eventual projections catch up |
| [Registration lifecycle](registration-lifecycle.md) | What happens when projections register, change, fail, or are no longer declared |
| [Tagging Projections](tagging-projections) | Organize and tag projections |
| [Appended event metadata filters](../events/filtering/index.md) | How tags and metadata correlate reducers and reactors that run alongside projections |

Once your read model exists, expose it to the frontend with a [query](../read-models/index.md) — or see
the whole loop in [Build a full-stack feature](/build-a-full-app/).
