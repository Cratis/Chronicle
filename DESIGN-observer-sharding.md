# Design — Observer Sharding (W5.5)

**Status:** Design, not implemented. No production code accompanies this document.
**Scope:** Chronicle Kernel, `Source/Kernel/Core` — the live observation path.
**Baseline:** branch `perf/fu-L-sharding-design`, worktree HEAD `93999c2c1`.
**Audience:** kernel maintainers reviewing whether to fund, scope down, or reject this work.

Every claim about current behavior below cites `file:line` in this repository. Where the code does not
answer a question, the section says so explicitly and names the investigation that would settle it.

---

## 0. Executive summary

**Recommended shard key:** `(observerId, eventStore, namespace, eventSequenceId, shardIndex)` where
`shardIndex = FNV1a(partition) mod shardCount`, with `shardCount` **persisted per observer** in
`ObserverDefinition` — *not* `(observerId, partition)`.

**Recommended structural split:** demote the existing `Observer` grain to a **control plane**
(definition, subscription lifecycle, quarantine, failed-partition registry, job orchestration —
single activation, forever) and introduce an `ObserverShard` **data plane** grain that owns a
per-shard cursor and the live `Handle` path.

**The three findings that dominate the design:**

1. **Sharding the observer grain alone buys nothing.** The real serialization point on the live path
   is `AppendedEventsQueue.Dispatch`, which walks partition groups **sequentially**
   (`Source/Kernel/Core/EventSequences/AppendedEventsQueue.cs:436-473`) and waits for every observer
   of partition *A* before starting partition *B*. It does so **because there is one global cursor**
   (`AppendedEventsQueue.cs:432-434` states the reason outright). Per-shard cursors are the
   *precondition* for parallel dispatch; the grain split is the vehicle for the cursor split.

2. **The W4.5 striped lock cannot be deleted, and it is already insufficient today.**
   `ProjectionHandleLock` guards a **sink document**, not grain memory, and it is a **process-local**
   `[Singleton]` (`Source/Kernel/Core/Projections/Engine/Pipelines/ProjectionPipelineManager.cs:27,50`).
   Grain isolation per `(observer, partition)` cannot serialize a projection whose key resolution
   *collapses* partitions onto one document, and I believe the coarse path is **already racy across
   silos** on the catch-up path today (§8.3) — an unproven suspicion with a named test to settle it.

3. **A whole class of observers cannot be sharded at all.** Projections with
   `IsEventSourceKeyed == false` (child collections, joins, non-event-source key resolvers —
   `Source/Kernel/Core/Projections/Engine/Projection.cs:211-214`) depend on **global cross-partition
   sequence order**, which the kernel enforces deliberately and documents as load-bearing
   (`Source/Kernel/Core/Observation/Jobs/HandleEventsForObserver.cs:171-177`). Sharding by partition
   destroys exactly that order. §14 is a per-component can/cannot/unproven table.

**Biggest risk:** silent double-apply during a rolling upgrade, because a v(N) `Observer` and a
v(N+1) `ObserverShard` can both be live for the same observer, and **the repository has no
mixed-build clustering test** to detect it (`Integration/Clustering/Clustering.csproj:18` builds every
silo from the same `Core.csproj`). First release must be stop-the-world.

**Phased plan:** Phase 0 prerequisites (stable hash, constraint-commit ordering fix, three missing
test oracles) → Phase 1 ship the shard grain at `shardCount == 1` (behaviorally identical, proves the
plumbing) → Phase 2 `shardCount > 1` for **kernel-owned, event-source-keyed** observers with parallel
dispatch → Phase 3 client-owned observers, only after a client-boundary duplicate/skip spec exists.
Kill switch: `Observers.Shards = 1`. Collapsing projections and the append path: never.

---

## 1. What the live path does today

### 1.1 One activation, one cursor, one turn

`Observer` is a single non-reentrant activation per `ObserverKey`
(`Source/Kernel/Core/Observation/Observer.cs:44-59`; `ObserverKey` is
`(ObserverId, EventStore, Namespace, EventSequenceId)` — `Source/Kernel/Concepts/Observation/ObserverKey.cs:16-19`).
It is `[KeepAlive]`, `[ObserverPlacement]`, holds two persistent states (definition and failed
partitions), a watchdog grain timer (`Observer.Watchdog.cs:20-29`), an event-type schema cache, and
the subscription. `Handle` carries no `[AlwaysInterleave]`
(`Source/Kernel/Core/Observation/IObserver.cs:288`, contrast the twelve methods that do), so
every partition's handling is serialized in the grain's turn — including the `await` on a remote
`subscriber.OnNext` that may be a gRPC round trip to a client (`Observer.Handling.cs:148`).

The cursor is a single scalar on the observer: `ObserverState.NextEventSequenceNumber`
(`Source/Kernel/Storage/Observation/ObserverState.cs:61-65`). It is the de-duplication gate for live
delivery — events below it are dropped before the subscriber is invoked
(`Observer.Handling.cs:112-114`) — and it is advanced to the batch tail after a successful handling
(`Observer.Handling.cs:187-191`).

### 1.2 Delivery is partition-serial by design

`AppendedEventsQueues` fans an appended batch to `Events.Queues` queue grains (default 2 —
`Source/Kernel/Core/Configuration/Events.cs:20`), filtered by a per-queue event-type union
(`AppendedEventsQueues.cs:85-103`, `AppendedEventsQueueRouter.cs:120-138`). Each queue's consumer
coalesces up to 100 batches and then dispatches:

```
Source/Kernel/Core/EventSequences/AppendedEventsQueue.cs:432-434
// Sort events by sequence number and deliver consecutive same-partition batches. Parallelism is across
// observers within a partition, never across partitions: handling a higher-numbered partition first would
// advance an observer's NextEventSequenceNumber past lower-numbered events from another partition and drop them.
```

The loop at `:436-473` therefore `await Task.WhenAll(tasks)` for partition *A*'s observers before
touching partition *B*. **This is the throughput ceiling, and its stated cause is the single global
cursor.** No amount of observer-grain sharding removes it unless the cursor is split first.

### 1.3 Subscriber grains are already per-partition

`ObserverSubscriberKey` is
`(ObserverId, EventStore, Namespace, EventSequenceId, EventSourceId, SiloAddress)`
(`Source/Kernel/Concepts/Observation/ObserverSubscriberKey.cs:22-28`), so
`ProjectionObserverSubscriber` — which has **no placement attribute**
(`Source/Kernel/Core/Projections/ProjectionObserverSubscriber.cs:36-42`) and therefore uses Orleans'
default placement — is already a distinct, cluster-spread grain per `(observer, partition)`.

> **This contradicts the framing in the work-package brief.** Grain-per-`(observer, partition)` is
> not a thing we would be introducing; it already exists one layer down. The remark in
> `ProjectionHandleLock.cs:18-21` ("The fully actor-native alternative — a grain per
> (observer, partition) whose turn-based execution serializes each key with no explicit lock — is
> deliberately deferred to the observer-sharding work package") is **wrong about why the lock
> exists**. See §8.

### 1.4 Replay is not per-partition for projections

The brief states "Replay is a job with per-partition steps that already distribute." That is true for
reducers, reactors and webhooks, but **false for projections**:

```
Source/Kernel/Core/Observation/Jobs/ReplayObserver.cs:34-47
if (request.ObserverType == ObserverType.Projection)
{
    return [ CreateStep<IHandleEventsForObserver>( ... EventSequenceNumber.First, EventSequenceNumber.Max ... ) ];
}
```

One step, whole sequence, global order — precisely because projections may join across event sources
(`HandleEventsForObserver.cs:171-177`). Catch-up *is* per-partition for every observer type
(`CatchUpObserver.cs:88-121`).

---

## 2. Sharding key

### 2.1 Recommendation

```
ObserverShardKey(ObserverId, EventStoreName, EventStoreNamespaceName, EventSequenceId, ShardIndex)
shardIndex = Fnv1a(partition.ToString()) % shardCount
```

Serialized with the existing `KeyHelper` `'#'` separator (`Source/Infrastructure/KeyHelper.cs:18,27-31`),
appending one more segment so the key remains a plain `IGrainWithStringKey`.

### 2.2 Why not `(observerId, partition)`

| | `(observerId, partition)` | `(observerId, hash(partition) % N)` |
|---|---|---|
| Activations | O(observers × distinct event sources) — unbounded | O(observers × N) — bounded, operator-chosen |
| Grain directory | one entry per partition ever seen | N entries per observer |
| Persistent state | one state document per partition | N documents per observer |
| Watchdog timers | one per partition (`Observer.Watchdog.cs:20-29`) | N per observer |
| Parallelism | maximal | N-way, tunable |
| Ordering | trivially preserved | preserved (pure function of partition) |

The Orleans guidance genuinely cuts both ways here: "optimal throughput is achieved by using multiple
smaller grains rather than a few larger grains" argues for per-partition, while "avoid chatty
communication between grains" and "avoid bottleneck grains" argue against multiplying a grain that is
*not* small. The deciding fact is that the observer grain **is not small**: even after the split
proposed in §4 it carries persistent state, a timer, and a schema cache. Chronicle's own event-source
cardinality is unbounded — event source ids are `EventSourceId` strings minted per domain entity.
Per-partition observer grains would create a directory entry, a storage document and a timer for every
entity the system has ever seen. Bounded sharding is the right trade.

Note that the *subscriber* layer already does per-partition grains (§1.3) and is the right place for
that granularity: subscriber grains are cheap, hold no cursor, and are collected normally.

### 2.3 The hash must be process-stable — the current one is not

`AppendedEventsQueueRouter.GetQueueIndexFor` uses:

```
Source/Kernel/Core/EventSequences/AppendedEventsQueueRouter.cs:44-45
var hash = observerKey.ObserverId.Value.GetHashCode(StringComparison.Ordinal);
return (int)((uint)hash % (uint)queueCount);
```

`string.GetHashCode(StringComparison.Ordinal)` is **randomized per process** in .NET Core. Today the
blast radius is limited because there is one `AppendedEventsQueues` activation per event sequence and
the router lives inside it, but it is already latently wrong: after that grain reactivates in a
different process, `SeedRouter` (`AppendedEventsQueues.cs:68-83`) records subscriptions under the
queue index where they were *found*, while `GetQueueIndexFor` now hashes differently. `IsSubscribed`
(`AppendedEventsQueues.cs:62-66`) then queries the wrong queue — and `Observer.Rescue.cs:38` uses
exactly that answer to decide whether an observer is stranded, so a false negative triggers a
spurious re-route through `Routing`.

A shard index is computed in **at least three processes** — the queue (routing a batch), the shard
grain (validating it owns a partition), and the catch-up/replay job (assigning per-partition steps to
shards). A per-process hash would let two shards both believe they own a partition: **silent
double-apply**. The design therefore mandates a stable hash, and the repository already has one:

```
Source/Kernel/Core/Observation/RoundRobinObserverSubscriberSelector.cs:34-45
// FNV-1a: deterministic across processes, unlike string.GetHashCode().
```

**Prerequisite P0-A:** extract that FNV-1a into a shared `Cratis.Chronicle.StableHash` helper, use it
for the shard index, and fix `AppendedEventsQueueRouter.cs:44` while we are there. This is a
standalone bug fix worth shipping whether or not sharding proceeds.

---

## 3. Shard count and configuration

### 3.1 Where it lives

Shard count is **persisted per observer** on `ObserverDefinition`
(`Source/Kernel/Storage/Observation/ObserverDefinition.cs`), seeded from a new
`Observers.Shards` option (default `1`) at subscribe time. It is deliberately **not** read live from
configuration: a config reload that changed the divisor under a running observer would remap
partitions mid-flight.

`Observers.Shards` joins the existing observer options
(`Source/Kernel/Core/Configuration/Observers.cs`), and the effective count must be clamped against
`Observers.MaxConcurrentPartitions` (default 32, `Observers.cs:14`) so sharding cannot outrun the job
throttle (`Source/Kernel/Core/Jobs/JobStepThrottle.cs:48`).

### 3.2 Observers that must be pinned to `shardCount = 1`

- Projections with `IsEventSourceKeyed == false` (§14). The kernel already computes this flag
  (`Projection.cs:211-214`); the shard count is derived, not configured, for projections.
- `[OnceOnly]` reactors. `Source/Clients/DotNET/Reactors/Reactors.cs:314` maps `[OnceOnly]` to
  `IsReplayable = false`; a reactor that may not be replayed also may not tolerate the bounded
  re-scan that a shard-count change implies (§3.3).

### 3.3 Changing the count on a running system

Remapping `hash(p) % N` to `hash(p) % M` moves partitions between shards whose cursors differ.
A partition landing on a shard whose cursor is **ahead** silently **skips** events. This is the single
most dangerous operation in the design and must not be a config edit.

Recommended procedure, as an explicit administrative operation on the control-plane grain:

1. Quiesce: control plane transitions to a non-`Active` running state; all shards stop accepting
   `Handle` (they already have the machinery — `ShouldHandleEvent` gates on
   `State.RunningState != Active`, `Observer.Handling.cs:378-382`).
2. Fold: `cursor = MIN(shard[i].NextEventSequenceNumber)` over the old shard set.
3. Rewrite: delete the old per-shard state documents, create `M` new ones all at `cursor`.
4. Resume: control plane routes back through `Routing`, which re-derives catch-up as it does today
   (`States/Routing.cs:89-138`).

Step 2/3 re-delivers, to shards that were ahead, events they had already handled — bounded by the
cursor spread. That is acceptable **exactly to the extent that observers are idempotent**, which is
the shipped contract for reactors (`Documentation/concepts/observer-patterns.md:29`,
`Documentation/concepts/glossary.md:66`) and structurally true for projections/reducers. It is *not*
acceptable for `[OnceOnly]` reactors — hence §3.2.

**Open question O-1.** Whether an operator would prefer "shard-count change requires a full replay"
(simpler, no re-scan reasoning, but forbidden for non-replayable observers and expensive for large
sequences) over quiesce-and-fold. The code does not decide this; it is a product call.

---

## 4. State ownership and storage

### 4.1 The split

**Control plane — `IObserver` (existing grain, one activation, never sharded).** Owns:
`ObserverDefinition`; the `ObserverSubscription` and its whole lifecycle including the atomic
`UnsubscribeIfMatchesClient` check documented at `Observer.cs:359-396`; the state machine and
`RunningState`; quarantine; the `FailedPartitions` registry and its retry reminders; job
orchestration (`CatchUp`, `Replay`, `TryRecoverAllFailedPartitions`); the watchdog; and the
**aggregated** observer view that external readers consume.

**Data plane — `IObserverShard` (new grain, N activations).** Owns: `NextEventSequenceNumber`,
`TailEventSequenceNumber`, `LastHandledEventSequenceNumber`, `InFlightPartitions`, and the debounced
progress-write counter — all *for its partitions only*. Implements `Handle`. Non-reentrant, so
Orleans serializes it with no lock.

This mapping follows the Orleans "staged aggregation" guidance directly: the fan-in point
(the aggregated cursor) is computed by folding N shard values, not by routing every event through one
coordinator.

### 4.2 Storage shape

Per-shard state is **one document per `(observerId, shardIndex)`**, written exactly like today's
observer state: a targeted `$set` upsert on a single document
(`Source/Kernel/Storage.MongoDB/Observation/ObserverStateStorage.cs:67-88`). This is the load-bearing
storage constraint (§13): **the design requires single-document atomicity and nothing more.**

Already shard-safe, no change needed:

- `IObserverHandledCountsStorage` is keyed `(observerId, partition)` with atomic increments
  (`Source/Kernel/Storage/Observation/IObserverHandledCountsStorage.cs:33,41,49,56`). W2.1 landing
  this is a genuine enabler — the per-event-type counters that would otherwise have been a
  cross-shard write-conflict are already out of the observer document.
- `IObserverKeyIndex` (`Source/Kernel/Storage/Keys/IObserverKeyIndex.cs`) yields partitions and needs
  no shard awareness; the job layer buckets its output.

### 4.3 The aggregated view and `WaitForCompletion`

```
Source/Kernel/Core/Services/Observation/Observers.cs:71-74
if (observers.All(_ =>
    (((EventSequenceNumber)_.LastHandledEventSequenceNumber).IsActualValue &&
     _.LastHandledEventSequenceNumber >= request.TailEventSequenceNumber) || ...
```

`WaitForCompletion` polls **persisted** observer state via `IObserverStateStorage.GetAll()`
(`Observers.cs:152-153`, `Source/Kernel/Storage/Observation/IObserverStateStorage.cs:31`). Under
sharding it must consult the fold, and the fold must be conservative:

- `LastHandledEventSequenceNumber(observer) := MIN over shards of shard.NextEventSequenceNumber − 1`.
  **Not** `MAX(shard.LastHandled)`: a shard that has raced ahead says nothing about a lagging shard,
  and `WaitForCompletion` is the primitive the client `AppendResult.WaitForCompletion()` extension is
  built on (`Source/Clients/DotNET/Observation/AppendResultWaitForCompletionExtensions.cs:21-38`). A
  `MAX` fold would return "done" while events are still unhandled — a correctness regression visible
  to every integration spec and every user of that API.
- `HandledEventCount` := SUM over shards.
- `RunningState`, `FailedPartitionCount`, `IsReplaying` := from the control plane, unchanged.

Two implementation options, both acceptable:

- **(a) Control-plane writeback.** Shards report progress to the control plane, which writes the
  aggregated legacy document. Keeps every existing reader (`GetObservers`, `ObserveAll`, the
  Workbench, `WaitForCompletion`) working unchanged, at the cost of chatter — mitigated by reporting
  on the same debounce cadence the shard already uses for its own writes
  (`Observer.Handling.cs:437-444`).
- **(b) Read-side fold.** `IObserverStateStorage.Get/GetAll` folds the shard documents at read time.
  No chatter; but `ObserveAll()` (`IObserverStateStorage.cs:18`) is a change-stream-backed observable
  and folding it correctly across N documents per observer is materially harder.

**Recommend (a)** for Phase 2, because it preserves the observable and every existing reader, and the
report is off the critical path.

### 4.4 What the shard needs that it does not own

`ShouldHandleEvent` (`Observer.Handling.cs:364-403`) consults five things the control plane owns:
subscription, failed partitions, running state, `_isPreparingCatchup`, and the replaying/catching-up
partition sets. Making each of those a cross-grain call per batch would be textbook chatty
communication. Instead: the control plane **pushes** a compact per-shard snapshot (subscription,
running state, the shard's own failed/replaying/catching-up partitions) on every change; the shard
caches it. Staleness is one-directional-safe:

- stale "not failed" → an extra delivery → idempotent, and the retry job holds the partition anyway;
- stale "failed" → a missed live delivery → recovered by catch-up from the persisted cursor, which is
  the same mechanism the spill path already relies on
  (`AppendedEventsQueue.cs:122-128,476-504`).

---

## 5. Failed-partition ownership

**The registry stays on the control plane.** Three reasons in the code:

1. Quarantine thresholds are observer-global — both the count and the percentage variants compute
   over *all* failed partitions (`Observer.Failing.cs:176-204`). A per-shard registry would need a
   cross-shard fold on every failure.
2. Retry reminders are already registered on the observer grain, keyed by the partition's string form
   (`Observer.Failing.cs:53`, dispatched at `Observer.cs:399-417`). Orleans reminders are per-grain;
   moving them to shards would multiply reminder rows by N and scatter them.
3. The retry job itself is partition-keyed and observer-scoped
   (`RetryFailedPartitionRequest`, `Observer.Failing.cs:169-173`), and its completion callbacks
   (`FailedPartitionRecovered` / `FailedPartitionPartiallyRecovered`) are `[AlwaysInterleave]` on
   `IObserver` (`IObserver.cs:193,202`).

**How a failed partition is retried without duplicating work.** The mutual exclusion that exists
today survives unchanged, because it is expressed as set membership rather than as a lock:

1. Shard *i* fails partition *p* → calls `controlPlane.PartitionFailed(p, …)`.
2. Control plane records the failure, registers the reminder, and pushes the updated failed set to
   shard *i* only (shard *i* is the sole owner of *p* by construction).
3. Shard *i*'s `ShouldHandleEvent` now drops live deliveries for *p*
   (`Observer.Handling.cs:372-376`) — the retry job is the only writer.
4. The retry job completes → `FailedPartitionRecovered` → control plane clears *p*, pushes the update
   to shard *i*, and starts a catch-up job for *p* if needed
   (`Observer.Failing.cs:70-85` → `StartCatchupJobIfNeeded`, `Observer.Catchup.cs:86-119`).

The one genuinely new hazard is the **push being lost** while the shard is mid-batch. Because a lost
"is failed" push only causes an extra delivery and a lost "is recovered" push only stalls the
partition until the next control-plane push or watchdog tick, neither loses events. Recommend the
control plane re-push the snapshot on its existing watchdog cadence (`Observers.WatchdogInterval`,
default 60 s — `Configuration/Observers.cs:77`) as the self-healing backstop.

---

## 6. Replay and catch-up interaction

### 6.1 Who suppresses live handling

Today, three mechanisms:

- `_isPreparingCatchup`, set in `CatchUp()` and cleared in `RegisterCatchingUpPartitions()`
  (`Observer.Catchup.cs:19,63`), read by `ShouldHandleEvent` (`Observer.Handling.cs:384-388`). It is a
  **whole-observer** gate covering the window between "we decided to catch up" and "we know which
  partitions are involved".
- `State.CatchingUpPartitions` / `State.ReplayingPartitions` — per-partition gates
  (`Observer.Handling.cs:390-400`).
- `RunningState != Active` — the whole-observer gate for `Replaying`, `Disconnected`, `Quarantined`.

### 6.2 Under sharding

- **Whole-observer replay** stays a control-plane operation. The control plane transitions to
  `Replay` (`States/Replay.cs:50-89`), which starts the job; before starting it broadcasts
  "suspend" to all shards. Shards suspend by setting their cached running state — same gate, same
  semantics, N times. Handoff back to live is `Replayed(lastHandled)` → control plane resets **every**
  shard's cursor to `lastHandled.Next()` (matching `Observer.Replay.cs:67-72`) and broadcasts
  "resume". Because all shards resume from one number, no shard can skip.
- **`_isPreparingCatchup`** becomes a control-plane flag broadcast as part of the suspend/resume
  snapshot. It is short-lived and coarse today; keeping it coarse under sharding is correct and
  cheap.
- **Per-partition catch-up and replay** need no cross-shard coordination: the partition sets are
  partitioned by the same hash, so each shard receives only its own. `CatchUpObserver.PrepareSteps`
  (`CatchUpObserver.cs:88-121`) enumerates keys from the index and calls
  `observer.RegisterCatchingUpPartitions(keysForSteps)`; under sharding it buckets `keysForSteps` by
  `Fnv1a(key) % shardCount` and registers with each shard.
- **The projection replay single-step path** (§1.4) is untouched: it is a global-order step that
  cannot be sharded, and it runs while every shard is suspended, so there is no interleaving.

### 6.3 The catch-up cursor question

`CatchUp()` starts a job from `State.NextEventSequenceNumber` (`Observer.Catchup.cs:32`). Under
sharding, the safe starting point for an observer-wide catch-up is `MIN(shard cursors)` — starting
from a higher number would skip the laggard's range. The resulting steps are per-partition and each
step is bounded by its own partition's events, so the laggard's low start costs re-scan only for
partitions in shards that were ahead — idempotent, bounded, and exactly the trade the existing
progress debounce already documents (`Observers.cs:83-91`).

`Observing.OnEnter`'s missed-events check (`States/Observing.cs:72-84`) and the watchdog's
fast-forward (`Observer.Watchdog.cs:126-182`) must likewise operate on the folded `MIN`, or run
per-shard. **Recommend per-shard**: each shard is the natural owner of "am I behind on my
partitions", and running it per shard avoids a fold on every watchdog tick. The consequence is that
`GetNextSequenceNumberGreaterOrEqualTo` (`Observer.Watchdog.cs:158-160`) is called N times instead of
once. On MongoDB that is N indexed lookups per watchdog interval per observer — measurable but small
at N ≤ 8; it should be re-measured, not assumed (**O-2**).

---

## 7. Delivery routing

### 7.1 The queue must address shards

`AppendedEventsQueueObserverSubscription` gains `ShardCount`. `Dispatch` then becomes:

```
group consecutive same-partition runs (unchanged, AppendedEventsQueue.cs:436-446)
for each subscription:
    shard = Fnv1a(partition) % subscription.ShardCount
    append (shard, partition, events) to a per-shard ordered list
dispatch the per-shard lists CONCURRENTLY;
within one shard's list, keep sequence order and dispatch sequentially
```

This is the change that actually delivers the parallelism, and it is only safe once cursors are
per-shard — which is the whole point of §4.

### 7.2 Does the per-queue subscribed-type union still work?

Yes, unchanged. The router indexes by `ObserverKey` and event-type ids
(`AppendedEventsQueueRouter.cs:29-31,77-98`) and answers "which queues must see this batch"
(`:120-138`). Shards subscribe as one logical observer — the subscription record stays
`ObserverKey`-keyed and the shard is resolved *inside* the queue at dispatch time. This deliberately
avoids N subscription records per observer, which would multiply the union maintenance and the
`SeedRouter` reconciliation (`AppendedEventsQueues.cs:68-83`) by N.

### 7.3 The spill path

`SpillToCatchup` clears subscriptions and triggers `observer.CatchUp()` per observer key
(`AppendedEventsQueue.cs:485-504`). Under sharding it still triggers the **control plane**, which
folds to `MIN` and starts the job. No change to the spill's coarseness — it is already documented as
coarse-but-safe (`:479-483`), and that reasoning holds per shard.

### 7.4 Stable hash

Mandatory. See §2.3.

---

## 8. Can the W4.5 striped lock be deleted?

**No.** Not by this design, and not by any partition-keyed grain arrangement. This section is the one
the brief flags as subtle, so it is argued in full.

### 8.1 What the lock actually protects

```
Source/Kernel/Core/Projections/Engine/Pipelines/ProjectionPipeline.cs:94-96
using var handleScope = projection.IsEventSourceKeyed
    ? await _handleLock.AcquireFor(@event.Context.EventSourceId)
    : await _handleLock.AcquireCoarse();
```

The critical section is `ResolveKey → SetInitialState → HandleEvent → … → SaveChanges`
(`ProjectionPipelineManager.cs:72-82`) — a read-modify-write **against the sink document**, not
against grain memory. Orleans turn-based execution serializes *messages to one activation*. It does
not serialize *writes to a shared database row*. Grain isolation can substitute for the lock only if
every writer of a given document is the same activation.

### 8.2 The collapsing case, head-on

`IsEventSourceKeyed` is false when the projection has any child projection, or any event type whose
key resolver does not resolve to the event source id:

```
Source/Kernel/Core/Projections/Engine/Projection.cs:211-214
IsEventSourceKeyed =
    eventTypes.Length > 0 &&
    !ChildProjections.Any() &&
    eventTypes.All(_ => _.ResolvesToEventSourceId);
```

For such a projection, **partition ≠ document key**: two different event sources can resolve onto the
same read-model document. Per-partition grains do not serialize them. Per-*shard* grains are strictly
worse — they serialize fewer things. The only actor-native fix is to route by **resolved key**, which
is impossible at dispatch time: the key is produced by the `ResolveKey` pipeline step
(`ProjectionPipelineManager.cs:74`) *after* dispatch, and resolution can itself read the event
sequence (`ReplayScopedEventSequenceStorage`, `:65`). Routing by resolved key would mean performing a
storage read in the queue's dispatch loop to decide where to send an event — a layering inversion on
the hottest path in the kernel.

So: **the coarse mode of `ProjectionHandleLock` must stay** as long as collapsing projections exist.
Consistent with that, §14 marks them unshardable outright — they also depend on global cross-partition
order (`HandleEventsForObserver.cs:171-177`), which sharding destroys independently of the lock.

### 8.3 The striped mode — and a suspected pre-existing gap

Could the *striped* mode go away for `IsEventSourceKeyed == true` projections, where partition ==
document key? Only if all handling for one partition flows through one activation. Today it does not:

- Live delivery goes `Observer.Handle` → `ProjectionObserverSubscriber.OnNext` → `pipeline.Handle`
  (`Observer.Handling.cs:147-148`; `ProjectionObserverSubscriber.cs:129`).
- Catch-up and retry go `HandleEventsForPartition` → the **same** subscriber grain
  (`HandleEventsForPartition.cs:115,350`), from a job-step grain that is `IGrainWithGuidCompoundKey`
  (`Source/Kernel/Core/Jobs/IJobStep.cs:14`) and default-placed, i.e. spread across silos.
- The subscriber grain key includes `SiloAddress`
  (`ObserverSubscriberKey.cs:22-28`), so a change in the recorded subscription silo yields a *second*
  activation for the same partition.

`ProjectionPipelineManager` is `[Singleton]` (`:27`) — **one instance per silo process** — and the
lock dictionary lives on it (`:50`). Therefore the lock only serializes writers **within one
process**.

> **Suspected pre-existing correctness gap (unverified).** For a collapsing projection, a catch-up
> runs per-partition steps (`CatchUpObserver.cs:88-121`) in parallel, throttled per silo
> (`JobStepThrottle.cs:48`), on default-placed step grains that spread across silos. Two steps on two
> silos hold two different `ProjectionHandleLock` instances and can interleave the read-modify-write
> on the same parent/join document. The coarse lock would not prevent it. I could not find anything
> in the code that would.
>
> **This is a suspicion, not a finding.** What would settle it: a CLUSTER-CI spec that (1) builds a
> projection with a `[Join]` or child collection, (2) appends across many event sources, (3) forces a
> catch-up (not a replay — replay takes the single-step global-order path, §1.4), and (4) asserts every
> parent/child link is present. If it is green, something serializes that I have not found and the
> reasoning above needs correcting. If it is red, it is a bug that exists today, independent of
> sharding, and it must be fixed *before* sharding — because sharding multiplies the concurrency.

### 8.4 Verdict

| | Today | With observer sharding |
|---|---|---|
| Coarse mode (collapsing projections) | required; process-local, possibly insufficient (§8.3) | **still required**, unchanged; those observers are not sharded |
| Striped mode (event-source-keyed) | required (job steps + live race across processes) | **still required** unless job-step handling is also routed through the shard grain |

A future work package *could* delete the striped mode by making the shard grain the single writer for
its partitions — i.e. catch-up/retry steps call `shard.Handle(...)` instead of the subscriber
directly, and the shard performs the pipeline call inside its turn. That is a materially larger change
(the job-step throttle, checkpointing and cancellation semantics all live on the step today) and it is
**out of scope for W5.5**. The brief's premise that sharding "deletes that interim entirely" does not
hold.

---

## 9. Client-side delivery semantics — the binding constraint

Chronicle delivers to **client-side** reactors and reducers over gRPC, not only to in-kernel
observers. This section establishes what is guaranteed today and whether sharding degrades it.

### 9.1 What is guaranteed today: at-least-once, kernel-side cursor only

**There is no client-side cursor and no client-side de-duplication.** The client receives
`EventsToObserve` over the duplex stream, invokes handlers, and returns a `ReactorResult` carrying
`LastSuccessfulObservation` (`Source/Clients/DotNET/Reactors/Reactors.cs:534-545`; the reducer path is
the same shape, `Source/Clients/DotNET/Reducers/Reducers.cs:558`). `ReactorHandler.GetState()` reads
the **kernel's** state over gRPC (`Source/Clients/DotNET/Reactors/ReactorHandler.cs:112-129`) — the
client stores nothing. The kernel is the sole cursor authority.

De-duplication happens once, kernel-side, before dispatch:

```
Source/Kernel/Core/Observation/Observer.Handling.cs:112-114
var tailEventSequenceNumber = State.NextEventSequenceNumber;
var eventsToHandle = events.Where(_ => _.Context.SequenceNumber >= tailEventSequenceNumber).ToArray();
```

The kernel-side chain — `ReactorObserverSubscriber.OnNext` → `IReactorMediator.OnNext` → the gRPC
`Observable.Create` stream (`Source/Kernel/Core/Observation/Reactors/Clients/ReactorObserverSubscriber.cs:38-66`;
`ReactorMediator.cs:46-63`; `Source/Kernel/Core/Services/Observation/Reactors/Reactors.cs:124-137`) —
adds no cursor of its own. If the mediator has no live observer for the connection it returns
`Disconnected` (`ReactorMediator.cs:60`), which the observer treats as a subscription teardown
(`Observer.Handling.cs:178-182`).

**Conclusion: the client boundary is at-least-once.** It is not at-most-once (a redelivery after a
crash is a duplicate, not a drop) and it is not exactly-once.

### 9.2 The redelivery window, and a correction to the framing

The brief states that this release's debounced cursor write (W2.1c) widens the client redelivery
window. **The code does not support that.** `WriteProgressStateDebounced`
(`Observer.Handling.cs:437-444`) is called from exactly four sites — `:64`, `:79`, `:92`, `:104` —
all on the **progress-only** paths: the batch contained no subscribed event type, or was excluded by
an event-source-type / stream-type / tag filter. Those events were never delivered to a client. On
the path where a subscriber actually ran, the cursor write is immediate:

```
Source/Kernel/Core/Observation/Observer.Handling.cs:235-238
if (stateChanged)
{
    await WriteStateAsync();
}
```

So on the **live** path the window remains one batch, as before W2.1c.

The window that *is* wide, and that does affect clients, is on the **catch-up / replay** path:

```
Source/Kernel/Core/Observation/Jobs/HandleEventsForPartition.cs:68-78
State.LastSuccessfullyHandledEventSequenceNumber = lastHandledEventSequenceNumber;
var writeStateResult = await WriteCheckpointDebounced();
```

debounced by `Jobs.StepCheckpointBatchInterval`, default **100**
(`Source/Kernel/Core/Configuration/Jobs.cs:44`). A crash mid-step can redeliver up to 100 events that
a client already handled. The configuration's own remark states the assumption plainly: "because
observers are idempotent, nothing is lost or double-applied" (`Jobs.cs:39-42`).

### 9.3 Is client idempotency a documented contract?

**Yes, for reactors — explicitly.**

- `Documentation/concepts/observer-patterns.md:29` — "Must be idempotent | Handled for you
  (rebuildable) | Handled for you (rebuildable) | **Yes — you own this**".
- `Documentation/concepts/glossary.md:66` — "They must be idempotent because they may run more than
  once."
- `Documentation/troubleshooting/index.md:23` — "reactors can run more than once for the same event
  during replay or recovery. The fix is not to prevent it but to make the reactor **idempotent**".

For projections and reducers the docs promise idempotency is handled by the framework (rebuildable
state) — which holds because their handlers are pure state transitions over a keyed document.

So the shipped contract is: **at-least-once delivery + caller-owned reactor idempotency.** Sharding
does not need to be exactly-once; it needs to not make skips possible and not widen duplicates
materially.

### 9.4 Under sharding

**Skips.** A partition maps to exactly one shard by a pure function of the partition, so no event can
fall between two cursors. The only skip risk is the shard-count remap, which §3.3 forbids outside the
quiesce-and-fold procedure.

**Duplicates.** Unchanged on the live path (each shard writes its own cursor immediately at the same
point `Observer.Handling.cs:235-238` does today) and unchanged on the job path (the step checkpoint is
per-partition already). Sharding does not widen the window.

**The genuinely new hazard is the subscription, not the cursor.** `RoundRobinObserverSubscriberSelector.Select`
picks the client instance from `subscription.Targets` by partition hash
(`RoundRobinObserverSubscriberSelector.cs:26-32`), and `UnsubscribeIfMatchesClient` is documented as
an atomic single-threaded check-and-replace precisely to avoid a TOCTOU on client reconnect
(`Observer.cs:359-396`, `IObserver.cs:126-138`). If each shard held its own copy of
`ObserverSubscription`, a reconnect could leave shards disagreeing about the target set — one shard
delivering to a dead connection while another delivers to the new one.

**This design keeps subscription ownership on the control plane** (§4.1) for exactly this reason. The
shard receives a pushed, read-only snapshot; `Subscribe` / `Unsubscribe` /
`UnsubscribeIfMatchesClient` remain single-activation operations. A shard delivering against a
momentarily stale snapshot gets `Disconnected` from the mediator (`ReactorMediator.cs:60`), which is
already handled (`Observer.Handling.cs:153-160,178-182`) — but that handling currently *mutates the
subscription* (`RemoveSubscriberTarget`, `Observer.cs:499-512`), so under sharding it must instead
**report to the control plane** and let it decide. That is a real, non-trivial refactor and is called
out as such in the phase plan.

### 9.5 Scoping recommendation

Client-owned observers *can* be sharded in principle — the contract is already at-least-once with
documented idempotency. But the subscription/connection lifecycle refactor in §9.4, combined with the
fact that **no test exercises client-side duplicate or skip behavior across a kernel restart**
(§11.3), means the risk is unquantified.

**Recommend: Phase 2 ships kernel-owned observers only. Client-owned reactors and reducers stay
`shardCount = 1` until the client-boundary spec exists (Phase 0-D) and passes.**

---

## 10. Storage guarantees the design depends on

### 10.1 What this design requires

**Single-document atomic upsert, and nothing else.** Per-shard state is one document per
`(observerId, shardIndex)`, written with a targeted `$set` upsert exactly as the observer document is
today (`Storage.MongoDB/Observation/ObserverStateStorage.cs:67-88`). The aggregated view is a
**read-side fold** (`MIN`, `SUM`) or a control-plane writeback (§4.3) — never a cross-document
transaction. All three providers give single-document atomicity.

### 10.2 What the kernel already requires, that sharding must not extend

`AppendMany` opens a **MongoDB multi-document transaction**:

```
Source/Kernel/Storage.MongoDB/EventSequences/EventSequenceStorage.cs:211-216
var client = database.Client;
using var session = await client.StartSessionAsync().ConfigureAwait(false);
session.StartTransaction();
```

(also `Storage.MongoDB/Observation/Reactors/ReactorDefinitionsStorage.cs:56-60`). This already
requires a replica set; on a **sharded** MongoDB it becomes a distributed transaction with its own
latency and configuration requirements. Sharding observers does not touch this path (§12) and must
not add a second transaction scope.

### 10.3 Unique constraints under a sharded store

```
Source/Kernel/Storage.MongoDB/Events/Constraints/UniqueConstraintsStorage.cs:29-57
// IsAllowed: FindAsync(_ => _.Value == value) …then Save: ReplaceOneAsync(upsert)
Source/Kernel/Storage.MongoDB/Events/Constraints/UniqueConstraintsStorage.cs:66-70
new CreateIndexOptions { Name = ValueIndexName, Unique = unique, Background = true }
```

This is a read-then-write whose atomicity comes **entirely from the single non-reentrant
`EventSequence` activation**, backed by a unique index on `Value` as the second line of defence. Two
provider-level caveats worth recording, both **pre-existing and untouched by this design**:

1. On a **sharded** MongoDB collection, a unique index is only enforced if it is prefixed by the shard
   key. A sharded constraint collection would silently degrade to per-chunk uniqueness. Nothing in the
   code declares a shard key, so this is a deployment hazard, not a code bug.
2. The code already degrades on purpose when the index cannot be built:
   `UniqueConstraintsStorage.cs:94-101` falls back to a **non-unique** index and logs. After that, the
   grain serialization is the only enforcement.

### 10.4 Provider parity

- **InMemory** must implement the per-shard state store with the same *sentinel* semantics as the
  persistent ones. `.ai/rules/framework.md` records the specific trap and this repository has been
  bitten by it (`chronicle-testing-harness-sentinel-filters`). This is an explicit acceptance criterion,
  not a footnote.
- **SQL** has no Orleans membership provider (per `REPORT-kernel-performance-analysis.md` §P13), so SQL
  deployments are single-node. Sharding is a no-op there but must not regress; `shardCount = 1` must
  be the exact code path that exists today.

---

## 11. Testing strategy

### 11.1 The oracle for "no events lost, no double-apply, ordering intact"

A single deterministic property harness, run in-process and again in CLUSTER-CI:

- **Setup:** one event sequence, K distinct event sources, M events per source, one observer under
  test, `shardCount ∈ {1, 2, 4}`.
- **Observer under test:** a kernel reactor that appends, per received event, a record of
  `(partition, sequenceNumber, receiptOrdinal)` into an in-memory sink.
- **Oracle:**
  - *No loss*: the multiset of `sequenceNumber` seen == the appended set.
  - *No double-apply*: for a run with no induced fault, every `sequenceNumber` appears exactly once.
    With an induced fault, duplicates are permitted but **only for sequence numbers at or above the
    last persisted cursor of the owning shard** — an assertion the harness can make because it can
    read the shard state.
  - *Per-partition order*: for each partition, `receiptOrdinal` is monotonic in `sequenceNumber`.
  - *Cross-partition order*: asserted **only** for `shardCount == 1` and for unsharded observers —
    sharding abandons it by design (§14), and a test asserting it would encode the wrong contract.

### 11.2 Existing suites that apply

- `Source/Kernel/Core.Specs/Observation/for_Observer/**` — the unit-level state-machine and handling
  specs; every one of them must keep passing against `shardCount == 1`. This is the Phase 1 gate.
- `Source/Kernel/Core.Specs/Observation/Jobs/for_HandleEventsForPartition/**` and
  `for_HandleEventsForObserver/**` — the job path; unchanged by this design, so an unchanged green run
  is evidence the split did not leak into the job layer.
- `Integration/Kernel/**`, `Integration/Client/**` — end-to-end reactor/reducer/read-model behavior.
- `Integration/Clustering/for_Clustering/when_appending_an_event_with_reactor_reducer_and_projection.cs`
  — the 2-silo cross-boundary path.
- `Source/Kernel/Core.Specs/Services/Observation/for_Observers/when_waiting_for_completion/**` — five
  existing specs pin `WaitForCompletion` semantics; the `MIN` fold (§4.3) must keep all five green,
  and they are the reason `MAX` is not an option.

### 11.3 New suites that do not exist and are required

| ID | Test | Why it is required | Phase |
|---|---|---|---|
| **T1** | Collapsing-projection cross-silo catch-up (`[Join]` / child collection, many event sources, forced catch-up, assert link completeness) | Settles the suspected pre-existing race in §8.3. If red, it blocks sharding. | 0 |
| **T2** | Client-boundary duplicate/skip across a kernel restart: client reactor records every receipt; kill and restart the kernel mid-batch; assert no gap and bounded duplicates | **Nothing tests this today.** It is the oracle for §9. | 0 |
| **T3** | Mixed-build clustering rig: two silos from different assembly versions | `Integration/Clustering/Clustering.csproj:18` references one `Core.csproj` for every silo, so wire/behavioral skew is **unverifiable today**. | 0 |
| **T4** | Shard-index stability across processes (property test over the FNV-1a helper) | Prevents the §2.3 double-apply class outright. | 0 |
| **T5** | The §11.1 property harness at `shardCount ∈ {1,2,4}`, with and without induced faults | The primary correctness oracle. | 1–2 |
| **T6** | Shard-count change procedure: quiesce → fold → resume, assert no skip | §3.3 is the most dangerous operation in the design. | 2 |
| **T7** | Failed-partition ownership: fail a partition on shard *i*, assert no other shard delivers it and the retry recovers it exactly once | §5. | 2 |

**T3 is a hard prerequisite** if the design ever needs a rolling upgrade (§12.2). Note also that
CLUSTER-CI is a *correctness* suite, not a benchmark — it cannot demonstrate that sharding is faster,
only that it is not wrong. The performance claim needs the measurement discipline already defined in
`PLAN-kernel-performance-execution.md`.

---

## 12. Interaction with the append path

**The append path is not touched, and must not be.** Everything below lives on the single
non-reentrant `EventSequence` activation (`Source/Kernel/Core/EventSequences/EventSequence.cs:59-60`,
`[EventSequencePlacement]`) and depends on that serialization for correctness:

- **Sequence-number assignment** — `State.SequenceNumber` read, incremented, and committed only after
  a durable append (`EventSequence.cs:508-531,545-548` for `AppendMany`; `:628,648-649` for `Append`).
- **Duplicate-key recovery** — the `do { … } while (!appendResult.IsSuccess)` retry loops
  (`:534-540`, `:618-646`) resolving `DuplicateEventSequenceNumber`.
- **Concurrency validation** — `:262-266`, `:330-334`.
- **Constraint claim and commit** — `GetValidAndCompliantEvent` claims (`:255`, `:314`, with a
  `ConstraintBatchClaims` set explicitly because "the persisted index is only updated after the whole
  batch has been appended", `:307-310`), and `constraintContext.Update(...)` commits (`:576`, `:656`).

If sharding touched this path, the immediate breakages would be: two activations assigning the same
sequence number; the duplicate-recovery loop racing itself; and the unique-constraint check-then-claim
losing its mutual exclusion (§10.3 shows the index alone is not a sufficient backstop).

### 12.1 One pre-existing append bug that sharding makes more likely

```
Source/Kernel/Core/EventSequences/EventSequence.cs:655-656
await (_appendedEventsQueues?.Enqueue(appendedEvents) ?? Task.CompletedTask);
await constraintContext.Update(appendedSequenceNumber);
```

(and the batch equivalent at `:572` before `:574-577`). `Enqueue` is a **grain call** and is awaited
**before** the constraint index is committed. If it throws, the exception unwinds to
`HandleAppendEventException` (`:660-663`) / the `AppendMany` catch (`:345-353`) and the unique-constraint
claim is **never persisted** — while the event is already durably appended. A later append with the
same unique value then sees no claim and is allowed.

This is a live bug today. It matters to this design because sharding increases the number of remote
grains an `Enqueue` fans out to, raising the probability of the fault that triggers it.

**Prerequisite P0-B:** commit the constraint index before enqueueing (or make the enqueue
non-throwing — it is already fire-and-forget in spirit; `AppendedEventsQueue.Enqueue` never blocks and
spills on overflow, `AppendedEventsQueue.cs:122-154`). Ship it independently of sharding.

---

## 13. Risks and failure modes

| # | Risk | Blast radius | Mitigation |
|---|---|---|---|
| R1 | Unstable shard hash → two shards own one partition | **Silent double-apply.** Worst case in the design. | P0-A stable FNV-1a + T4 |
| R2 | Rolling upgrade: v(N) `Observer` and v(N+1) `ObserverShard` both live | Silent double-apply, undetectable | Stop-the-world first release; T3 before any rolling story |
| R3 | Shard-count change under load | **Silent skip** | §3.3 procedure; T6; pin `[OnceOnly]` to 1 |
| R4 | `MAX` instead of `MIN` in the completion fold | `WaitForCompletion` returns early; every dependent spec flakes | §4.3; the five existing `when_waiting_for_completion` specs |
| R5 | Someone shards a collapsing projection | Corrupt read model, silently | Derive shard count from `IsEventSourceKeyed`; make it non-overridable |
| R6 | Divergent subscription state across shards | Delivery to a dead client connection; lost `Unsubscribe` | Subscription stays on the control plane (§9.4) |
| R7 | The §8.3 cross-silo catch-up race is real | Pre-existing corruption, amplified by sharding | T1 in Phase 0; if red, fix before Phase 2 |
| R8 | Control-plane push loss | Extra delivery (safe) or stalled partition | Watchdog re-push (§5) |
| R9 | N× watchdog storage lookups | Load, not correctness | Measure (O-2); jitter as W6.4 already proposes |
| R10 | Directory/activation growth | Memory, activation storms | Bounded by `shardCount`; that is the whole reason for §2.2 |

### 13.1 Phased rollout

**Phase 0 — prerequisites (each independently valuable; ship even if sharding is abandoned)**
- P0-A stable FNV-1a helper; fix `AppendedEventsQueueRouter.cs:44`. Gate: T4.
- P0-B constraint-commit-before-enqueue ordering (§12.1).
- P0-C mixed-build clustering rig (T3).
- P0-D client-boundary duplicate/skip spec (T2).
- P0-E collapsing-projection cross-silo catch-up spec (T1). **If red, stop and fix.**

**Phase 1 — identity sharding.** Introduce `ObserverShard` and route everything through it with
`shardCount == 1`. Behavior must be bit-identical. Gate: the entire existing spec suite green,
unmodified, plus T5 at `shardCount == 1`. This validates the split with zero semantic change and is
independently revertible.

**Phase 2 — `shardCount > 1`, kernel-owned event-source-keyed observers only.** Adds the per-shard
cursor, the `MIN` fold, per-shard failed-partition pushes, and parallel dispatch (§7.1). Gate: T5 at
2 and 4, T6, T7, CLUSTER-CI green. **Kill switch: `Observers.Shards = 1`** returns to the Phase 1
path at runtime for new subscriptions and via the §3.3 procedure for running ones.

**Phase 3 — client-owned observers.** Requires the §9.4 subscription refactor and a green T2 at
`shardCount > 1`. Not scoped here.

**Never:** collapsing projections; the append path; `[OnceOnly]` reactors.

---

## 14. What can scale, what cannot, what is unproven

| Component | Verdict | Binding invariant / evidence |
|---|---|---|
| `EventSequence` (append) | **Cannot** | Sequence-number assignment, duplicate recovery, concurrency and unique-constraint claim all rely on one non-reentrant activation — `EventSequence.cs:59-60,508-548,618-646,262-266,255+656` |
| Observer **control plane** (subscription, quarantine, failed-partition registry, job orchestration) | **Cannot** | Atomic `UnsubscribeIfMatchesClient` (`Observer.cs:359-396`); observer-global quarantine thresholds (`Observer.Failing.cs:176-204`); per-grain reminders (`Observer.Failing.cs:53`) |
| Projections with `IsEventSourceKeyed == false` (joins, child collections, non-event-source keys) | **Cannot** | Requires global cross-partition order (`HandleEventsForObserver.cs:171-177`); partition ≠ document key (`Projection.cs:211-214`) |
| Projections with `IsEventSourceKeyed == true` | **Can** | Partition == document key; per-partition order is the only ordering requirement |
| Reducers (kernel and client) | **Can** | Read model keyed by event source; per-partition order sufficient |
| Kernel reactors | **Can** | Idempotent side effects; at-least-once already the contract |
| Client reactors / reducers over gRPC | **Can, but not yet** | Contract permits it (§9.1–9.3) but the subscription-lifecycle refactor (§9.4) is unproven and T2 does not exist |
| `[OnceOnly]` reactors | **Cannot** | Not replayable (`Clients/DotNET/Reactors/Reactors.cs:314`), so the shard-count-change re-scan (§3.3) would double-fire |
| `AppendedEventsQueue` dispatch loop | **Can, only after cursors split** | Partition-serial *because* of the single cursor (`AppendedEventsQueue.cs:432-434`) |
| `ProjectionHandleLock` (coarse mode) | **Cannot be removed** | Protects a sink document, not grain memory (§8.1–8.2) |
| `ProjectionHandleLock` (striped mode) | **Not removed by W5.5** | Job steps bypass the observer grain (`HandleEventsForPartition.cs:115,350`); manager is per-process (`ProjectionPipelineManager.cs:27,50`) |
| Cross-silo coarse-lock coverage today | **Unproven — suspected broken** | §8.3; settled by T1 |
| Rolling upgrade unsharded → sharded | **Unproven — assume unsupported** | No mixed-build test (`Integration/Clustering/Clustering.csproj:18`); settled by T3 |
| Sharded/replica-set MongoDB under the current schema | **Partly unproven** | `AppendMany` needs a transaction (`EventSequenceStorage.cs:211-216`); unique indexes need a shard-key prefix (§10.3). No deployment test exists. |

---

## 15. Alternatives considered and rejected

**A. Keep the W4.5 striped lock and do nothing else.** Rejected as a *substitute* for sharding — the
lock addresses read-modify-write races, not the throughput ceiling, which is the partition-serial
dispatch loop (§1.2). Accepted as a *complement*: §8 concludes the lock stays regardless.

**B. Make `Observer` `[Reentrant]` with per-partition cursors, no new grain.** This is the strongest
alternative and deserves serious consideration. The dominant cost in `Observer.Handle` is the `await`
on a remote `subscriber.OnNext` (`Observer.Handling.cs:148`) — network I/O, not CPU. Reentrancy plus
per-partition cursors would let many partitions be in flight in one activation, capturing most of the
intra-silo win at a fraction of the structural cost: no directory growth, no state split, no shard
remap problem, no migration story.

Rejected as *the* answer because it does not scale across silos — one observer still lives on one
silo, which is the stated goal of W5.5. But **strongly recommended as a measurement gate before
Phase 2**: if a reentrancy prototype recovers most of the throughput, the cost/benefit of true
sharding changes materially. Its own risk is real — the Orleans guidance is explicit that "handling
interleaving increases risk by being error-prone", and `Observer` mutates `State` on nearly every
path.

**C. Orleans Streams.** Rejected. The kernel deliberately moved *away* from Orleans streams:
`ProjectionObserverSubscriber.cs:53-56` records that "the previous Orleans MemoryStreams pub-sub
propagated subscribers to the producer's PullingAgent on a periodic refresh that could lag the first
publish and silently drop it under load". Reintroducing streams would re-open a closed bug.

**D. Partition-affinity placement without sharding.** Placing the single `Observer` activation near
its data does not help: there is one activation, so there is one silo's worth of throughput
regardless of which silo it is. Worth doing for hop reduction (it is W5.1/W5.2 in the plan) but it is
not a scale-out mechanism.

**E. `[StatelessWorker]` observer.** Rejected outright. `StatelessWorker` gives multiple local
activations with **no identity and no shared state** — the cursor would fork silently. It is the
right tool for the functional operations the Orleans guidance names (decryption, decompression), not
for a stateful cursor owner.

**F. Shard the queue instead of the observer** (more `Events.Queues`, each owning a partition range).
Rejected: it moves the fan-out but not the fan-in. Every queue still calls the same single `Observer`
activation, which still serializes on one cursor. It would help only if the cursor were already split
— at which point it is the same design as §7.1, but with the shard boundary in the wrong place
(queues are per event sequence, shards must be per observer).

**G. Route by resolved read-model key rather than by partition.** The theoretically correct answer for
collapsing projections (it would make the actor boundary match the consistency boundary). Rejected as
infeasible: the key is produced by the `ResolveKey` pipeline step after dispatch and may itself read
storage (`ProjectionPipelineManager.cs:65,74`). Resolving it at dispatch time puts a storage read in
the queue's hot loop.

---

## 16. Open questions

- **O-1 (§3.3)** Quiesce-and-fold vs. require-a-replay for shard-count changes. A product call; the
  code does not decide it.
- **O-2 (§6.3)** Cost of N× watchdog `GetNextSequenceNumberGreaterOrEqualTo` lookups per observer per
  interval. Measurable; not yet measured.
- **O-3 (§8.3)** Is the coarse lock already insufficient across silos on the catch-up path? Settled by
  T1. This is the highest-value unknown in the document: a red T1 is a bug that exists today.
- **O-4 (§4.3)** Control-plane writeback vs. read-side fold for the aggregated observer view — in
  particular whether `IObserverStateStorage.ObserveAll()` (a change-stream observable,
  `IObserverStateStorage.cs:18`) can be folded across N documents per observer without losing change
  granularity. Recommendation is writeback; the fold has not been prototyped.
- **O-5 (§10.4)** Does anyone run Chronicle against a *sharded* (as opposed to replica-set) MongoDB?
  If yes, §10.3's unique-index caveat is a live production hazard independent of this work. Not
  answerable from the code.
- **O-6 (§9.4)** The `Disconnected`-result handling currently mutates the subscription inside
  `Observer.Handle` (`Observer.Handling.cs:153-160` → `Observer.cs:499-512`). Moving that to a report
  to the control plane changes the retry-against-remaining-instances behavior in ways the existing
  fan-out specs may or may not pin. Needs a read of
  `Source/Kernel/Core.Specs/Observation/for_Observer/**` before Phase 3 is scoped.

---

## 17. Also noted while reading (out of scope, worth filing)

- **`IInFlightEventsStorage` is dead.** It is defined (`Source/Kernel/Storage/Observation/IInFlightEventsStorage.cs`)
  and implemented by all three providers (`Storage.InMemory/Observation/InFlightEventsStorage.cs`,
  `Storage.Sql/EventStores/Namespaces/InFlightEvents/InFlightEventsStorage.cs`,
  `Storage.MongoDB/Observation/InFlightEventsStorage.cs`), but **nothing in `Source/Kernel/Core` calls
  it** — the in-flight bookkeeping actually used is `ObserverState.InFlightPartitions`
  (`Observer.Handling.cs:120-125`, `ObserverState.cs:88`). `.ai/rules/framework.md` names exactly this
  pattern ("treat 'no spec touches this public API' as a defect in itself"). Either wire it up — it is
  a better fit for per-shard, per-event durability than the coarse partition set — or delete it.
- **`AppendedEventsQueueRouter` hash instability** (§2.3) is a bug today, before any sharding.
- **Constraint-index commit ordering** (§12.1) is a correctness bug today.
