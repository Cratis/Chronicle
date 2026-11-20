---
id: 0035-remove-unseen-client-observers
title: Remove a client reactor or reducer that no client has subscribed to for a configured period, by recorded last-seen time and a bounded storage sweep
status: superseded
stage: none
class: contract
reversibility: costly
proposed: 2026-11-19
decided: 2026-11-19
decider: einari
superseded-by: 0036-unseen-observer-removal-is-opt-in
applies-to:
  - Source/Kernel/Core/Observation/Observer*.cs
  - Source/Kernel/Core/Observation/ObserverRemover.cs
  - Source/Kernel/Core/Observation/UnseenObserverRemoval*.cs
  - Source/Kernel/Core/Configuration/Observers.cs
  - Source/Kernel/Storage/Observation/IObserverStateStorage.cs
  - Source/Kernel/Storage.MongoDB/Observation/ObserverStateStorage.cs
  - Source/Kernel/Storage.Sql/EventStores/Namespaces/Observers/**
  - Source/Kernel/Storage.InMemory/Observation/ObserverStateStorage.cs
---
<!-- markdownlint-disable-next-line MD025 -->
# Remove a client reactor or reducer that no client has subscribed to for a configured period, by recorded last-seen time and a bounded storage sweep

> **2026-11-19 — superseded in part by [decision 0036](./0036-unseen-observer-removal-is-opt-in.md).** The decider changed the answer on the default the same day: the removal is off unless an operator sets `Observers.UnseenRemovalEnabled`, `UnseenRemovalAfter` keeps its 7-day default as the period used when it is on, and the last-seen time is recorded even while the removal is off. Decisions 1 (default and "zero turns the feature off") and 3 ("while the setting is zero nothing is written") below were the original choice and are kept as written; the rest of this record still applies and is read together with 0036.

Decided on 2026-11-19 by einari. It came out of the discussion of [decision 0034](./0034-release-dormant-subscribed-observers.md) as a separate question; it is a separate record so that either can be superseded alone. It relies on [decision 0030](./0030-release-idle-unsubscribed-observers.md) (an unsubscribed idle observer is already released from memory, so nothing resident keeps an unseen observer's cost down to its stored records) and on the existing `IObserverRemover` / `RemoveObserver` path.

## Context

A client reactor or reducer that is renamed, deleted or retired in application code leaves its observer behind in the kernel: the definition (event store wide), the state of each namespace, its failed partitions, handled counts, jobs and alert reminders stay forever, and one per namespace is activated whenever something touches it. On a kernel with hundreds of event store/namespace pairs those records and their activations accumulate with every tenant and every renamed handler. Today the only way to remove one is the operator command `RemoveObserver`, which refuses while a client is subscribed.

The facts the design rests on, read in the code:

- A definition lives once per event store (`IObserverDefinitionsStorage`, with `Type` and `Owner`); a state document lives once per namespace (`IObserverStateStorage`). Reactors and reducers declared by a client have `Owner == Client`. Projections are `Owner == Kernel` even when a client declared them: their subscription is the kernel's, and removing one tears down generations and read-model bindings through `ProjectionsManager.Remove`.
- `ObserverRemover.Remove` already refuses a subscribed or `Active` observer, retires each namespace's state, deletes its jobs, failed partitions, in-flight events, handled counts, retry reminders and alert reminder, and deletes the definition. It never touches a reducer's read model data.
- Nothing records that a client has ever held a subscription. `RunningStateChangedAt` is the closest field and is not that.
- Storage already carries side fields with their own targeted read and write, outside the `Save` of the observer's state (`RunningStateChangedAt`: a `$set` of named fields in MongoDB, a column copied across `Save` in SQL, an additive migration `v19_115_0_1`). Older kernels ignore the unknown MongoDB element, and an older kernel's SQL `Save` leaves an unknown column as it was or, at worst, clears it.
- Periodic kernel work that is not a grain reminder is a silo-local `ILifecycleParticipant<ISiloLifecycle>` with a `PeriodicTimer` that calls idempotent grain methods (`ImportRetention`).

## Decision

1. **A client observer is removed when no client has held a subscription to it, in any namespace, for `Observers.UnseenRemovalAfter` (default 7 days; zero turns the feature off).** Removal is the existing `IObserverRemover` path, so the same records are deleted and the same refusals apply. A reducer's read model data is not deleted. When the client connects again the observer is registered as new and replays from the start, like any new observer; this includes reactors, so their side effects run again for the whole history. The decider chose this knowingly.
2. **Scope: client reactors and reducers. Client-declared projections are not included.** Their subscription is the kernel's (`Owner == Kernel`), removal also drops generations and read-model bindings (a different blast radius, handled by its own manager), and a projection keeps serving queries whether or not its declaring client is connected. Webhooks, kernel reactors, event store subscriptions and anything with `Owner != Client` are never candidates.
3. **"Seen" is a durable `LastSeenAt` per observer per namespace**, a side field of the observer state document beside `RunningStateChangedAt`: an optional field in MongoDB, a nullable column in SQL (`v19_172_...` migration), written only through its own storage methods so `Save` of the observer's state neither depends on nor clears it. It is written when a client subscribes (once per subscription), when the last client target unsubscribes or disconnects, and, so that an observer subscribed for weeks is not mistaken for an unseen one, refreshed by the watchdog tick while a client holds the subscription, at most once per `min(1 hour, UnseenRemovalAfter / 4)`. It is not written on any other tick. While the setting is zero nothing is written.
4. **The sweep reads storage, not grains.** One silo-local sweeper (the `ImportRetention` pattern) runs every `Observers.UnseenRemovalSweepInterval` (default 1 hour), per event store: it lists the client-owned reactor and reducer definitions, then per namespace reads `LastSeenAt` for those ids in batches of `Observers.UnseenRemovalBatchSize` (default 100). Only an observer whose `LastSeenAt` is older than the period in every namespace that has state for it becomes a candidate. Only candidates are activated, for the existing removal guard.
5. **The clock for existing records starts when the new version first sees them.** An observer with no `LastSeenAt` is never a candidate: the sweep sets `LastSeenAt` to now where it is absent (an atomic set-if-absent per document), so everything that exists at upgrade has the full period before its first possible removal.
6. **Never removed:** a subscribed observer (the removal guard), an observer in any running state other than disconnected or suspended (replaying, catching up, quarantined, initializing, changing definition, active), one that owns failed partitions or has an unfinished job, anything kernel-owned, and any observer whose definition is not client-owned. A candidate the guard refuses has its `LastSeenAt` refreshed, so it is not retried every hour.
7. **Two nodes and rolling upgrades.** The sweep is idempotent: the clock start is set-if-absent, removal of an already removed observer reports not found, and a second sweeper finds nothing to do. No coordination is needed for correctness; the sweep is jittered so two nodes rarely overlap. An older node does not write `LastSeenAt`; an observer subscribed through an older node is therefore refreshed only by the guard path above, never wrongly removed because the guard refuses a subscribed observer. An older node ignores the field in MongoDB, and in SQL its `Save` may clear it, which only restarts that observer's clock.
8. **Every removal is logged at information level** with event store, namespace and observer id, and counted by the counter `chronicle.observers.unseen_removed`.

## Options considered

- **Do nothing; operators run `RemoveObserver`.** Rejected: nobody does, and the cost scales with tenants.
- **Remove on the first sweep when there is no last-seen value.** Rejected by the decider: an upgrade would delete every observer whose client happens to be down for a deploy.
- **A flag "has a client" written on subscribe and cleared on unsubscribe, instead of a refreshed timestamp.** Rejected: a silo that dies leaves the flag set forever, and the observer is then never removed.
- **Write `LastSeenAt` on every watchdog tick.** Rejected: it is a storage write per observer per minute, which is what 0030 and 0034 are trying to remove.
- **Find candidates by activating every observer and asking it.** Rejected: tens of thousands of activations to find none.
- **A reminder per event store instead of a silo-local timer.** Possible, and single-owner by construction, but it needs something to register one reminder per event store at startup and when a store is added; the silo-local sweeper is idempotent and has precedent. Revisit if two sweepers prove noisy.
- **Include client-declared projections.** Rejected, see decision 2.
- **A separate collection of last-seen times.** Rejected: two documents to keep consistent for one fact, and a new migration in every provider for no gain over a side field that already has a precedent.

## Default if unanswered

Observers of renamed or deleted client handlers stay forever, as today.

## Timeline and scope

- **In scope:** the field and its storage in MongoDB, SQL and in-memory storage; writing it from the observer; the sweeper; settings, log, counter, documentation.
- **Out of scope:** projections, webhooks, kernel observers; deleting reducer read model data; any client or wire change; a visible "unseen since" marker in the Workbench or CLI.
- **Release:** `minor` (new settings, new default behavior).

## Verification

- **Done when:** with the default setting an observer whose client has not subscribed to it for seven days is removed with its records, and nothing else is; an upgraded kernel removes nothing for the first seven days; with the setting at zero no `LastSeenAt` is written and nothing is removed.
- **Verify by:** specifications for the sweep (eligibility, clock start, idempotence, refusal paths, read model untouched, a returning client registering as new), for the observer's writes, for storage in each provider, and for the defaults and binding of each setting; the documentation gate.

## Consequences

- A kernel stops accumulating the records and activations of observers nobody uses.
- An observer removed by mistake of timing (a client away for more than the period) replays from the start when it returns. Operators who cannot accept that for a reactor with side effects set `unseenRemovalAfter` to `0`, or to a period longer than the longest outage they expect.
