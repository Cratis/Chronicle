---
title: Immediate projections
description: Chronicle has no synchronous projection mode. How to get a read model that includes the event you just appended.
---

You may have been told that an *immediate projection* updates its read model synchronously, inside the append, so the read model is current before the append returns. **Chronicle has no such mode.** Every append returns once the event is in the log, and materialized read models catch up afterwards.

"Immediate projection" is the name of the kernel component that computes a read model on demand when you read a **passive** read model. You never declare or configure it yourself.

## Getting a read model that includes the latest event

You have two tools, and neither makes the append slower for anyone else:

- **Mark the read model passive.** It is not stored; Chronicle computes it from its events when you read it, so each read includes everything appended up to that call. The first read replays the instance's history; while it stays in memory, later reads apply only newer events. This works for a read model keyed by its event source id: reading a passive instance only replays events from that event source, so a projection that joins other event sources or uses a custom key misses their events — keep that one materialized. See [Passive projections](declarative/passive.mdx) and [Getting a single instance](../read-models/getting-single-instance.mdx).
- **Keep it materialized, and wait after the append.** The caller that needs its own write back waits for the append's observers to catch up, then reads the stored instance. In the .NET client this is `WaitForCompletion()` on the append result — see [Observing appends](../events/observing-appends.mdx#waiting-for-observer-completion-after-append). With the .NET client and an updated server, the wait includes observers that handle the appended event types, plus observers subscribed to all events. Other clients and older servers still wait for every observer on the sequence.

To stop two writers from both succeeding, use a [constraint](../constraints/index.md) instead: a read, however consistent, is taken before the append and cannot stop a race.

## See also

- [Read model consistency](../read-models/consistency.md) — the trade-offs in full
- [Eventual consistency](eventual-consistency.mdx) — designing for the default
