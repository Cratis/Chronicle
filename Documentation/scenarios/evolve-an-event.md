---
title: Evolve an event's shape
description: Change an event type over time without rewriting the history that's already been written.
---

**Goal:** an [event type](../concepts/event-type.md) needs to change — a renamed field, a name split into first and last name, a new value with a sensible default — but you have years of old events already in the log that you must not corrupt.

## Why you don't edit history

Events are **immutable facts**. Rewriting old events to a new shape would be falsifying the record — and it breaks the guarantee everything else depends on. Instead, you describe how one generation of an event type maps to the next: an [event type migration](../concepts/event-type-migrations). Stored content is not overwritten; Chronicle adds the missing generations alongside it and can also migrate on demand when it delivers an event pinned to a specific generation.

## Do it

1. Define the new shape of the event type.
2. Register an **event type migration** that maps the previous generation to the new one — renaming, splitting or combining properties, or supplying a default for a new field. A migration changes the properties of one event type across its generations; it never turns one event into several events (see [Event Type Migrations](../concepts/event-type-migrations) for the typed builder API).
3. Upgrade every kernel silo first, then deploy and register the new generation. Workers on an older release can overwrite content during the rollout.
4. Let the background backfill finish. Registering a generation starts an asynchronous job that adds the new generation to events already in the log. It covers the **default namespace's event log only**; events appended after registration get their generations when they are appended. Check the job's status before you rely on it. Repeated write conflicts on an event are skipped with a warning, so inspect the job and the target content before replaying.
5. [Rebuild affected read models](./rebuild-a-read-model.md) so they reflect the migrated shape. Do this after the backfill, not immediately after deploy.

## Guidance

- **Add, don't mutate.** Prefer adding a new event type or a new optional concept over overloading an existing event — it keeps each event a single, clear fact.
- **Migrations are cumulative.** Each generation maps to the next, so content from any past generation can be upcast, and later generations downcast, through the chain.
- **Some changes lose information.** Splitting or combining properties may not round-trip. Check the rules in [Generation validation](../migrations/validation.mdx) and keep the original generation's content, which backfill never replaces.

## See also

- [Event Type Migrations](../concepts/event-type-migrations) — the full migration API and operations.
- [Migrations overview](../migrations/index.md) — how backfill works and its limits.
- [Rebuild a read model](./rebuild-a-read-model.md) — apply the new shape to existing read models.
