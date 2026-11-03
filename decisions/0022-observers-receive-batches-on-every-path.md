---
id: 0022-observers-receive-batches-on-every-path
title: Deliver to every observer in pages spanning many partitions on every path, live included, settle each partition on its own, and let client reactors and reducers choose exactly one of per-event or batch mode, with a batch context to decide on
status: proposed
stage: none
class: contract
reversibility: costly
proposed: 2026-11-03
applies-to:
  - Source/Kernel/Core/EventSequences/AppendedEventsQueue*.cs
  - Source/Kernel/Core/Observation/**
  - Source/Kernel/Core/Projections/**
  - Source/Kernel/Core/Configuration/Observers.cs
  - Source/Kernel/Contracts/Observation/Reactions/**
  - Source/Kernel/Contracts/Observation/Reducers/**
  - Source/Kernel/Grpc/Observation/Reactors/**
  - Source/Kernel/Grpc/Observation/Reducers/**
  - Source/Clients/DotNET/Observation/**
  - Source/Clients/DotNET/Reactors/**
  - Source/Clients/DotNET/Reducers/**
  - Source/Clients/DotNET.CodeAnalysis/**
  - Source/Clients/Testing/**
  - Documentation/reactors/**
  - Documentation/reducers/**
  - Documentation/concepts/observers.mdx
---

<!-- markdownlint-disable-next-line MD025 -->
# Deliver to every observer in pages spanning many partitions on every path, live included, settle each partition on its own, and let client reactors and reducers choose exactly one of per-event or batch mode, with a batch context to decide on

## Context

Observers (reactors, reducers, projections, webhooks) are handed events one partition at a time on every path except projection replay and catch-up. On the client, a reactor's handler is called one event at a time no matter how many events arrive. That caps throughput, and it gives an implementor no way to do the work in bulk: one bulk write to a search index, one query that loads supporting data for many read models.

### How events reach an observer

| Path | When | Unit handed to the subscriber today |
| --- | --- | --- |
| Live | The observer is up to date and events are appended now | One partition per call. Calls run one after another per observer. |
| Catch-up | The observer is behind, for example after downtime or an import ([decision 0023](0023-bulk-event-import.md)) | One partition per call, except projections, which get pages. |
| Replay | The observer is rewound | One partition per call, except projections, which get pages. |
| Retry | A failed partition is recovered | One partition per call. |

### The live path

- `AppendedEventsQueue.QueueHandler` already coalesces a burst. It drains up to 100 queued appends (`MaxCoalescedBatchesPerDispatch`) into one list (`Source/Kernel/Core/EventSequences/AppendedEventsQueue.cs:510-530`).
- `DeliverToObserver` then splits that list back into per-partition runs and awaits `observer.Handle(partition, events)` for each run, one after another (`AppendedEventsQueue.cs:628-653`, `:992`).
- The calls are sequential because the observer has a single cursor, `NextEventSequenceNumber`. Handling a later partition first would move the cursor past earlier events of another partition (comment at `:620-624`).
- So an `AppendMany` of 50 events for 50 users becomes 50 sequential grain calls. For a client reactor it also becomes 50 gRPC round trips with 50 DI scopes, each carrying one event.

### Catch-up, replay and retry

- Client-owned subscribers are capped at `Observers.MaxEventsPerSubscriberCall = 100`, one partition per call (`Source/Kernel/Core/Configuration/Observers.cs:64`).
- Non-projection replay and catch-up create one `HandleEventsForPartition` step per partition (`ReplayObserver.cs:139-166`, `CatchUpObserver.cs:169-204`).
- Replaying 300 000 events over 100 000 event sources therefore takes about 100 000 client round trips of about three events each.
- Projections already avoid this. `HandleEventsForObserver` hands pages of up to 500 events across partitions to `IObserverPageSubscriber.OnNextPage` (`HandleEventsForObserver.cs:299-331`), and the sink flushes them in bulk.

### What is already batched

- **The subscriber contract takes a collection for one partition.** It is `IObserverSubscriber.OnNext(Key partition, IEnumerable<AppendedEvent> events, ObserverSubscriberContext context)` (`Source/Kernel/Core/Observation/IObserverSubscriber.cs:26`). It returns `ObserverSubscriberResult(State, LastSuccessfulObservation, ...)`.
- **The client wire carries several events for one partition.**
  - `EventsToObserve { Partition, repeated AppendedEvent Events, ReplayState, DeliveryId, DeliverWithinMilliseconds }`
  - `ReduceOperationMessage { Partition, InitialState, repeated AppendedEvent Events, ... }`
- **The .NET client.**
  - Reducers fold the collection: one `ReducerDispatcher` call drives the per-event `Reduce` methods (`Source/Clients/DotNET/Reducers/ReducerDispatcher.cs:101`).
  - Reactors loop one event at a time and stop at the first failure (`Source/Clients/DotNET/Reactors/ReactorDispatcher.cs:200-245`).

### Records this must fit

- **[Decision 0020](0020-kernel-http-delivery-contract.md) (accepted)** defines the HTTP envelope as a list of per-partition batches with one result per batch. It sends one batch per request until the receiver declares support for more.
- **[Decision 0018](0018-partition-ownership-protocol.md) (accepted)**: every partition in a delivery must be held by the deliverer.
- **[Decision 0004](0004-ordered-lanes-observer-delivery.md) (proposed)**: a lane batch spans many keys.
- **Grain interfaces only grow within a major**: a new delivery shape is a new method with a raised `[Version]`, and the old method forwards to it.

## Decision

### 1. One delivery unit on every path: the page

Every path delivers a **page** to every observer kind: live, catch-up, replay and retry.

- A page is an ordered list of **partition batches**. Each partition batch holds that partition's events, in sequence order.
- A page is answered with **one result per partition**.
- Delivering a single partition is simply a page with one batch, so there is no second shape.
- A page is bounded by an event count and a byte size. Each observer declares its bounds (point 5), and the kernel's maximums cap them. The proposed kernel maximums are 1 000 events and 4 MiB.

### 2. Live delivery sends what the queue has already coalesced as one page

- `DeliverToObserver` stops splitting the coalesced list into sequential calls. It builds pages from it within the observer's bounds and calls a new `IObserver.HandlePage(ObserverPage page)`. The existing `Handle(partition, events)` is kept and forwards as a one-batch page.
- **No added latency by default.** The page holds what was already waiting. Under light load a page has one event; under a burst it fills up.
- An observer may declare a `MaxBatchDelay` to wait for fuller pages. It defaults to zero.
- **Inside `HandlePage`, the observer grain works per partition.**
  - It filters each partition against its cursor and failed partitions, marks the partitions in flight and checks claims (decision 0018).
  - It then makes one subscriber call for the whole page and settles the per-partition results.
  - The cursor advances to the end of the page only after every partition is settled. A failed partition is recorded at its first unhandled event.
  - This is exactly the outcome today's sequential loop produces, reached in one call.
- `LiveDeliveryBudget` still bounds live work. A page that exceeds it is handed to catch-up, as today.

### 3. One subscriber contract for pages

- A new grain method `OnNextPage(ObserverPage page, ObserverSubscriberContext context)` returns `ObserverPageResult`, with one `ObserverSubscriberResult` per partition. The existing `OnNext` forwards to it as a one-batch page.
- Every subscriber implements it:
  - **Projections** extend the page handling they already have for replay and catch-up to live and retry, with one bulk sink window per page.
  - **Reducers** load the initial states of every partition in the page with one sink read, and write the successful states in one bulk window.
  - **Reactors and reducers owned by a client** forward the page to the client (point 4).
  - **Webhooks** send several batches per request only to receivers that declare support for it, per decision 0020.
- Today's `IObserverPageSubscriber.OnNextPage(IEnumerable<AppendedEvent>, ...)` becomes the projection-only special case of this contract and is retired in the next major.
- Each partition settles on its own. `Ok` advances that partition. `Failed` parks that partition at its first unhandled event while the others proceed. No order is promised across partitions, so nothing is lost by this.

### 4. The wire to clients

- **New messages**, added to the existing `OneOf` stream messages:
  - `EventPageToObserve { DeliveryId, ReplayState, DeliverWithinMilliseconds, repeated PartitionEvents Batches }`
  - `PartitionEvents { Partition, repeated AppendedEvent Events, string InitialState }`. `InitialState` is set for reducers only.
- **The answer** is `PageResult { DeliveryId, repeated PartitionResult Results }`. `PartitionResult` has the fields of today's `ReactorResult` and `ReducerResult`.
- **Negotiation.** At registration the client declares page support and its bounds: `MaxEventsPerPage`, `MaxBytesPerPage` and `MaxBatchDelay`. A client that declares nothing keeps receiving `EventsToObserve` and `ReduceOperationMessage`, so the change is additive.

### 5. Client programming model: exactly one mode per observer

An observer is either **per-event** or **batch**. What its class declares decides which. There is never a fallback from one to the other, and the client never calls both for the same observer.

**Per-event mode** is today's code, unchanged.

- Reactor handler methods take one event. Reducer `Reduce` methods take one event and the current state.
- The client receives pages and loops them for you: for reactors, each partition's events in order; for reducers, one fold per partition.
- You get the throughput of pages without writing anything new.
- Per-event methods cannot take `BatchContext`. It belongs to batch mode only.

**Batch mode** means the class declares the event types it subscribes to with `[SubscribeTo]`, declares one batch method, and declares no per-event methods. The batch is untyped, and the method pattern-matches on the events.

```csharp
[Reactor]
[SubscribeTo(typeof(UserRegistered), typeof(UserRenamed), typeof(UserRemoved))]
public class IndexUsers(ISearchIndex index) : IReactor
{
    public async Task Handle(EventBatch batch, BatchContext context)
    {
        var operations = batch.Select(e => e.Event switch
        {
            UserRegistered registered => IndexOperation.Upsert(e.Context.EventSourceId, registered.Name),
            UserRenamed renamed => IndexOperation.Upsert(e.Context.EventSourceId, renamed.NewName),
            UserRemoved => IndexOperation.Delete(e.Context.EventSourceId),
            _ => IndexOperation.None
        });

        var response = await index.Bulk(operations, context.CancellationToken);

        foreach (var failure in response.Failures)
        {
            batch.Fail(failure.EventSourceId, failure.Exception);
        }
    }
}
```

```csharp
[SubscribeTo(typeof(AccountOpened), typeof(MoneyDeposited), typeof(MoneyWithdrawn))]
public record Account(AccountId Id, decimal Balance)
{
    public static Account? Reduce(EventBatch events, Account? current, BatchContext context) =>
        events.Aggregate(current, (account, e) => e.Event switch
        {
            AccountOpened => new Account(e.Context.EventSourceId, 0),
            MoneyDeposited deposited => account! with { Balance = account.Balance + deposited.Amount },
            MoneyWithdrawn withdrawn => account! with { Balance = account.Balance - withdrawn.Amount },
            _ => account
        });
}
```

**The batch method:**

- **Reactor.**
  - It takes `(EventBatch batch, BatchContext context)` and returns `Task` or `void`.
  - It is called once per page, and the batch spans every partition in the page, in sequence order.
  - **Returning events or other side effects from a batch method is not supported.** Any other return type is an analyzer error and a registration error. A batch reactor that needs to append does so through the event log it injects.
- **Reducer.**
  - It takes `(EventBatch events, TReadModel? current, BatchContext context)` and returns `TReadModel?` or `Task<TReadModel?>`. Returning `null` removes the read model, as today.
  - It is called **once per event source** in the page. `events` holds that event source's events in order, and `current` is its stored state.
  - The calls for the event sources of a page run in parallel within a bound, each with its own activation as today. A reducer stays a fold over one event source's events.
  - It works on static self-reducers and on `IReducerFor<T>` alike.

**The rules that keep the two modes from being confused:**

1. **The mode is decided by the parameter type, not by the method name.** A method whose first parameter is `EventBatch` is the batch method. Any other handler method is per-event.
2. **A batch observer declares its event types with `[SubscribeTo]`**, on the reactor class or on the reducer's read model or reducer class.
   - These are exactly the event types the observer registers.
   - A batch method without `[SubscribeTo]` is an error.
   - `[SubscribeTo]` on a per-event observer is an error, because a per-event observer's types come from its methods.
3. **One batch method per observer, and nothing else.**
   - A class with a batch method may not declare per-event handler methods.
   - A class may not declare two batch methods.
   - A per-event method may not take `BatchContext`.
   - Each of these is an analyzer error at compile time and a registration error at runtime, so a mixed class never connects.
4. **Pattern matching is checked.** The analyzer warns when a batch method matches on an event type that is not in `[SubscribeTo]`, since that case can never be reached.
5. **The kernel decides when a page is sent, and the observer's bounds decide how big it is.** The implementor decides what to do with it: group by partition, by type, or not at all.

**`BatchContext`** is the same type for reactors and reducers, and is read-only:

- `ObserverId`, `EventStore`, `Namespace`, `EventSequenceId`
- `Origin`: `Live`, `CatchUp`, `Replay` or `Retry`; and `IsReplay`
- `Partitions`, `EventCount`, `FirstSequenceNumber`, `LastSequenceNumber`, all describing the page
- `Deadline` and `CancellationToken`, from `DeliverWithinMilliseconds`

**`EventBatch`** is an `IReadOnlyList<EventAndContext>` in sequence order. Each element carries `Event`, `Context` and its `ReactorDelivery`, so effects can still be keyed per event.

**Reporting outcomes from a batch:**

- **Reactor:**
  - Returning normally means every partition succeeded.
  - Throwing fails every partition in the batch at its first event, and the kernel's normal failed-partition recovery takes over (decision 0004 parking, if accepted).
  - `batch.Fail(partition, exception)` or `batch.Fail(eventContext, exception)` fails that partition from that event, or from its first event. The earlier events of that partition count as handled, and the rest of the batch succeeds.
- **Reducer:** throwing, or calling `events.Fail(...)`, fails that one event source and writes no state, as today. The other event sources of the page are unaffected.
- **Retries** arrive as pages with `Origin = Retry`.

**Bounds** are declared on the observer, with `[BatchSize(MaxEvents = 1000, MaxDelayMilliseconds = 50)]` or the equivalent registration option.

- The defaults are 100 events and no delay in per-event mode, matching today, and 500 events and no delay in batch mode.

### 6. Other SDKs

TypeScript, Kotlin, Elixir and Python live in their own repositories. They follow the same wire contract, and each adopts the same two-mode rule. `Documentation/clients/where-the-clients-differ.md` records where one has not yet done so.

## Options considered

- **Batch handlers per event type, called alongside per-event methods (not taken; it was this record's first draft).**
  - One delivery would turn into several calls, one per run of consecutive same-type events.
  - A class could mix modes, so the reader of a reactor could not tell which method a given event would reach.
  - It keeps order only by keeping batches small.
  - Rejected because it calls both kinds of method for one observer.
- **When a batch method exists, prefer it and fall back to per-event methods for other types (not taken).** This is the confusion the review ruled out: which method runs would depend on the event type and on the class's other methods.
- **A marker interface such as `IBatchReactor` (not taken).** It goes against convention-based discovery and adds a second way to say what the parameter type already says.
- **A typed `EventBatch<TEvent>`, where `TEvent` is an event type or an interface the events share (not taken, see the rulings below).**
  - For: the batch is typed.
  - Against: unrelated events would need a shared interface only so one consumer can read them together. `[SubscribeTo]` and pattern matching keep the events free of that, and the analyzer check in rule 4 keeps the list and the matches in step.
- **A page-level reducer batch method that receives every event source of the page and returns a state per event source (not taken, see the rulings below).**
  - For: it could load supporting data once for many event sources.
  - Against: it is harder to write correctly, and a forgotten event source needs extra rules. One call per event source keeps a reducer a fold.
- **Returning events from a batch reactor, as per-event handlers can (not supported now, see the rulings below).** The keyed appends of decision 0012 tie each appended event to one source event, which a batch method does not have.
- **`BatchContext` in per-event methods too (not taken, see the rulings below).** It would blur the line between the modes that this record exists to keep sharp.
- **Keep live delivery per partition and batch only catch-up and replay (rejected by the review ruling).** Live is where most production traffic is, and the queue already coalesces bursts that it then serializes.
- **A batching delay on live delivery by default (not taken).** It would add latency to every single-event command for the benefit of bursts that the coalescing queue already batches. It stays an opt-in through `MaxBatchDelay`.
- **Halt the whole page at the first failed partition (not taken).** Only projection walks need the global order that halting protects. Per-partition settlement gives the same cursor outcome as today's sequential loop.

## Default if unanswered

- Live delivery stays one partition per call, one call after another per observer.
- Client replay and catch-up stay at about one round trip per event source.
- Reactors stay one handler call per event, with no way to batch external work.
- An import of 300 000 events ([decision 0023](0023-bulk-event-import.md)) inherits that cost on every observer.

## Timeline and scope

- **In scope, in this order:**
  1. **Kernel page contract.** `ObserverPage`, `HandlePage` and `OnNextPage` with forwarding and `[Version]` bumps, plus per-partition settlement.
  2. **Live pages** from `AppendedEventsQueue`.
  3. **Pages for catch-up, replay and retry** of every observer kind.
  4. **The client wire messages and negotiation**, and the .NET per-event mode on pages.
  5. **.NET batch mode.** `EventBatch`, `BatchContext`, `[SubscribeTo]`, `[BatchSize]`, analyzer rules, and `Cratis.Chronicle.Testing` scenario support.
  6. **Documentation.**
- **Out of scope:**
  - Webhook receivers beyond what decision 0020 already allows.
  - Parallel delivery of several pages to one observer at once. That is decision 0004's lanes.
  - SDKs outside this repository.
- **Release:** `minor`. Every step is additive and negotiated. Old clients and old silos keep the per-partition methods until the next major.
- **Horizon:** if decision 0004 is accepted, a lane batch is an `ObserverPage`, and this record's per-partition settlement becomes per key.

## Verification

- **Done when:**
  - An `AppendMany` of 50 events for 50 event sources reaches a .NET reactor in one call, for a batch-mode or per-event reactor alike, instead of 50.
  - A replay of N events over P partitions makes about N / page-size client calls instead of P.
  - A batch reactor that fails one partition with `batch.Fail` sees only that partition parked while the observer's cursor reaches the end of the page.
  - Paged delivery produces read models identical to per-partition delivery, for projections and for reducers in both modes.
  - A class mixing a batch method and a per-event method fails to compile with the new analyzer rule, as do a batch method without `[SubscribeTo]`, a batch reactor method returning events, and a per-event method taking `BatchContext`.
  - A client that declares no page support still receives `EventsToObserve`.
- **Verify by:**
  - Integration specs under `Integration/` for:
    - live paging with mixed partitions, in per-event and batch mode
    - per-partition settlement with one failing partition
    - reducer parity between paged and per-partition delivery
    - `Origin` on the context for live, catch-up, replay and retry
    - an old-shape client against the new kernel
  - Analyzer specs for each mode rule in point 5, and for the warning on a pattern match outside `[SubscribeTo]`.
  - A benchmark under `Benchmarks/` for 300 000 events over 100 000 event sources, live and in replay, reporting subscriber calls and wall time before and after.

## Consequences

- **Easier:**
  - Throughput scales with events, not event sources, on every path.
  - Implementors can bulk-write and bulk-load.
  - Existing reactors and reducers speed up with no change.
  - One delivery shape serves the kernel, gRPC clients and HTTP receivers.
- **Harder:**
  - The observer grain settles many partitions per call, and checks claims for each.
  - Reducer pages hold many states in memory, bounded by the page's byte limit.
  - Batch-mode code pattern-matches on event types instead of getting one typed method per type.
  - A batch implementor must report partial failures explicitly or accept whole-batch failure.
- **Forecloses:**
  - Mixing per-event and batch methods in one observer.
  - Side effects returned from batch reactor methods, until a later record defines how they are attributed.
  - Promising any order across partitions beyond the page being in sequence order.
- **Open questions:**
  - **Deferral:** whether a batch reactor should be able to defer a partition, not only fail it.
  - **Concurrent reducer calls:** the bound on how many event sources of one page are reduced at once on the client.

## Rulings from review

Rulings given by Einar on 2026-11-03 while the record is still proposed. The rest of the record stays open for a verdict.

- **Every observer can take batches, on every path, live included.** One event at a time is not the model.
- **Clients take batches, and the implementor decides how to handle them**, with a context type that carries what they need to decide.
- **An observer's batch method and its per-event methods are never both called.** The design must make that impossible to get confused about.
- **Batch observers declare their event types with an attribute**, `[SubscribeTo(...)]`, and pattern-match on the events inside the batch method.
- **Batch reactor methods do not return events** for now.
- **A reducer's batch method is called once per event source**, with the same pattern-matching approach.
- **Per-event methods cannot use `BatchContext`.**
