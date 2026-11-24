---
id: 0038-lazy-observers-routed-by-a-silo-local-table
title: Route events by a silo-local table replicated by revision, ring observers without a live subscription, and keep no grain alive that owes nothing
status: proposed
stage: none
class: architecture
reversibility: moderate
proposed: 2026-11-24
applies-to:
  - Source/Kernel/Core/EventSequences/**
  - Source/Kernel/Core/Observation/**
  - Source/Kernel/Storage/Observation/**
  - Documentation/hosting/configuration/observers.md
---

<!-- markdownlint-disable-next-line MD025 -->
# Route events by a silo-local table replicated by revision, ring observers without a live subscription, and keep no grain alive that owes nothing

Proposed on 2026-11-24. It is accepted when the pull request that introduces it is approved. It amends [decision 0030](./0030-release-idle-unsubscribed-observers.md) and [decision 0034](./0034-release-dormant-subscribed-observers.md) (see [Amended statements](#amended-statements)), and implements [#6189](https://github.com/Cratis/Chronicle/issues/6189).

## Context

Observers are the main cost of a kernel at scale: an observer per namespace is a grain, and before this decision every one of them, every appended events queue and every queue router carried `[KeepAlive]`. Decisions 0030 and 0034 released idle and dormant observers, but each released observer still kept an entry in its queue, durable records were read to seed every queue on activation, and the router activated every queue on startup to seed itself.

## Decision

1. **Definitions carry a revision and are replicated by it.** An `ObserverDefinition` holds an `ObserverDefinitionRevision`, stamped from storage when the definition is saved: the stored revision when the content is unchanged, the next one otherwise. A definition stored before revisions existed reads as revision 0. After a save the revision is published to every active silo, and each silo answers once its routing table holds it. A silo that does not answer, such as one of an earlier release, is reported as lagging and catches up through the existing invalidation.
2. **Each silo holds an immutable routing table per event store** (`IObservers`): event sequence and event type to the observers that take them, built from the definition registry snapshot through a state provider over the Storage APIs. A route carries no filters, so it matches every event of its types and the observer applies its own filters.
3. **The queue router is stateless.** An observer's queue is a stable hash of its identifier, and the queues a batch goes to come from the routing table; without a table every queue receives the batch.
4. **A queue rings the observers it holds no live subscription for.** The instances of a namespace are the observers with a stored cursor there. When an event an instance takes arrives, or the queue's safety net finds an event waiting after its cursor, the queue runs one watchdog pass on the observer, which resumes from its cursor and takes its live subscription again. An observer that stays unsubscribed after a ring is rung again only after a backoff that doubles up to `dormantSafetyNetFullWake`. Nothing is seeded from durable records and a released observer leaves no entry in its queue.
5. **No grain owes its residency to `[KeepAlive]`.** The appended events queue, the queue router and the observer no longer carry it. A queue keeps itself only while it holds live subscriptions, queued or in-flight deliveries, catch-up triggers or rings. An observer keeps itself unless it is idle or released as dormant. Orleans collects them like any other grain otherwise.
6. **One supervisor per event store and observer** (`IObserverSupervisor`) owns the definition axis: it publishes the revision, finds the namespaces an observer is behind in without activating it, and holds the connection that serves each namespace in memory as a hint, which an observer without a record of its own resumes from. It is never on the event path.
7. **The live subscription remains the observer's handshake with its queue.** It is what tells the queue that the observer's cursor is current, so the queue never delivers past a hole. It no longer carries definitions, seeding or dormancy.
8. **The projection engine is set up lazily.** Subscribing projections in a namespace no longer sets that namespace up in the engine of every silo; the engine compiles a projection for a namespace when an observer there first asks for it, and shares the compiled plan between namespaces.

## Consequences

- A kernel with many quiet namespaces keeps no queue, router or observer resident for them.
- A queue collected while observers were subscribed to it loses their live subscriptions. The next event rings them, and their watchdog recovers what was missed from their cursors, so delivery is delayed, never lost.
- The durable target sweeps (`DormantTargetSweep`, `DormantSiloDepartures`) and the shadow comparison of durable subscriptions are removed. A record that names a connection that is gone is validated when the observer wakes.
- Grain interfaces only grew: `IAppendedEventsQueue.TryMarkDormant` now drops the live subscription so the table rings the observer, and `IsDormant` answers from the queue's instances. The new grain interfaces and the one new payload type are additive.

## Not decided here

- Registering reactors and reducers through the registration inbox, split into a definition and a binding. The client wire contract is unchanged and reactors and reducers still register per connection.
- Removing the definition registry's invalidation marks and its periodic reload, which silos of earlier releases still rely on.

## Amended statements

- Decision 0030: "A subscribed observer always stays in memory" becomes: a subscribed observer stays in memory while it owes work; Orleans may collect it once it is released as dormant.
- Decision 0034, point 2: the queue no longer keeps the entry of a dormant observer or seeds entries from durable records; the routing table and the observer's cursor are what it is rung by.
