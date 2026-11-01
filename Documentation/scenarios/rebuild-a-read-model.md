---
title: Rebuild a read model
description: Replay events to rebuild a read model after you change its projection.
---

**Goal:** you changed a [projection](../concepts/projection.md) (added a property, fixed a mapping) and the existing read model still reflects the old logic. You want it rebuilt from the events.

## Why this works

Read models are **derived data** — the [event log](../concepts/event-sequence.md) is the source of truth. So a read model is always disposable: Chronicle can replay the events through the projection and produce it again from scratch. You never migrate a read model in place; you rebuild it.

## Do it

1. Make your projection change and deploy it.
2. **Replay** the observer so it reprocesses the event sequence from the beginning and rewrites its read model. Use the [Cratis CLI](/cli/) to trigger a replay:

   ```bash
   cratis chronicle observers replay <observer-id>
   ```

   See [`cratis chronicle observers replay`](/cli/chronicle/observers/#replay) for the full command reference. The [Chronicle Workbench](/chronicle/workbench/) can also replay an observer, and clear an observer's quarantine, from its observers view for a user who is allowed to.

3. Query the read model and confirm it reflects the new logic.

:::tip
A rebuild never touches the events, only the derived read model, so it is safe to repeat. This is also how you recover a read model that drifted or whose sink was wiped. It is not free: while the rebuild runs, the read model does not follow new events, so readers lag by at least the length of the rebuild. See [What readers see during a rebuild](#what-readers-see-during-a-rebuild).
:::

## What readers see during a rebuild

While the replay runs, the observer handles no live events. It writes the rebuilt model to a separate container and readers keep getting the **pre-rebuild model**, as it was when the replay started. This holds for lookups by key and, on the MongoDB and SQL sinks, for materialized pages. [Reads during a replay](../read-models/getting-single-instance.mdx#reads-during-a-replay) describes the key lookups and the short window around promotion.

When the replay finishes, the rebuilt model replaces the old one. The observer then catches up on the events appended during the rebuild. A single read therefore returns either the old model or the rebuilt one. Chronicle does not offer point-in-time snapshot reads across a rebuild. The longer the rebuild runs, the staler the read model gets, and the more events remain to catch up on afterwards.

## A rebuild that produces nothing

If a rebuild writes no instances, Chronicle discards the empty result and keeps the existing read model unchanged. This protects the read model from a race that would otherwise replace it with nothing. The consequence is that a read model which should legitimately become empty is **not** cleared by a rebuild.

## Notes

- A rebuild reprocesses **all** matching events, so for very large streams it takes time proportional to the history. Plan rebuilds of big projections accordingly.
- Reducers rebuild the same way — they're also derived from events.
- Replaying a projection with **global** scope is refused, whether full or per partition. Kernel projections cannot be declared global. See [Observers](../concepts/observers.mdx) and the [scope decision](https://github.com/Cratis/Chronicle/blob/main/decisions/0015-global-projection-scope-is-restricted.md).
- A projection definition change that needs history means a full rebuild, or a recommendation to replay when the observer configuration says not to rebuild automatically. See [definition evolution](../hosting/configuration/observers.md#definition-evolution).

## See also

- [Operate a read model rebuild](./operate-a-read-model-rebuild.md) — watch a rebuild, handle one that fails, and roll back.
- [Read Models](../read-models/) — consistency and how read models are served.
- [Projections](../projections/) — the full projection model.
