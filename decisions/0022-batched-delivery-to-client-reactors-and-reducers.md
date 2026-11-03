---
id: 0022-batched-delivery-to-client-reactors-and-reducers
title: Deliver replay and catch-up to client reactors and reducers as multi-partition pages with one result per partition, keep per-event handlers unchanged, and add an opt-in collection handler for reactors
status: proposed
stage: none
class: contract
reversibility: costly
proposed: 2026-11-03
applies-to:
  - Source/Kernel/Core/Observation/Jobs/**
  - Source/Kernel/Core/Observation/Reactors/Clients/**
  - Source/Kernel/Core/Observation/Reducers/**
  - Source/Kernel/Core/Configuration/Observers.cs
  - Source/Kernel/Contracts/Observation/Reactions/**
  - Source/Kernel/Contracts/Observation/Reducers/**
  - Source/Kernel/Grpc/Observation/Reactors/**
  - Source/Kernel/Grpc/Observation/Reducers/**
  - Source/Clients/DotNET/Reactors/**
  - Source/Clients/DotNET/Reducers/**
  - Source/Clients/DotNET.CodeAnalysis/**
  - Source/Clients/Testing/**
  - Documentation/reactors/**
  - Documentation/reducers/**
---

<!-- markdownlint-disable-next-line MD025 -->
# Deliver replay and catch-up to client reactors and reducers as multi-partition pages with one result per partition, keep per-event handlers unchanged, and add an opt-in collection handler for reactors

## Context

Replay and catch-up of a client-owned observer are slow, because every round trip carries a few events for a single partition. Bulk import ([decision 0023](0023-bulk-event-import.md)) makes this worse: committing 300 000 events ends in a catch-up over the same volume.

### What already batches

The kernel already batches every observer:

- `IObserverSubscriber.OnNext(Key partition, IEnumerable<AppendedEvent> events, ObserverSubscriberContext context)` takes a collection (`Source/Kernel/Core/Observation/IObserverSubscriber.cs:26`).
- Replay and catch-up read through `BoundedEventCursor`, which has a default of 500 (`Source/Kernel/Core/Observation/Jobs/BoundedEventCursor.cs:27`).

The wire also already carries collections:

- `EventsToObserve { Partition, repeated AppendedEvent Events, ReplayState, DeliveryId, DeliverWithinMilliseconds }`.
- `ReduceOperationMessage { Partition, InitialState, repeated AppendedEvent Events, ... }`.
- Each message is answered by one result that carries `LastSuccessfulObservation`.

Reducers already fold a collection end to end:

- `ReducerDispatcher` makes one `handler.OnNext(appendedEvents, initialState, sp)` call per message (`Source/Clients/DotNET/Reducers/ReducerDispatcher.cs:101`).
- `ReducerInvoker` threads the state through the user's per-event `Reduce` methods (`ReducerInvoker.cs:59-130`).
- The kernel writes the folded state once (`Source/Kernel/Core/Observation/Reducers/ReducerPipeline.cs:63-160`).

Reactors receive a collection, but the client calls the user's handler once per event:

- The loop is `ReactorDispatcher.cs:200-245`. It stops at the first failure and reports the handled prefix as `LastSuccessfulObservation`.
- No handler signature takes a collection.

### Where the throughput goes

1. **Every client call holds one partition.**
   - Client-owned subscribers are capped at `Observers.MaxEventsPerSubscriberCall = 100` (`Source/Kernel/Core/Configuration/Observers.cs:64`).
   - Replay and catch-up of a non-projection observer create one `HandleEventsForPartition` step per partition (`ReplayObserver.cs:139-166`, `CatchUpObserver.cs:169-204`).
   - With 300 000 events over 100 000 event sources, that is about 100 000 round trips of about 3 events each. Each trip pays a gRPC hop, a client DI scope, and a kernel `Sink.FindOrDefault` plus `ApplyChanges` for reducers.
   - Projections avoid this. `HandleEventsForObserver` walks the sequence once and hands pages of up to `ReadModels.MaxBatchSize = 500` events across partitions to `IObserverPageSubscriber.OnNextPage` (`HandleEventsForObserver.cs:299-331`). The sink buffers and flushes in bulk (`BeginBulk`/`FlushBulk`, `Storage.MongoDB/Sinks/Sink.cs`).
2. **A reactor cannot do bulk work.** A reactor that writes to an external store, search index or API cannot turn 500 events into one bulk call, because it only ever sees one event.
3. **Reducer catch-up does no bulk sink work.** `ReducerCatchupHandler` does nothing (`ReducerCatchupHandler.cs:15-21`), unlike `ProjectionCatchupHandler`.

### Accepted and proposed records this must fit

- [Decision 0020](0020-kernel-http-delivery-contract.md) (accepted) already shapes HTTP delivery as **a list of per-partition batches with one result per batch**. It sends one partition per request in v1, so that more can be sent later without a breaking change. gRPC delivery should use the same shape, so that the kernel has one notion of a delivery page.
- [Decision 0018](0018-partition-ownership-protocol.md) (accepted): a delivery is a writer and delivers only partitions it holds a claim for.
- [Decision 0004](0004-ordered-lanes-observer-delivery.md) (proposed): a lane batch spans many keys. A multi-partition page is the transport such a lane batch would need.

## Decision

1. **Kernel: page delivery for client observers during replay and catch-up.**
   - A client-owned reactor or reducer that declares page support (point 4) is replayed and caught up the way projections are: by one `HandleEventsForObserver` walk in sequence order, with pages handed to a new `IObserverPageSubscriber` implementation on the reactor and reducer subscribers.
   - A page is bounded by an event count and a byte size. The proposed defaults are `Observers.MaxEventsPerPage = 500` and 4 MiB, matching decision 0020.
   - The page is grouped into per-partition batches, with each batch in sequence order.
   - Partitions known to be failed are filtered out before dispatch, as projections do today.
   - Live delivery is unchanged. It is per partition and bounded by `LiveDeliveryBudget`.
2. **Wire: one page message and one page result, mirroring decision 0020.**
   - New message `EventPageToObserve { DeliveryId, ReplayState, DeliverWithinMilliseconds, repeated PartitionEvents Batches }`, where `PartitionEvents { Partition, repeated AppendedEvent Events, string InitialState }`. `InitialState` is reducer only, and the kernel loads it for the whole page in one sink read.
   - The answer is `PageResult { DeliveryId, repeated PartitionResult Results }`, where `PartitionResult` has the fields of today's `ReactorResult` and `ReducerResult` (`Partition, State, LastSuccessfulObservation, ExceptionMessages, ExceptionStackTrace, ReadModelState`).
   - These are added to the existing `OneOf` stream messages, so the change is additive.
   - A client that never declared page support never receives one.
3. **Kernel: per-partition settlement.**
   - Each `PartitionResult` settles its own partition. An `Ok` advances that partition. A `Failed` records a failed partition at the first unhandled event and lets every other partition in the page proceed.
   - This is possible because no order is promised across partitions (decision 0020, "Ordering and concurrency").
   - Reducer results keep today's rule: a failed partition reports `Unavailable`, so no partially folded state is written.
   - Successful reducer states from one page are written in a single bulk sink window (`BeginBulk`/`FlushBulk`), and reducer catch-up gains the bulk bracket that projection catch-up has.
4. **.NET client, reducers: no new user API.**
   - Reducers are written exactly as today, as static self-reducers or `IReducerFor<T>`, one event at a time.
   - The client declares page support for every reducer on registration, folds each partition of a page with the existing `ReducerInvoker` (partitions in parallel, bounded), and answers one `PartitionResult` per partition.
   - This is the "batch in the client, send the states back as a collection" model.
5. **.NET client, reactors: per-event handlers unchanged, plus an opt-in collection handler.**
   - Existing per-event methods keep working and receive pages transparently. The client loops them per partition, as it does today, and answers per partition.
   - A reactor may also declare a method that takes a collection:

     ```csharp
     [Reactor]
     public class IndexUsers(ISearchIndex index) : IReactor
     {
         public Task Registered(EventBatch<UserRegistered> batch) =>
             index.BulkUpsert(batch.Select(e => new UserDocument(e.Context.EventSourceId, e.Event.Name)));
     }
     ```

   - `EventBatch<TEvent>` is an `IReadOnlyList<EventAndContext<TEvent>>`. Its members are `Partitions`, `IsReplay`, `Delivery` (the `ReactorDelivery`) and `MarkFailed(EventContext, Exception)`.
   - A batch is the **maximal run of consecutive events of that type** inside the page, across partitions. Within each partition, events keep sequence order.
   - Throwing fails every partition in the batch at its first event in the batch. `MarkFailed` fails only that event's partition, from that event onward, so a partial bulk failure can be reported per partition.
   - Return types follow today's side-effect rules. Returned events are appended with the per-event idempotency key `{observerId}:{sourceSequenceNumber}` from decision 0012.
   - A single reactor class can mix per-event and batch methods. Each event type routes to exactly one handler; declaring both for one type is an analyzer error.
6. **Other SDKs follow the same contract.** TypeScript, Kotlin, Elixir and Python live in their own repositories. They gain page support by answering per partition. The collection handler is optional per SDK, and its absence is recorded in `Documentation/clients/where-the-clients-differ.md`.

## Options considered

- **Keep per-partition delivery and only add a collection handler (not taken).** Cheapest, but each batch would average a handful of events in replay, so a bulk handler would gain almost nothing. Page delivery is what makes the handler worth having.
- **Batch handler receives all event types of a page as `IEnumerable<object>` (not taken as the default).** This preserves global page order but forces a type switch inside every handler and loses the typed method-per-event convention. Runs of consecutive same-type events keep typing and per-partition order. A `ReactorPage` "everything" signature can be added later if demand appears.
- **Group a page by event type regardless of position (not taken).** This yields bigger batches, but it reorders events within a partition (an `Updated` before its `Created`). That breaks the one ordering promise Chronicle makes.
- **Halt the whole page at the first failed partition, like the non-page `HandleEventsForObserver` walk (not taken).** That walk halts to keep global order, which projections need. Reactors and reducers are only ordered per partition, so per-partition settlement keeps healthy partitions moving.
- **Client-streaming or a server-side batching window on the live path (deferred).** Live delivery is serial per observer today (`AppendedEventsQueue.cs:471-506`), and changing it is decision 0004's territory.
- **A new `IBatchReactor` interface (not taken).** It goes against convention-based discovery. A parameter type on an ordinary handler method is discoverable and needs no marker.

## Default if unanswered

Replay and catch-up of reactors and reducers stay per partition at up to 100 events per call. A 300 000-event import or replay costs one client round trip, plus for reducers one sink read and one sink write, per partition. The cost grows with the number of event sources, not events. Reactors that feed external bulk APIs keep issuing one call per event. Nothing breaks, but bulk import (decision 0023) inherits this as its downstream bottleneck.

## Timeline and scope

- **In scope:**
  - Page delivery for client-owned reactors and reducers during replay, catch-up and failed-partition retry.
  - The two wire messages.
  - Per-partition settlement.
  - Bulk sink windows for reducer pages and reducer catch-up.
  - The .NET reducer page support with no user API change.
  - The .NET `EventBatch<T>` reactor handler, its analyzer rules and the `Cratis.Chronicle.Testing` reactor scenario support for it.
  - Documentation.
- **Out of scope:** live-path batching, webhook and HTTP receivers (decision 0020 already covers them), kernel projections (already paged), and SDKs outside this repository.
- **Release:** additive, `minor`. Page delivery is negotiated per observer on registration, so an older client keeps receiving `EventsToObserve` and an older kernel never sends a page.
- **Horizon:** holds until decision 0004 (ordered lanes) is accepted. Its lane batch should then reuse `EventPageToObserve` unchanged.

## Verification

- **Done when:**
  - A replay of a .NET reducer over N events and P partitions makes about N / 500 client calls instead of P, and produces the same read models as per-partition delivery.
  - A reactor with an `EventBatch<T>` method receives batches spanning several partitions during replay.
  - A failure marked for one partition parks only that partition while the others advance.
  - An older client registered against the new kernel still receives per-partition `EventsToObserve`.
- **Verify by:**
  - Integration specs under `Integration/` for:
    - reducer replay parity (paged against per-partition)
    - a batch reactor partial failure with `MarkFailed`
    - per-partition settlement in a page with one throwing partition
    - mixed per-event and batch methods preserving per-partition order
  - A benchmark under `Benchmarks/` replaying 300 000 events over 100 000 event sources, reporting client calls and wall time before and after.

## Consequences

- **Easier:**
  - Replay and catch-up time of reactors and reducers scales with events instead of event sources.
  - Reactors can call external bulk APIs.
  - One page concept serves gRPC and HTTP delivery.
  - Reducer authors change nothing.
- **Harder:**
  - The kernel holds per-partition results for a page and must settle them independently, including against decision 0018 claims for every partition in the page.
  - Reducer pages load and write many states at once, so memory is bounded by the page byte limit.
- **Forecloses:**
  - Promising a reactor a cross-partition total order inside a batch beyond "sequence order within each partition".
  - Grouping by type out of position.
- **Open questions:**
  - Whether batch handlers also apply on the live path when an appended batch spans partitions. Today the live path delivers per partition.
  - Whether `[OnceOnly]` applies per batch method the same way it applies per event method.
  - The page byte budget for reducers whose `InitialState` is large.
