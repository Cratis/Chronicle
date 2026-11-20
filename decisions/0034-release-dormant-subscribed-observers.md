---
id: 0034-release-dormant-subscribed-observers
title: Persist the subscription so a subscribed observer with nothing to do can be released, woken by the appended events queue, and stop costing a resident activation, keeper calls and a per-namespace stream
status: accepted
stage: none
class: contract
reversibility: costly
proposed: 2026-11-19
decided: 2026-11-19
decider: einari
applies-to:
  - Source/Kernel/Core/Observation/Observer*.cs
  - Source/Kernel/Core/Observation/States/**
  - Source/Kernel/Core/EventSequences/AppendedEventsQueue*.cs
  - Source/Kernel/Core/Observation/Reactors/Clients/**
  - Source/Kernel/Core/Observation/Reducers/Clients/**
  - Source/Kernel/Grpc/Observation/**
  - Source/Kernel/Core/Configuration/Observers.cs
  - Source/Kernel/Core/Configuration/Clients.cs
  - Source/Kernel/Core/Observation/Dormant*.cs
  - Source/Kernel/Core/Observation/DurableSubscriptionRecords.cs
  - Source/Kernel/Storage/Observation/ObserverState.cs
  - Benchmarks/Chronicle.Benchmarks.Kernel/**
---
<!-- markdownlint-disable-next-line MD025 -->
# Persist the subscription so a subscribed observer with nothing to do can be released, woken by the appended events queue, and stop costing a resident activation, keeper calls and a per-namespace stream

Proposed on 2026-11-19. It is accepted only when the decider (einari) says so; until then it binds nothing. It extends [decision 0030](./0030-release-idle-unsubscribed-observers.md): that record keeps every subscribed observer resident and defers, as a design of its own, the option "persist the subscription and let the appended events queue activate the observer". This is that design. It does not contradict 0030's decisions 2 to 5 (releasing unsubscribed idle observers stays exactly as it is). It would replace only 0030's decision 1, "subscribed observers stay resident", for the observers this record makes releasable, and it names the conditions under which that holds. If accepted, 0030 gets a dated banner pointing here; its text is not edited. It leans on [decision 0018](./0018-partition-ownership-protocol.md) (who may deliver to a partition), [decision 0011](./0011-a-type-set-change-quiesces-the-observer-through-orleans-drain-and-admission.md) and [decision 0010](./0010-observer-event-types-are-the-union-of-registered-definitions.md) (durable observer membership), and is written to survive [decision 0004](./0004-ordered-lanes-observer-delivery.md) (proposed, not decided).

> **2026-11-19 - accepted by einari.** The design direction is accepted. The decider's answers settle the open questions below; the text of the record is unchanged.
>
> 1. Direction accepted. It narrows [decision 0030](./0030-release-idle-unsubscribed-observers.md) decision 1 only, for the observer kinds in scope; 0030 carries a banner that points here and its text is not edited.
> 2. Dormant release is on by default at 900 seconds (the value of 0030's idle deactivation); `0` turns it off. Server-level, in the `Observers` configuration section.
> 3. A lost wake-up may delay delivery to a dormant observer by at most 15 minutes: the safety-net wake defaults to 900 seconds, jittered, server-level.
> 4. The durable subscription is an optional field on the observer state document (DP1 option A).
> 5. Scope of the first delivery: client reactors, client reducers and projections (Stages 1 to 3 together, each switchable by its setting). Webhooks, kernel reactors and event store subscriptions stay resident (Stage 4 not accepted yet). The multiplexed client stream (Stage 5) is not part of this delivery; no client SDK or wire-contract change is wanted.
> 6. A new, separate question, the automatic removal of observers no client has subscribed for a period, is decided in [decision 0035](./0035-remove-unseen-client-observers.md), so that either can be superseded alone.
>
> **Implementation status (2026-11-19).** The pull request that records this acceptance delivers decision 0035 only. The dormant release, wake, durable subscription and keeper change described here are *not* implemented in it, and the record's stage stays `none`. Nothing in this record is claimed as done.

## Context

### What a production kernel looks like now

Measured on a production kernel, one week after the measurements in 0030 and #5792:

- Two silos hold 41-60 thousand grain activations each, up from 19-31 thousand a week earlier. Cluster-wide that is about 44 thousand observer grains, 26 thousand client reactor grains and 8.5 thousand client reducer grains, across roughly 360 event store/namespace pairs. The count is the product of namespaces and observers per namespace, so it grows with every tenant.
- About 0.15 events per second are appended across the whole cluster. At any moment almost every subscribed observer is dormant: it has no event to handle and nothing pending.
- Live heap is 2.5-3.9 GB per silo against a 6.1 GB GC hard limit (8 GiB container), and one silo has thrown `OutOfMemoryException`.
- While idle each silo handles 18-20 thousand grain calls per second (about 12 per observer per minute, from the per-observer watchdog and the client subscription keeper), allocates 255-278 MB/s and uses about 1.5 cores.
- Each client reactor or reducer, in each namespace, holds one open gRPC stream on the silo (about 12 thousand on one silo), each with HTTP/2 stream state, pipes, a DI scope and an authentication principal.

Separate work reduces the cost of one resident observer and the idle calls. This record is about not needing a resident activation, and as far as the design allows not needing a resident stream, for an observer that has nothing to do. 0030 measured about 44-46 KiB retained per subscribed observer and about 5 KiB per released one (`Benchmarks/Chronicle.Benchmarks.Kernel --observer-footprint`); that gap is the prize for the observer grain, and it is the reason this record exists.

### Where a subscription lives today

Everything below was read in the code at the commit this record is written against.

- **The subscription itself is activation state.** `Observer._subscription` (`Observer.cs`) is an `ObserverSubscription`: observer id and key, event types, `SubscriberType` (a CLR `Type`), the silo, `Arguments`, `IsReplayable`, `Filters`, `GenerationDelivery`, and `Targets` (one per connected client instance, each with a `ConnectedClient`). Nothing in it is persisted. `Observer.ClientSubscriptionRestoration.cs` says so: "the subscription and the superseded registrations live for the activation only".
- **The queue holds the other half, also in memory.** `AppendedEventsQueue._subscriptions` is a list of `AppendedEventsQueueObserverSubscription` (observer key, event type ids, filters, all-types flag). `AppendedEventsQueues` (one per event sequence, `[KeepAlive]`) owns `Events.Queues` queue grains (default 2, `[KeepAlive]`) and a router that picks the queues whose union of event types matches a batch (`AppendedEventsQueueRouter.GetQueuesToDeliverTo`). The queue does call `GetGrain<IObserver>(key)` when it creates a lane (`RouteToLane`), so it already activates an observer it holds a subscription for. It holds none for a released one, because `Observing.OnLeave` unsubscribes and `Observer.OnDeactivateAsync` transitions to `Disconnected`.
- **A freshly activated observer ignores pushes.** `Observer.ShouldHandleEvent` returns false when `_subscription.IsSubscribed` is false, when the observer is not `Active`, or while it is preparing a catch-up, and `Handle` then only notes the held-back events. A push is a doorbell the observer cannot answer until it has restored its subscription and reached `Observing`.
- **A subscription is re-established by `Subscribe`, which re-reads and re-routes.** `Subscribe` reloads state, claims the definition, and `Routing.OnEnter` compares the stored cursor (`NextEventSequenceNumber`) with the tail. When the observer is behind and has catch-up work, `Routing` calls `Observer.CatchUp()`, which starts a catch-up job (`Observer.Catchup.cs`, `StartCatchUpJob`). Otherwise it goes to `Observing`, whose `OnEnter` subscribes to the queue and then checks for events missed in between (`GetNextSequenceNumberGreaterOrEqualTo`), sending the observer back to `Routing` if there are some. So today a subscription restored on an observer whose cursor is behind is a catch-up job, not a live resume.
- **The durable parts already exist.** `ObserverDefinition` (event types, event sequence, type, owner, `IsReplayable`, `GenerationDelivery`) and `ObserverState` (cursor, running state, failed and catching-up partitions, partition claims and ownership revision of decision 0018, type-set control of decision 0011, delivery fences, alert and quarantine state) are stored and restored on activation (`Observer.OnActivation`, `RestoreDeliveryFences`). Retry of a failed partition is an Orleans reminder (`Observer.Deferral.cs`, `Observer.Failing.cs`).

### What is lost when an observer deactivates, and what puts it back

On deactivation the observer unsubscribes from the queue and sets `_subscription` to `Unsubscribed`. What is lost: the subscriber type, the targets and their `ConnectedClient` records, the silo, the filters, the superseded registrations, `_subscriptionSetupFailed`, and the queue entry. Per kind of observer, what re-establishes it:

| Observer kind | Re-established by | Cadence | Source |
| --- | --- | --- | --- |
| Client reactor or reducer | The gRPC stream handler's keeper: `ClientSubscriptionKeeper.KeepWhileConnected` calls `Reactor.IsSubscriptionIntact` (which calls `IObserver.IsClientTarget`) and, when not intact, `Reactor.KeepSubscription`, which calls `KnowsClient` and `RestoreClientSubscription` with the client's definition | Safety net every jittered `connectedClients.subscriptionCheckIntervalSeconds` (60 s by default) and, while a subscription is not confirmed intact or after a silo leaves, every `reviseIntervalSeconds` | `Grpc/Observation/ClientSubscriptionKeeper.cs`, `Grpc/Observation/Reactors/Reactors.cs`, `Grpc/Observation/Reducers/Reducers.cs`, `Core/Observation/Reactors/Clients/Reactor.cs`, `Core/Observation/Reducers/Clients/Reducer.cs` |
| Projection | `ProjectionsManager.SubscribeIfNotSubscribed` on registration, on kernel start and, for every namespace, when a silo departs (`ProjectionsManager.SiloDepartures.cs`) | Event driven, plus timers that stay resident | `Core/Projections/ProjectionsManager*.cs` |
| Kernel reactor | `Reactors.DiscoverAndRegister(eventStore, namespace)`, a singleton run when a namespace starts | On namespace start | `Core/Observation/Reactors/Kernel/Reactors.cs` |
| Webhook | The `Webhooks` manager on registration and through the reminder `webhook-subscribe:` it registers when a subscribe fails | Registration and reminder | `Core/Observation/Webhooks/Webhooks.cs` |
| Event store subscription | `EventStoreSubscriptionsManager` through the reminder `event-store-subscription-subscribe:` (one minute period) | Reminder | `Core/Observation/EventStoreSubscriptions/EventStoreSubscriptionsManager.cs` |

For client observers the stream handler is the owner of the "is the client still there" fact: it holds the open stream, the delivery delegate in the silo-local `ReactorMediator` and its reducer counterpart (keyed by observer, connection, event store and namespace) and the client's definition in a closure. The `Reactor` and `Reducer` grains (keyed by observer, event store, namespace, event sequence and connection) persist the client's definition at registration and afterwards only serve the keeper; they have no `[KeepAlive]` and stay resident because the keeper keeps calling them. `ConnectedClients` (one per silo, `[KeepAlive]`) keeps, in memory, the set of observers each connection serves (`RecordObserver`) and hands it to `IClientDepartureNotifier` when the connection ends.

### Why 0030 stopped where it did

0030 correctly found that releasing a subscribed observer today stops live delivery until something calls `Subscribe` again, and that for client observers the keeper's calls reactivate the observer at once. Since then the keeper was made cheaper (#5816: a confirmed-intact subscription is checked once per safety-net interval), which makes releasing a client observer possible in principle but, with the subscription still in memory, would leave live delivery stopped for up to that interval after each release. The remaining prerequisite is the one 0030 named: a durable subscription, a queue that keeps its key across the observer's deactivation, and an activation path that resumes without a full catch-up.

### What changes if nobody chooses

0030 stands. Every subscribed observer, and every client reactor and reducer grain kept warm by its keeper, stays resident; resident memory and idle calls keep growing in proportion to tenants times observers, with the cluster already near its limit on one silo. The levers that remain without this record are operational and bounded: raise `connectedClients.subscriptionCheckIntervalSeconds` and `observers.watchdogInterval` (fewer idle calls, no memory back), or add silos and memory.

## Decision

This is a recommendation for the decider; the numbered points are the choices, each with the alternatives in "Options considered". Names of settings are proposals.

1. **A subscription becomes durable, in the observer's own state document.** The observer state gains one optional, top-level field that records the facts of the subscription that are not already stored: a `Kind` from a closed enumeration (client reactor, client reducer, projection, kernel reactor, webhook, event store subscription), whether it delivers pages, its `Filters`, and for client observers the list of targets as connection id plus the silo that terminates the connection. Event types, event sequence, replayability and generation delivery stay in `ObserverDefinition`. The CLR `SubscriberType` is never stored; it is derived from the kind. The silo is never stored for kernel-owned kinds; the activation uses its own. The observer writes the field in the same state write that already records a (re)subscription and clears it in `Unsubscribe`, guarded by the ownership revision of decision 0018.
2. **The queue keeps a subscription across the observer's deactivation and wakes the observer by delivering to it.** Deactivating a subscribed observer that is dormant (point 4) no longer removes its queue entry. On activation a queue seeds its entries from the durable records of its event sequence, not only from observers that happen to be resident. A push is a doorbell, not the data.
3. **A woken observer resumes from its cursor without a catch-up job, and the log is the source of the data.** On activation with a durable record the observer restores its subscription, validates its targets, and reaches `Observing` by a resume path that reads the events from its persisted cursor up to the tail it captured, delivers them as live pages under the rules of decision 0018, and falls back to the existing spill-to-catch-up path when the gap is larger than a bound or anything is not clean. The pushed batch is not trusted as data; it only caused the activation.
4. **Dormant means: subscribed, `Observing`, and nothing owed.** A subscribed observer is released after `observers.dormantAfter` seconds (default off in the first release; `0` is off) when every condition of 0030's `IsIdle` holds except "has no subscription", plus: its durable record is written, no live lane holds events, no delivery fence is active, no partition has failed or is claimed, and it is not quarantined, replaying or catching up. Anything owed keeps it resident, exactly as in 0030.
5. **The watchdog's duties for a released observer are taken over, not dropped.** Client liveness is taken over by the departure notification and silo-departure sweeps, which read the durable targets; the missed-push and stranded-subscription checks by the queue's seeding from storage plus a jittered safety-net wake that runs one watchdog pass and releases again; everything that needs a job, a failed partition, a reminder or an alert keeps the observer resident by definition (see "Watchdog duties").
6. **The keeper stops activating observers and stops needing the per-namespace `Reactor` and `Reducer` grains.** Once the subscription is durable, "intact" is true while the stream is open on a live silo. The stream handler (which already holds the client's definition) checks the observer only when a silo departs, when the stream is registered, and at a long safety net, not through the `Reactor`/`Reducer` grain. Those grains are then collected by Orleans.
7. **One stream per client connection, instead of one per reactor, reducer and namespace, is a separate, additive wire change.** It is described and costed here because it is the largest remaining per-tenant cost, but it needs client SDK work and its own acceptance; this record does not ask for it (Stage 5).
8. **It ships in independently reversible stages behind settings, and the wake-and-release stages are enabled only behind the cluster compatibility floor** that decisions 0017, 0018 and 0004 already share ("one floor, not three").

## Decision points

### DP1. What the durable subscription contains, and who owns it

| Option | What it is | Consequences |
| --- | --- | --- |
| **A. Optional field on the observer state document (recommended)** | `Kind`, page-delivery flag, `Filters`, client targets (connection id, terminating silo). Written and cleared by the observer. | One owner and one write path, the same shape decisions 0011 and 0018 already used to add durable fields: a new optional top-level field in MongoDB (`$set` of named fields, unknown elements ignored by older kernels) and a nullable column in SQL. The queue's seeding query is `IObserverStateStorage.GetAll()` for a namespace, already part of the storage contract. Cost: the field lives in every provider (MongoDB, SQL, in-memory) and must be listable by event sequence. |
| B. A new subscription collection keyed by event store, namespace, observer | A table of its own, read by the queues and written by the observers. | Cleaner to index by event sequence, but two documents must stay consistent (cursor and subscription), a new migration in every provider, and a new thing an older kernel does not know to clear. |
| C. Derive it, store nothing new | Rebuild the subscription from `ObserverDefinition` plus the `Reactor`/`Reducer` grain state plus `ConnectedClients`. | No schema change, but the definition of a client observer is stored per connection (the `Reactor` grain key includes the connection id), the connection to silo mapping is in memory on one silo, and Webhooks and projections have their own sources. It cannot tell an observer that was subscribed and is dormant from one that was never subscribed or whose client left, which is the distinction this whole record depends on. Rejected. |

The record is advisory, never authority: before delivering, a woken observer validates each target against `ConnectedClients` (as `Observer.CheckConnectedClient` does today) and clears what no longer holds. A stale record, for example left by an older silo that unsubscribed without knowing the field, costs one wake and then repairs itself.

Not decided from the code: whether `Filters` and the page-delivery flag are fully derivable from the stored `ReactorDefinition`/`ReducerDefinition`, which would shrink the field to just the targets. They are needed on the observer side, and the definition is stored per connection, so the recommendation stores them.

### DP2. How the queue finds and wakes subscribers that have no activation

| Option | Consequences |
| --- | --- |
| **A. The queue keeps entries across deactivation and seeds from storage (recommended)** | Smallest change: `RouteToLane` already activates the observer through `GetGrain<IObserver>`. The per-append cost does not change, because a dormant observer's entry is the entry a resident observer already has: the router still picks queues by the union of event types and the queue still matches per subscription. The new costs are one read of the namespace's durable subscriptions when a queue activates (about 360 such reads at a kernel start, to be staggered), and one activation per matching dormant observer per append batch. |
| B. A per-namespace registry grain matches event types and activates observers | Separates "who is subscribed" from the queue, but duplicates the router and adds a hop to every append. Rejected unless 0004's reader replaces the queue. |
| C. Poll only: a sweep compares each dormant observer's cursor with the tail | No push path to keep correct, but delivery latency becomes the sweep interval and every sweep costs a read per dormant observer. Kept as the safety net, not as the mechanism. |
| D. An Orleans reminder per dormant observer | Tens of thousands of reminder rows and a reminder tick per wake. Rejected; reminders stay what they are today, for failures and retries. |

Seeding is not free of ordering: an append can arrive while a queue is activating and still empty. The existing rule for an unseeded queue applies ("a queue left unseeded keeps receiving every batch until it is reconciled": `AppendedEventsQueues.SeedRouter`), and the safety-net wake covers the rest.

The new risk is a **wake storm**: an event type observed by many observers in a namespace wakes all of them for one append, where today they are already active. At 0.15 events per second cluster-wide this is bounded by (appends per second) times (dormant observers per matching event type per namespace). The code cannot say how many observers share an event type in the production kernel; that number decides whether the wake path needs admission control (the registration gate `observers.maxConcurrentSubscriptions` is the existing precedent). The idle period is the self-regulating part: a namespace that receives events often never lets its observers go dormant.

### DP3. How a woken observer resumes without a full catch-up, and without losing or doubling events

Today a restored subscription over a cursor that is behind ends in a catch-up job (`Routing.EvaluateState` then `Observer.CatchUp`). For an observer that was dormant for hours and wakes for one event, a job per wake is the wrong unit.

| Option | Consequences |
| --- | --- |
| **A. Inline resume from the cursor, with the existing catch-up as the fallback (recommended)** | The activation restores the subscription, subscribes to the queue, captures the tail, reads from its cursor to that tail with the same index the catch-up uses (`ObserverKeyIndexes`, `GetNextSequenceNumberGreaterOrEqualTo`), and delivers pages through the live path under 0018's rule that a partition with no claim is owned by live delivery. A gap over a bound, any failed or claimed partition, or any read failure falls back to `CatchUp()` unchanged. No-loss comes from the cursor and the log; the push is not the data. |
| B. Always catch up | Correct today, no new code, but a job (and its state writes and watchdog nudges) per wake. This would turn the saving in memory into a cost in job churn. |
| C. Trust the pushed batch | The batch that woke the observer is delivered directly when its first relevant event equals the cursor. Fast, but `Handle` drops pushes until the observer is `Active` today, and a lane treats a dropped push as delivered; correctness would rest on a check that races the delivery. Rejected. |

The no-loss and no-double-delivery argument is the one `Observing.OnEnter` already makes: the queue subscription is taken first, then the log is checked from the cursor, so an event is either before the captured tail (read from the log) or after it (arrives by push), and events below the cursor are not delivered again. Delivery stays at least once, as in 0018 ("a crash between a delivery and its record may redeliver"). What this record adds is the order of three steps on a woken activation: restore the subscription, subscribe to the queue, then read from the cursor. Partition claims, failed partitions and delivery fences keep their meaning because an observer that holds any of them is not dormant (DP4) and so is never in this path.

Not established from the code, and to be settled by a spike before Stage 3 ships: whether the existing page delivery on `Observing` can be reused for a bounded read from the cursor without the job machinery, or needs a small new inline reader; and that `HandleEventsForPartition` discards, not redelivers, events below the cursor. Both are likely but I did not trace them.

### DP4. What "dormant" means, and when an observer is released

Dormant is a state the observer can compute from fields it already holds, like 0030's `IsIdle`, with the subscription condition inverted and three additions: the durable record is persisted, the queue lane for this observer is empty and closed, and no delivery fence is active. The timing is the same idle period mechanism: a watchdog tick that finds the observer dormant starts a period, any tick that does not resets it, and when `observers.dormantAfter` has elapsed it calls `DeactivateOnIdle()`. `OnDeactivateAsync` must then neither transition to `Disconnected` (which would unsubscribe) nor write; it keeps the queue entry and leaves the durable record. The first release applies to client reactors, client reducers and projections. Webhooks (probes, delivery fences), kernel reactors and event store subscriptions are out of the first release and stay resident; they have their own managers and reminders, which this record did not trace to the end.

The recommendation is default off. A default of 900 s, like 0030's, is a decision for after the footprint and wake numbers exist.

### DP5. The watchdog's duties for a released observer

`Observer.Watchdog.cs` runs, per tick: `ReleaseWhenIdle`, ownership relinquish, webhook probe reconciliation, `CheckConnectedClient`, `RecoverIfStuck` (deferred routing, catch-up back-off, owed recovery after quarantine, job tasks, stranded preparation, `CheckNextSequenceNumber`, `CheckStrandedSubscription`), `NudgeStalledJobs`, flushing debounced progress, and alert reconciliation. A dormant observer is by definition one for which most of these have nothing to do. Who takes over the rest:

| Duty | While dormant | Taken over by |
| --- | --- | --- |
| Client liveness (`CheckConnectedClient` calls `ConnectedClientsLocator.Find` per target) | The client may leave | Client disconnect: `ConnectedClients` → `ClientDepartureNotifier` already calls `UnsubscribeIfMatchesClient` on every observer it knows for the connection (which activates a dormant one, once); silo death loses that in-memory set, so a per-namespace sweep over the durable targets on a silo departure (the same trigger `ProjectionsManager.CheckForDepartedSilos` uses) and the safety-net wake |
| Missed push, lost queue entry, stranded subscription (`CheckNextSequenceNumber`, `CheckStrandedSubscription`, `AppendedEventsQueue.IsSubscribed`) | A silo restart empties the queue; an append arrives while it is unseeded | Seeding from durable records at queue activation, plus the safety-net wake that runs one watchdog pass and releases again |
| Stalled or missing catch-up, replay and retry jobs (`CheckJobTasks`, `NudgeStalledJobs`) | None can exist | Not needed: an observer with a job, a catching-up or replaying partition is not dormant |
| Failed partition retry | None can exist | Not needed: failed partitions keep the observer resident (their retry reminders are durable and could later release it; out of scope here) |
| Alerts, quarantine | None can exist | Not needed: 0030's alert conditions and quarantine keep the observer resident |
| Definition ownership moved (`RelinquishOwnershipIfMoved`) | A registration elsewhere claims the definition | Runs on the next activation; the safety-net wake bounds the delay |
| Debounced progress writes | None pending by definition | Not needed |

The cost of the safety net is a choice: with about 44 thousand observers, a wake every 15 minutes is about 49 activations per second across the cluster, and every 60 minutes about 12 per second (simple arithmetic on the numbers above, not a measurement). Each activation reads the definition, state, failures and delivery fences (0030). Today the same observers cost 12 grain calls per minute each. The interval also bounds how long a lost push can delay delivery to a dormant observer; today that bound is the watchdog interval (60 s by default). That trade is one of the decider's questions.

Two operator-facing paths I did not trace and that may activate a dormant observer: `GetObserverInformation`/`ObserverInformationSnapshot` (which reports `IsSubscribed` from the activation) and the Workbench and CLI observer views. They should read the durable record, not wake the observer, or they will wake every observer whenever someone opens the list.

### DP6. Can the per-observer client stream and the client reactor and reducer grains stop being per namespace

Two different costs hide here.

**The `Reactor` and `Reducer` grains (26 thousand and 8.5 thousand activations).** These can go without any wire change: with a durable subscription the stream handler no longer needs them (Decision 6), so Orleans collects them. This is Stage 1 and is independent of releasing observers.

**The stream.** Today the client library holds one event store handle per namespace (`_eventStore.Namespace` in `Source/Clients/DotNET/Reactors/Reactors.cs`), and each registers every reactor as its own `Observe` call carrying one `RegisterReactor` (connection id, event store, one namespace, one definition, page delivery). `EventsToObserve` and `ReactorResult` carry no namespace; the stream identifies it. On the kernel `ReactorMediator` is already keyed by observer, connection, event store and namespace, so the delivery side is shaped for sharing a stream.

| Option | Consequences |
| --- | --- |
| **A. Keep one stream per registration for now (recommended for this record)** | No contract change. The per-stream cost stays. After Stage 3 the kernel can measure how much of the heap and CPU is streams rather than observers and decide with a number. |
| B. One stream per connection and reactor, multiplexing namespaces | A new additive RPC (for example `ObserveMany`) whose messages carry the namespace, with namespace-scoped delivery ids. Cuts streams by the number of namespaces a client serves. Needs **client and wire-contract changes** in every SDK (.NET, Kotlin, TypeScript, Elixir), a capability check so a new client falls back to the old RPC against an old kernel, and an answer to head-of-line blocking: one HTTP/2 flow-control window shared by every tenant's deliveries on that stream. |
| C. One stream per connection for everything | Fewest streams, the largest blast radius for one slow handler and for reconnect. Same contract change as B, worse isolation. |
| D. The kernel closes the stream of a dormant client observer and the client reopens on wake | Would also remove the stream cost for dormant observers, but needs a connection-level channel by which the kernel can tell a client to reopen; I did not verify one exists. Rejected for now. |

The wire contract must keep serving every released minor of the current major (`.github/workflows/wire-compatibility.yml`); a new RPC is additive and does not need a `major` label, but no existing message or RPC may change. This option is flagged in the staged plan as needing its own acceptance.

### DP7. Rolling-upgrade compatibility

- **Grain interfaces only grow within a major** (project rule). New grain methods (restoring from a durable record on `IObserver`, seeding on `IAppendedEventsQueue`) are added beside the existing ones, with `[Version]` raised on the interfaces that gain them (`IObserver` is at 10, `IAppendedEventsQueue` and `IAppendedEventsQueues` at 1; `OrleansContracts.txt` is the authority). No existing method changes.
- **State is additive.** The durable subscription is one new optional top-level field of the observer state, the pattern decision 0018 verified: older kernels ignore the unknown MongoDB element or nullable SQL column and leave it alone when they save, and `GrainPayloadContracts.txt` gates the payload. The field can be stale when an older silo owns the observer (an older observer unsubscribes without clearing it); validation on wake (DP1) repairs it.
- **Wake and release are enabled only behind the cluster compatibility floor**, reusing the mechanism decisions 0017 and 0018 share (`IClusterReleases`), so that a silo that does not understand a dormant observer never owns one and an older queue is never left as the only one that can wake it. Before the floor the field is written in shadow, as 0018 does for claims.
- **Old clients keep working unchanged** in Stages 1 to 4: nothing on the wire changes; the stream the client opens is the stream the kernel serves.
- **Old queues and new observers**: a new observer that is released while its queue (on an older silo) still holds its entry is woken by that older queue exactly as a resident one is; an older observer deactivating unsubscribes as today. Mixed clusters therefore degrade to today's behavior, not to loss.
- Release label: `minor` (new settings, no breaking contract). Stage 5 is a wire addition and also `minor`.

## Options considered

- **Do nothing (0030 stands).** Rejected as the recommendation, kept as the default if unanswered. The cost scales with tenants times observers, and the cluster is already at its heap limit.
- **Remove `[KeepAlive]` and keep subscribed observers alive with `DelayDeactivation` while subscribed** (0030's first rejected option). Still rejected: it keeps the subscription in memory and would only trade one bookkeeping problem for another. A released observer must be able to be rebuilt, so the subscription has to be durable.
- **Persist the subscription in the queue instead of the observer.** The queue is per event sequence and `[KeepAlive]`; storing there makes the queue the second authority for a thing the observer owns and breaks the single-writer reasoning of 0018. Rejected for DP1.
- **Raise `connectedClients.subscriptionCheckIntervalSeconds` and `observers.watchdogInterval` only.** Available today, no code, and worth doing in the meantime. It reduces idle calls, not residency or memory, and it lengthens how long a lost subscription goes unrepaired.
- **Move observer subscriptions to the ordered lanes engine of 0004 instead.** 0004 makes append notifications only wake a durable reader, which is the same idea; it is Proposed, dormant until a cluster finalization, and larger. This record is built so its durable record and wake semantics carry over: the record is the observer's, not the queue's, and the wake is "a notification activates a reader that resumes from its durable cursor". If 0004 is accepted first, Stages 2 and 3 are reconsidered against it.
- **Per-observer Orleans reminders as the wake.** Rejected in DP2.

## Staged delivery plan

Each stage ships independently, is reversible by a setting, and has a measurement. Estimates are ceilings from numbers already in this record or the repository, not predictions; they must be replaced by measurements before a stage is called done. The benchmark modes are `Benchmarks/Chronicle.Benchmarks.Kernel --observer-footprint` (live heap per observer, subscribed and released) and `--keeper-calls` (keeper calls per second and per observer per minute, CPU); each stage extends them where noted.

| Stage | What ships | Setting (off = today) | Expected saving (estimate) | How it is measured |
| --- | --- | --- | --- | --- |
| 0 | Measurement: a `--dormant` phase for `--observer-footprint` (subscribed, durably recorded, released, woken) and a `--wake` mode counting activations and storage reads per wake. No product behavior. | none | none | The numbers every later stage is compared with |
| 1 | The keeper stops calling `Reactor`/`Reducer` grains and observers in steady state; the stream handler checks on registration, silo departure and a long safety net. The `Reactor`/`Reducer` grains become collectible. Without the durable record the long safety net keeps using the existing `IsClientTarget` call, so this stage needs no schema change. | `connectedClients.keeperMode` (`legacy` or `silo-local`) | Up to the 34.5 thousand `Reactor` and `Reducer` activations; at the 46-61 KiB per activation of #5792 that is at most about 1.5-2.0 GiB cluster-wide, about 0.75-1.0 GiB per silo with two silos, against 2.5-3.9 GB live heap. It is a ceiling: that regression is per activation of any type. Fewer idle calls by the keeper's share, which the code cannot give | `--keeper-calls` before and after (calls per observer per minute, busiest second, CPU); activation counts by grain type and live heap in production |
| 2 | The durable subscription is written and cleared, the queue seeds from it, in shadow: nothing is released and nothing wakes. A metric counts disagreements between the seeded set and the live set. | `observers.durableSubscriptions` (`off`, `shadow`) | None; one extra field per observer state write | The disagreement metric stays zero for a full week of production traffic including a rolling upgrade |
| 3 | Dormant release and wake for client reactors, client reducers and projections: queue entries survive deactivation, inline resume from the cursor, the safety-net wake, departure sweeps. Enabled only behind the compatibility floor. | `observers.dormantAfter` (`0` off), `observers.dormantSafetyNetSeconds` | At most the gap of 0030 for 44 thousand observers: about 39-41 KiB each, about 1.6-1.7 GiB cluster-wide, about 0.8-0.9 GiB per silo, plus their `ObserverAlerts` companions. Watchdog ticks for dormant observers go away: 44 thousand at a 60 second interval is about 730 ticks per second cluster-wide, replaced by the safety-net wakes of DP5. Real saving scales with the share that is dormant, which at 0.15 events per second should be most | `--observer-footprint --dormant`; `--wake`; in production: observer activations per silo falling towards the number of non-dormant observers, live heap after GC, idle grain calls per second, wakes per second and events delivered with and without a catch-up job |
| 4 | Webhooks, kernel reactors and event store subscriptions, each after its own analysis, or none if their reminders and probes make it not worth it. | `observers.dormantKinds` | Remaining resident observers, not estimated | As Stage 3 |
| 5 | Multiplexed client stream (DP6 option B), a new additive RPC and SDK support in each client. Own acceptance. | A client option; kernels without it keep the old RPC | Streams cut by the number of namespaces a client serves; not estimated, the cost per stream was not measured | Open streams and heap per silo against namespaces served; a client against an old kernel falls back |

Rollback of any stage is its setting. The durable field stays in storage after a rollback and is ignored, which is harmless.

## Risks

- **Wake storms** (DP2): one append waking every observer of an event type in a namespace; bounded by admission control and by the idle period, not eliminated.
- **A lost doorbell**: a push dropped because the observer was not yet `Active`, a queue emptied by a silo restart, or a stale durable record. The log and the cursor make it a delay, bounded by the safety-net interval, not a loss; the interval is a latency promise the decider has to accept.
- **Stale durable record** after an older silo unsubscribed, or after a client left while the `ConnectedClients` set was lost with its silo. Validated and repaired on wake; costs one activation.
- **Two writers**: a woken observer delivering while a catch-up job or recovery is starting. Excluded by the dormancy definition (no claims, no failed partitions, no jobs) and by 0018's single-writer rule; the inline resume must take the same acquisition path as any other writer. This is the part most in need of review and specs.
- **Operator views waking observers** (DP5): listing observers must not activate them.
- **Metrics for released observers**: per-observer gauges are released on deactivation (`ReleaseGauge`); how dormant observers appear in the bounded observer gauges (#4610) was not checked.
- **Shared-state interaction with decisions 0010/0011**: a type-set change quiesces by removing the queue subscription and draining jobs; a dormant observer must be woken (or the change applied to its durable record and entry) before a quiesce, or its membership drifts. Not traced.

## Default if unanswered

0030 remains in force: every subscribed observer, and every client reactor and reducer grain kept warm by its keeper, stays resident until its silo stops. The cost is the one measured above and it grows with tenants: tens of KiB per observer per namespace, thousands of idle grain calls per second per silo, a heap close to its hard limit on a kernel handling 0.15 events per second. The interim operational levers are raising the keeper and watchdog intervals and adding silos or memory. Nothing in this record is done unless it is accepted.

## Timeline and scope

- **In scope:** the durable subscription for client reactors, client reducers and projections; queue seeding and wake; the inline resume; dormancy and its release; the watchdog takeover; the keeper change and the collectible `Reactor`/`Reducer` grains; the benchmark modes; documentation of the settings in `Documentation/hosting/configuration/observers.md`.
- **Staging:** Stage 0 and Stage 1 first (they need no schema change and no floor). Stage 2 one release before Stage 3, so the shadow field has been in storage across a rolling upgrade.
- **Out of scope:** reducing the cost of one resident observer (separate work); releasing observers that own failed partitions, jobs, alerts or webhooks; the `[KeepAlive]` grains `ConnectedClients`, `AppendedEventsQueues`, `AppendedEventsQueue` and `ProjectionChangesetNotifier`; ordered lanes (0004); the multiplexed stream, which is Stage 5 and needs its own acceptance; any change to existing grain methods or wire messages.
- **Release:** `minor` per stage.

## Verification

- **Done when:** with every setting off, behavior is identical to today; with Stage 3 on, an idle client reactor, reducer or projection observer whose subscription is intact is deactivated after `observers.dormantAfter`, keeps its queue entry, is woken by a matching append, delivers that event to its client exactly once without starting a catch-up job, and is not woken by an append of an event type it does not observe; a dormant observer whose client leaves, whose silo dies, or whose queue is rebuilt after a restart is repaired within the safety-net interval or sooner; per-silo observer activations and live heap fall toward the number of non-dormant observers; and no event is lost or delivered out of order in a rolling upgrade across the compatibility floor.
- **Verify by:**
  - Specs under `Source/Kernel/Core.Specs/Observation/for_Observer` for: dormant release, a subscribed observer that is not dormant (each owed condition), a woken observer with no gap, with a gap under and over the bound, with a stale record, with a failed partition arriving during the wake, and the setting at `0`; specs for the queue seeding from storage (including an append during seeding) and for `OnDeactivateAsync` keeping the entry; specs for the keeper in `silo-local` mode (registration, silo departure, safety net).
  - The extended `--observer-footprint` (subscribed, recorded, released, woken) and `--keeper-calls` before and after each stage, reported with the other numbers in the pull request that ships the stage.
  - An integration spec with two silo versions across the floor, covering old queue with new observer and new queue with old observer.
  - `bash Documentation/verify-markdown.sh` for the documentation of the settings.
  - In production: observer and `Reactor`/`Reducer` activations per silo, live heap after GC, idle grain calls per second, wakes per second, wakes that fell back to a catch-up job, and delivery latency of the first event after a dormant period.

## Open questions only the decider can answer

1. **Where the durable subscription lives.** The recommendation is an optional field on the observer state document (DP1 option A). The alternative is a separate collection. Which?
2. **The safety-net interval and the latency it promises.** Waking dormant observers every 15 minutes costs about 49 activations per second across the cluster at today's size; every 60 minutes about 12. A lost push then delays delivery to a dormant observer by up to that long, against up to 60 seconds today. Which bound?
3. **First-release scope.** Client reactors, client reducers and projections only, with webhooks, kernel reactors and event store subscriptions later or never?
4. **A default.** Ship `observers.dormantAfter` off and turn it on after the numbers, or on at a stated value like 0030's 900 seconds?
5. **Wake-storm tolerance.** Is a bounded number of wakes per second, or a per-namespace limit, acceptable as admission control, with deliveries to the rest delayed?
6. **Stage 1 on its own.** May the keeper stop calling observers in steady state (silo-local mode) ahead of the durable record, accepting a longer window for repairing a lost subscription?
7. **Operator surfaces.** Should a dormant observer appear as subscribed with a new "dormant" marker in the Workbench, CLI and API, without waking it? That is a visible contract.
8. **The multiplexed stream.** Is the decider willing to take on client SDK and wire-contract work in .NET, Kotlin, TypeScript and Elixir for Stage 5 if Stage 3 shows streams dominate, or should it be dropped from the plan?
9. **Ordering against 0004.** Proceed on the current delivery engine, or wait for the ordered lanes decision?
10. **Failed-partition observers.** Their retry is a durable reminder; may a later record let such observers be released too?

## What I could not establish from the code

- How many observers share an event type in the production kernel, which sets the size of the wake storm.
- Whether the existing live page delivery can serve a bounded read from the cursor without the catch-up job, and whether `HandleEventsForPartition` discards events below the cursor (DP3).
- The keeper's share of the 12 idle calls per observer per minute, and the cost of one stream; the benchmark modes exist for the first and the second needs a new one.
- How the Workbench, CLI and `GetObserverInformation` read observers, and whether they activate them (DP5).
- Whether `EventStoreSubscriptionsManager`'s one-minute reminder stops once subscribed, and the full lifecycle of webhook probes (DP4, Stage 4).
- Whether a connection-level channel exists by which the kernel could tell a client to reopen a stream (DP6 option D).
- How dormant observers appear in the bounded observer gauges (#4610), and the interaction with the quiesce of decision 0011 (Risks).

## Findings from the code, recorded while implementing Stage 2 (2026-11-19)

These resolve items of "What I could not establish from the code" and two of the Risks. They were read in the code, not measured.

- **Below-cursor events are discarded, not redelivered.** `Observer.HandleEventsForPartition` filters `events.Where(sequenceNumber >= _pageFloor ?? State.NextEventSequenceNumber)` and notes what it discarded. `Observing.OnEnter` already implements "queue subscription first, then read the log from the cursor" and routes back through `Routing` when events were missed, so the no-loss argument of DP3 holds for the existing path. What remains for the spike is only whether a bounded inline read can replace the `Routing` to catch-up job step.
- **Decision 0018's single-writer claims are in shadow in this code** (`Observer.ClaimGrants.cs`, `_shadowClaims`); nothing enforces them yet. A woken dormant observer therefore takes no claim: dormant already means no claims, no failed partitions and no jobs, which is 0018's own rule that a partition without a claim belongs to live delivery. A new `ClaimHolderKind` is not needed and would be a stored enum value older kernels do not know.
- **The compatibility floor is `IClusterReleases` plus a named boundary, not the generation floor.** `IClusterReleases.Summarize()` reports the published release of every silo (`Unpublished` and `Omitted` count the rest); `ClusterReleaseBoundary` holds named boundaries. The storage-backed `GenerationFloor` governs projection generations and is read at startup only. Release and wake are therefore gated by a boundary release evaluated at each release decision, because a rolling upgrade can put an older silo back; no stored floor is needed because the durable field is advisory.
- **The durable field is safe to add in every provider, and an older node leaves it alone.** MongoDB: older kernels ignore the unknown element (Arc's process-wide `IgnoreExtraElementsConvention`, as 0018 verified) and `ObserverStateStorage.Save` is an update of named fields, so an older save does not remove it; only an older node's `Rename` (load, insert, delete) drops it. SQL: a nullable `DurableSubscription` column added by migration `v19_173_0_1`; an older node's model neither selects nor updates it, and its `Rename` copies only mapped columns. A dropped or stale record is repaired on the next wake. The field is written as a `$set` (null clears), because the type-set guard pipeline renders `$set` operators only.
- **Metrics for released observers.** `ObserverGauges` registers an entry per resident observer and releases it in `OnDeactivateAsync`; the failed-partition, quarantine and running-state gauges only ever describe observers that cannot be dormant, so they lose nothing. `observer.resident` stops counting subscribed observers that were released, so a dormant-observers gauge fed from the queue's entries must ship with Stage 3.
- **Type-set change and the quiesce of 0010/0011.** The quiesce is not in `Core` yet (only its stored field and guard exist). A type set changes today through a re-registration that calls `IObserver.Subscribe...`, which activates the observer, rewrites its definition and replaces its queue entry by key, so a dormant observer is always woken by the change. The drift risk is confined to the queue entry that is seeded from storage: the durable record deliberately holds no event types, so seeding must read the observer definition and `SubscribesToAllEvents` together with the record and never cache types in it. When 0011's quiesce lands it has to wake a dormant observer, or drop its queue entry, before it drains.
- **Things that wake a dormant observer today, beyond the keeper.** `ProjectionsManager.SubscribeIfNotSubscribed` (start, registration, timers, and every namespace when a silo is dead), `GetObserverInformation`, `GetConnectedClientsForObserver`, `WaitForObserverCompletion`, `ConnectedClientsQuery.GetAllForObserver` and the connected-reducer lookup in `ReadModels`. The observer listing does not. Stage 3 has to change the projection manager and these reads or releasing projections will not last.

## As built (2026-11-20)

Stages 1 to 4 are built in one release. Stage 5 (the wire contract) is not touched.

**Settings.** `observers.durableSubscriptions` is `Off`, `Shadow` or `Release`, and defaults to `Release`. `observers.dormantAfter` (900 s, `0` off), `observers.dormantSafetyNet` (900 s, jittered per observer between 60% and 90%), `observers.maxConcurrentWakes` (0, automatic), `observers.dormantResumeMaxEvents` (1000) and `observers.durableSubscriptionCheckInterval` (15 minutes, `Shadow` only). `connectedClients.keeperMode` is `Legacy` or `SiloLocal` and defaults to `SiloLocal`. Every default is on.

**Release.** `Observer.Dormancy.cs` holds the predicate (subscribed, active, nothing owed, in scope, floor reached, setting on, period passed) and runs at the end of each watchdog pass. The observer asks its queue to keep the subscription, and the queue holds a per-observer entry seeded from storage. Only client reactors, client reducers and projections are in scope.

**Wake.** The queue rings a doorbell through the wake admission (`maxConcurrentWakes`) when an event the entry observes is appended. The safety net is driven from the queue with no per-observer timer. The woken observer resumes from its persisted cursor (`Observer.DormantResume.cs`): it subscribes to the queue first, reads from the cursor, delivers in order, and falls back to the catch-up job when the gap exceeds `dormantResumeMaxEvents`. A record that names a client that is gone is dropped and repaired on the wake.

**The watchdog's duties (DP5).** None is held per observer. A client that disconnects is found by `IClientDepartureNotifier.NotifyDormant`, which reads the durable subscriptions through `IDormantTargetSweep` (collected for a second and shared between connections that leave together). A silo that leaves is found by `DormantSiloDepartures` on every silo, which wakes the observers whose record names a client served from it. A stale record is validated and repaired on the wake. The removal of unseen observers (0035) reads the record: an observer whose record names a client is seen and never removed, and a record found stale on a wake records the last-seen time, so a client that left while the observer was dormant starts the clock then.

**The keeper (DP6, option "stay per namespace, stop calling").** The per-namespace client `Reactor` and `Reducer` grains and the gRPC streams are unchanged. What changed is that the stream's check no longer calls the client grain, which is what kept it and the observer resident. In `SiloLocal` mode, while the stream is open, a subscription is intact when the observer's durable subscription names the connection; the records of a namespace are read once and shared for 30 seconds by `IDurableSubscriptionRecords`. An observer with no record, a failed read, `Legacy` mode, a setting that does not apply or a mixed cluster fall back to the original two calls. `ProjectionsManager` treats a projection held by a record as subscribed, on start, registration, timers and silo loss. This is a deviation from the "stream registration, silo departure and a long safety net" shape of DP6: the checks keep the cadence they had (every `reviseIntervalSeconds` when unconfirmed, up to `subscriptionCheckIntervalSeconds` otherwise); what is gone is the call that woke the observer. A per-namespace stream that is never opened for a dormant observer (option "no stream until needed") was not attempted.

**Surfaces.** The observer listing reports `isSubscribed` from the record, which is true for a resident and a dormant observer alike. Reading one observer and reading its connected clients check `IAppendedEventsQueues.IsDormant` first and answer from storage and the record without activating it. No dormant marker is exposed: the contract types are generated from Core and adding a field is a wire change, left to Stage 5. `IsDormant` is `[AlwaysInterleave]`; without it a queue busy delivering made the call time out in the integration run.

**Telemetry.** `observer.dormant` (gauge per event store and namespace, fed from the queues, zero rather than absent when empty), `observer.dormant_releases`, `observer.dormant_wakes` (`WakeCause`: `event`, `safety_net`), `observer.dormant_wakes_delayed`, `observer.dormant_resumes` (`ResumeOutcome`: `cursor`, `catch_up`, `stale_record`). A released observer's gauge entries are released with its activation.

**Compatibility.** The grain interfaces only grew: `IObserver` v11, `IAppendedEventsQueue` v2, `IAppendedEventsQueues` v2 (one method `IsDormant` and the earlier `TryMarkDormant`). No stored or serialized payload changed. Release and wake are gated by `DormantObserverFloor` (19.173.0), evaluated at each decision, so a rolling upgrade releases nothing until every silo supports it, and returns to resident when an older silo joins.

**Deviations from the decision.**

- DP6 is the "stop calling" variant described above, not a removal of the per-namespace grains.
- A new observer is released only after its alert reminder ticked (one to two minutes after it subscribed, Orleans' minimum reminder period), so an observer is dormant a few minutes after subscribing.
- The `ObserverAlerts` companion grain is not touched. It is not `[KeepAlive]`, so Orleans collects it after its collection age (15 minutes by default); the benchmark run does not set that age, so its figures still count 2200 of them.
- A reducer read-model read that needs the connected reducer, and `WaitForObserverCompletion`, still activate the observer because they use it.

**Measurements.** `Benchmarks/Chronicle.Benchmarks.Kernel` `--observer-footprint --dormant`, 2000 reactor observers on one connection and 200 warm-up observers, one in-process silo with in-memory storage, a shared and loaded machine (so timings are noisy; heap and call counts are not). The baseline is the `Off` run taken before any dormancy existed; after is `Release` with `--dormant-after 30`.

| | Before (`Off`) | After (released) |
| --- | --- | --- |
| `Observer` activations resident | 2201 | 0 (of 2200 companions: 2200, see above) |
| Live heap over the baseline | +37.7 MiB (18.4 KiB per observer) | +10.9 MiB (5.6 KiB per observer) |
| Grain calls per observer per minute, production cadence (`--watchdog 60`, 125 s) | 1.47 | 0.06 |
| Bytes allocated per observer per minute, same | 11 904 | 1 122 |
| Process CPU, same | 0.005 cores | 0.004 cores |
| Grain calls per observer per minute, harness cadence (`--watchdog 1`, 30 s) | 87.9 | 0.23 |
| Wake of one observer (activation from nothing, median / p95 / max) | 0.01 / 0.03 / 0.14 ms (a resident call) | 0.42 / 1.90 / 7.03 ms |
| Wake of all 2000 in batches of 100 | 18 ms | 485 ms (0.24 ms per observer) |
| Subscribed, before release (`Release` writes the record) | 18.4 KiB per observer | 19.3 KiB per observer |
| Woken again | not applicable | 16.5 KiB per observer |

Raw logs are in the working record of the change and not committed. What the harness does not cover: the gRPC streams and the client `Reactor` and `Reducer` grains (so the keeper's saving is shown by the specifications and the integration scenario, not by a figure), the event-triggered wake through the queue (covered by the specifications and the integration scenario), cross-silo calls and serialization, MongoDB and SQL reads, and a quiet machine.

**Proof of the guarantees.** See the pull request. In short: `for_Observer/when_going_dormant`, `when_waking_from_dormancy`, `for_AppendedEventsQueue`, `for_WakeAdmission`, `for_DormantObserverFloor`, `for_DurableSubscriptionRecords`, `for_DormantTargetSweep`, `for_DormantSiloDepartures`, `for_ClientSubscriptionKeeper`, `for_ProjectionsManager` and `for_UnseenObserverRemoval` in `Core.Specs`, and `Integration/Kernel/for_DormantObserver` in a running kernel with MongoDB.

## Consequences

- A kernel's resident memory and idle work would follow the number of observers that have something to do, not tenants times observers, and the keeper and the per-namespace client grains stop being a per-tenant tax.
- The observer state gains a durable subscription and the queue gains a wake path; both are new things to keep correct, and both carry a floor and a shadow stage because of it.
- Delivery to a dormant observer carries one activation of latency and, when a push is lost, up to the safety-net interval.
- It narrows 0030's decision 1 for the kinds named, and leaves everything else in 0030 as decided. It does not touch the wire contract until Stage 5, which is separate.
