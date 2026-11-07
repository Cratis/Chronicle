---
title: Chronicle immediate projections — read after an append
description: Chronicle has no synchronous projection mode. Read passive state on demand or wait for materialized observers when a caller needs its own write.
---

An immediate projection in Chronicle computes a passive read model on demand; it does not update a
stored read model inside an append. A successful append confirms that the event is in the log, while
materialized projections process it asynchronously. Reading the stored instance immediately can
therefore return its previous state.

The kernel manages the immediate-projection component. You choose passive behavior on the projection,
not a separate immediate mode.

## Getting a read model that includes the latest event

Choose between computing the instance from history and waiting for its stored view.

For a passive read model, Chronicle replays the instance's history from the start on each read, so
the result reflects revisions, redactions, and personal-data erasure. It is not stored in a sink. A
passive by-ID read selects events from that event source only: cross-source joins or custom
aggregation keys can need events outside that selection. Keep those views materialized. See
[passive projections](declarative/passive.mdx) and
[getting a single instance](../read-models/getting-single-instance.mdx) for setup and read behavior.

For a materialized view, the caller can wait after its append and then query the stored instance.
The .NET client exposes `WaitForCompletion()` on the append result. Check whether the wait succeeded,
timed out, or reported observer failures before treating the view as caught up. With the .NET client
and an updated server, it waits for observers handling the appended event types and observers
subscribed to all events. Other clients and older servers wait for every observer on the sequence.
[Observing appends](../events/observing-appends.mdx#waiting-for-observer-completion-after-append)
explains the result and timeout behavior.

Neither option protects a decision made before an append. Two writers can both read valid state and
then make conflicting changes. Use a [constraint](../constraints/index.md) for a unique value or a
create-once rule, or a [decision read](../read-models/decision-reads.mdx) for a rule over state.
[Choosing how a rule holds](../dynamic-consistency-boundary/choosing.mdx) compares those protections.

## See also

- [Read model consistency](../read-models/consistency.md) — the trade-offs in full
- [Eventual consistency](eventual-consistency.mdx) — designing for the default
