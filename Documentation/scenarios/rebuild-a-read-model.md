---
title: Rebuild a read model
description: Replay events to rebuild a read model after you change its projection.
---

**Goal:** you changed a [projection](../concepts/projection.md) (added a property, fixed a mapping) and the existing read model still reflects the old logic. You want it rebuilt from the events.

## Why this works

Read models are **derived data** — the [event log](../concepts/event-sequence.md) is the source of truth. So a read model is always disposable: Chronicle can replay the events through the projection and produce it again from scratch. You never migrate a read model in place; you rebuild it.

## Do it

1. Make your projection change and deploy it.
2. **Replay** the observer so it reprocesses the event sequence from the beginning and rewrites its read model. You can trigger a replay from the [Workbench](../get-started/) or programmatically as part of your deployment.
3. Query the read model and confirm it reflects the new logic.

:::tip
Rebuilds are safe to run as often as you like — they don't touch the events, only the derived read model. This is also how you recover a read model that drifted or whose sink was wiped.
:::

## Notes

- A rebuild reprocesses **all** matching events, so for very large streams it takes time proportional to the history. Plan rebuilds of big projections accordingly.
- Reducers rebuild the same way — they're also derived from events.

## See also

- [Read Models](../read-models/) — consistency and how read models are served.
- [Projections](../projections/) — the full projection model.
