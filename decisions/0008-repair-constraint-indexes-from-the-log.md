---
id: 0008-repair-constraint-indexes-from-the-log
title: Keep constraint indexes in step with the event log through a per-sequence watermark
status: accepted
stage: none
class: contract
reversibility: costly
decided: 2026-10-14
decider: L0 (woksin's delegation)
applies-to:
  - Source/Kernel/Core/EventSequences/**
  - Source/Kernel/Core/Events/Constraints/**
  - Source/Kernel/Storage/EventSequences/**
  - Source/Kernel/Storage/Events/Constraints/**
  - Source/Kernel/Storage.InMemory/EventSequences/**
  - Source/Kernel/Storage.InMemory/Events/Constraints/**
  - Source/Kernel/Storage.MongoDB/EventSequences/**
  - Source/Kernel/Storage.MongoDB/Events/Constraints/**
  - Source/Kernel/Storage.Sql/EventStores/Namespaces/EventSequences/**
  - Source/Kernel/Storage.Sql/EventStores/Namespaces/UniqueConstraints/**
  - Integration/Kernel/Events/Constraints/**
  - Documentation/constraints/**
  - Documentation/closing-streams/**
---

<!-- markdownlint-disable-next-line MD025 -->
# Keep constraint indexes in step with the event log through a per-sequence watermark

Accepted on 2026-10-14 by L0 on woksin's delegation for Cratis/Chronicle#4895, including the background catch-up (item 8) and closing constraints covered only from the upgrade on. This extends [Decision 0005](0005-settle-unique-constraint-claims-in-every-store.md). [Decision 0007](0007-idempotent-keyed-appends.md) (idempotent keyed appends, accepted the same day) defers its constraint gap to #4895; statements below that mention 0007 apply once it is implemented.

## Context

### How claims are written today

Unique-constraint claims and closed-stream closures are written after the event is durable, in a separate write.

- **Where the update runs.** `CompleteDurableAppend` (`Source/Kernel/Core/EventSequences/EventSequence.cs:945-978`) runs three steps in order:
  1. it persists the state snapshot (`:949-956`);
  2. it hands the events to the appended-events queues (`:958-965`);
  3. it calls `ConstraintValidationContext.Update` for each event, catching and only logging every exception (`:968-977`).

  Its own remarks say the index must be written "because a value that is durable in the log but missing from its unique index is invisible to validation and lets a later duplicate through" (`:940-943`). A failed write nevertheless leaves the value missing.
- **What 0005 settled.** Decision 0005 kept "the event is durable before the index write" and made `DuplicateUniqueConstraintValue` diagnostic (`Source/Kernel/Storage/Events/Constraints/DuplicateUniqueConstraintValue.cs:20-25`). It added no repair.
- **How the updates are built.**
  - **Validators.** The factory builds one validator per definition that applies to the sequence, plus a closed-stream check that applies to every event (`Core/Events/Constraints/ConstraintValidationFactory.cs:26-43`; `ClosedStreamConstraintValidator.cs:24`).
  - **Updaters.** The context takes one updater from each validator that keeps an index (`ConstraintValidationContext.cs:46`). `Update` runs them with `Task.WhenAll` (`:125-128`), so when two fail, only the first exception surfaces. `IUpdateConstraintIndex.Update(EventSequenceNumber)` takes no cancellation token (`IUpdateConstraintIndex.cs:18`).
  - **The unique updater** (`UniqueConstraintIndexUpdater.cs:22-73`):
    - releases on removal types (`:28-43`);
    - ignores types it does not cover (`:46-49`);
    - releases a per-event-source claim whose values are all empty (`:59-68`);
    - otherwise calls `Save` (`:70-71`).
  - **The closing updater** (`ClosesStreamIndexUpdater.cs:25-39`):
    - closes or reopens only its own owner's scope;
    - throws `InvalidClosingStreamScope` when the scope cannot be resolved (`:29`);
    - stamps `ClosedAt` with the current time (`:37`).
  - **Inputs.** Both updaters use only the context (event source, event type, content, stream dimensions) and the sequence number. A context can therefore be built from a stored event, and `ReindexConstraintsStep` already does this:
    - it builds the context from the stored event (`ReindexConstraintsStep.cs:74-95`);
    - it releases PII to plaintext first (`:176`, `:211-222`);
    - it skips subjects whose key was erased (`:83-93`).

    The closing reindexer handles redacted events through their `originalEventType` (`ClosesStreamConstraintReindexer.cs:54-81`). It stages the final closure per scope in memory and writes only at the end (`:51-114`).
- **Store semantics.** Each operation is a deterministic function of the event and the current entries for its source or value. No store checks sequence order.
  - **Per-event-source claims** replace the source's entry (MongoDB `Storage.MongoDB/Events/Constraints/UniqueConstraintsStorage.cs:147-162`; InMemory, under one lock, `Storage.InMemory/Events/Constraints/UniqueConstraintsStorage.cs:62-91`). A held value raises `DuplicateUniqueConstraintValue`, and the old claim stays.
  - **Per-value claims** are added with `SetOnInsert` (MongoDB `:92-98`), so they never replace. `Remove` deletes every value of the source (`DeleteManyAsync`, `:120-122`), and `RemoveValue` deletes one.
  - **Closures** are upserted or deleted by owner and scope (`Storage.MongoDB/Events/Constraints/ClosedStreamsConstraintStorage.cs:57-69`).
- **Same-source claims.** `IsAllowed` lets a source claim a value it already holds (`Storage.MongoDB/.../UniqueConstraintsStorage.cs:62`; InMemory `:43-50`).
- **Reads.** `IUniqueConstraintsStorage` can answer `IsAllowed` for one value, but it cannot list the values a source holds (`Storage/Events/Constraints/IUniqueConstraintsStorage.cs:20-63`). Closures can be read per owner (`IClosedStreamsConstraintStorage.cs:48`).

### Failure paths

1. **A failed update is swallowed** (`EventSequence.cs:968-977`). The causes include:
   - a transient storage error;
   - a settled-store `DuplicateUniqueConstraintValue`;
   - `InvalidClosingStreamScope`.
2. **An exception after the commit.**
   - **Batch path.** The sequence number advances at `:685`, then `InvalidAppendAcknowledgment` is thrown at `:703` before `CompleteDurableAppend` (`:714`). `AppendMany` turns it into a failed result (`:404-411`).
   - **Single path.** Any throw after the storage write lands in `HandleAppendEventException` (`:903-905`). Storage itself throws after a durable write in two places:
     - MongoDB resolves identities after `InsertOneAsync` (`Storage.MongoDB/EventSequences/EventSequenceStorage.cs:273`, `:302`);
     - SQL does the same after `SaveChangesAsync` (`Storage.Sql/.../EventSequences/EventSequenceStorage.cs:192`, `:211`).
   - **Commit errors.** A MongoDB batch commit error is rethrown after an abort attempt (`EventSequenceStorage.cs:428`, `:440-444`). When the commit result is unknown, the batch can still be durable.
   - **Effect.** The client is told that a durable batch failed. Because the same source may reclaim its own value (above), a retry is admitted and appended a second time.
   - **Not a post-commit path.** The issue also cites `:1037`. That catch belongs to `GetValidAndCompliantEvent`, which runs before the commit, so it is not a post-commit path.
3. **An ungraceful kill** between the commit (`:685/714` batch, `:884-899` single) and the update.
4. **Publication retries apply stale mutations.** `ReconcileExistingPublication` builds a context from the retry's content (`EventSequence.Validation.cs:169-178`) and writes claims at the receipt's number without regard to later events.

### Why nothing repairs it

- **Activation** rebuilds only the sequence number and the per-type tails (`EventSequencesStorageProvider.cs:33-44`).
- **`RebuildStaleIndexes`** starts a reindex only when unique definitions exist (`ConstraintIndexes.cs:42-45`), and only for unique definitions that changed (`ConstraintDefinitionComparison.cs:66-95`, started at `ConstraintIndexes.cs:76-90`). A closing constraint is rebuilt only when a job names it (`ReindexConstraintsStep.cs:130-131`). Nothing in the kernel names one: the only starters are `ConstraintIndexes.cs:86` and two patches (`Patches/15.38.5/RebuildConstraintIndexes.cs:58`, `Patches/16.4.1/RebuildConstraintIndexesForDecryptedValues.cs:64`).
- **A failed reindex** returns the exception as a step failure (`ReindexConstraintsStep.cs:181-200`), after it may already have cleared a scope (`:69-72`). Nothing restores it.
- **The documentation** states the gap (`Documentation/constraints/index.md:51`, `Documentation/closing-streams/index.mdx:101-102`).

### Facts the design rests on

- **Non-reentrancy.** `IEventSequence` (`IEventSequence.cs:22`) is non-reentrant; only reads are `[AlwaysInterleave]` (`:61, 72, 83, 97`). Every append, update and repair is therefore serialized within one activation. Two activations of the same sequence can still overlap during SQL silo handover (0005, context).
- **Closing constraints are not retroactive.** "Registering or changing a declaration does not start a reindex and closes nothing in history" (`Documentation/closing-streams/index.mdx:102`, `closing-events.mdx:63`). A scope closed by an event cannot be reopened by hand (`EventSequence.cs:561-564` returns `ClosedByEvent`).
- **The state snapshot.**
  - `EventSequenceState` holds the next sequence number and the per-type tails (`Storage/EventSequences/EventSequenceState.cs:11-22`).
  - It is written through `SaveState`:
    - MongoDB `EventSequenceStorage.cs:74-86`;
    - SQL `:81-87`;
    - InMemory, where `SaveState` is a no-op (`Storage.InMemory/EventSequences/EventSequenceStorage.cs:101`) and `GetState` returns only the next sequence number (`:87-98`).
  - The snapshot is written on the first append after activation, then every `StatePersistenceInterval` appends (default 1,000, `Core/Configuration/Events.cs:45`), and on deactivation when appends are unwritten (`EventSequence.cs:165-174`, `:1223-1236`).
  - InMemory caches the event-sequence storage and the constraint storages per sequence for the life of the process (`Storage.InMemory/EventStoreNamespaceStorage.cs:115-135`).
- **Event-type filters.** `GetFromSequenceNumber` takes `EventType` values (`Storage/EventSequences/IEventSequenceStorage.cs:307`). Every provider matches them on the id alone (MongoDB `EventSequenceStorage.cs:100`, SQL `:786-789`, InMemory `:735`).
- **Transactions.** A MongoDB batch is one session transaction (`EventSequenceStorage.cs:346-350`, committed at `:428`). A SQL batch is one `SaveChangesAsync` (`Storage.Sql/.../EventSequenceStorage.cs:357`).
- **Stored hashes.** Each stored event carries the content hash of its appended generation in its context, or `EventHash.NotSet` when none was computed (`Concepts/Events/EventContext.cs:42`, `Storage.MongoDB/EventSequences/EventSequenceStorage.cs:395`, `Concepts/Events/EventHash.cs:15`).
- **Timers.** The only existing grain timer interleaves (`EventSequence.cs:147-154`). In Orleans 10 a grain timer keeps the activation alive only with `KeepAlive = true`.
- **Response timeout.** A grain call times out after 30 seconds by default (`Core/Configuration/Clustering.cs:86`).
- **Packaging.** Kernel projects are not packaged, except `Contracts` (`Source/Kernel/Directory.Build.props:9-10`). Wire contracts are generated from Core Arc artifacts (`Core/Core.csproj:47-53`), for example `ReplayObserver` (`Core/Observation/ReplayObserver.cs:18-32`).

This crosses every provider and changes what an append reports after its commit. It adds two refusals and three persisted fields, so it needs a durable decision.

## Decision

Each event sequence keeps a durable **constraint watermark**. The kernel retries a failed index update within a bounded budget. When updates cannot be confirmed, it **folds** the stored events from the watermark to the tail, in log order, into a staged final outcome per index key. It then writes only the keys whose staged outcome differs from the index: every release first, then the claims. Intermediate states are never written.

While a constraint's range is not fully repaired, appends that constraint validates are refused. They are never admitted on a partial state. An append whose events are durable is never reported as failed. Existing stores catch up their unique-constraint history in the background without moving any key backwards. Closing constraints are covered from the upgrade onward, which is today's guarantee.

### 1. The watermark

- **Definition.** `EventSequenceState.ConstraintIndexWatermark` (`EventSequenceNumber?`) is exclusive: every event below it has had every index update applied or settled (item 2). There are two exceptions:
  - unique-constraint updates below the catch-up boundary while a catch-up is pending (item 8);
  - closing-constraint updates for events before the constraint's coverage start (item 3).

  It is the ruled "highest sequence number whose updates all succeeded", plus one. It is stored the same way as `EventSequenceState.SequenceNumber` (`EventSequenceState.cs:13-16`), so an empty sequence is `First` and needs no sentinel. `null` means it was never recorded (item 8).
- **Catch-up boundary.** `EventSequenceState.ConstraintIndexCatchUpBelow` (`EventSequenceNumber?`) is set while a background catch-up of the unique-constraint history below it is pending (items 7 and 8). Otherwise it is `null`.
- **Catch-up refusal marker.** `EventSequenceState.ConstraintIndexCatchUpRefusing` (a list of constraint names, `null` when empty) names the constraints whose catch-up write is in progress (item 8). It is set before the first write of a catch-up write turn and cleared when the writes it covers are done. While it holds a name, that constraint's governed appends are refused with `ConstraintIndexUnavailable`, and the refusal survives a restart.
- **Pending constraints.** In memory, the grain keeps for each index-keeping constraint the first event whose update for that constraint is not yet applied. A constraint with such an event is **pending**.
  - The in-memory watermark is the lowest pending event, or the next sequence number when nothing is pending.
  - It advances only contiguously.
- **Gaps.** A jump in the sequence number makes every constraint pending from the first missing number. Jumps come from:
  - duplicate renumbering (`EventSequence.cs:1159`, `:1178-1200`);
  - a publication receipt (`EventSequence.Validation.cs:162-165`);
  - a keyed `AlreadyAppended` hit, if 0007 lands.

  Events another activation wrote are therefore always folded, never skipped.
- **Persistence.** The watermark, the boundary and the refusal marker are persisted inside the state snapshot. The snapshot is also written after every completed repair, when a catch-up starts or ends, and when the marker is set or cleared (item 8). In `CompleteDurableAppend` the order becomes:
  1. enqueue;
  2. constraint updates;
  3. snapshot.

  Update retries therefore never delay live delivery to observers, and the snapshot carries the watermark the updates produced.
- **Lag and lead.** The grain writes the watermark after the index writes it covers and never ahead of an awaited write. Within one activation a persisted watermark can lag but does not lead. A lag only costs folding (item 3). Four cases can still make the index lag the watermark:
  - **A reindex job clearing a scope.** The job notifies the grain before it clears anything (item 7).
  - **An operator editing the persisted state while the grain is active.** The grain would overwrite the edit in `SaveState` or `OnDeactivateAsync` (`EventSequence.cs:165-174`). The procedure therefore stops the kernel on every silo first (item 10).
  - **Duplicate activations** (SQL handover, 0005). An older activation's delayed write can land after a newer activation applied a later event for the same key, when both watermarks are already past both events.
  - **An abandoned attempt that lands late** (item 2).

  The last two are residuals that only a reindex clears (item 10).
- **Providers.**
  - **MongoDB.** `constraintIndexWatermark`, `constraintIndexCatchUpBelow` and `constraintIndexCatchUpRefusing` (an array of constraint names) fields on the existing state document. `SaveState` adds them to its `$set`, and `GetState` maps a missing field to `null`.
  - **SQL.** Nullable `ConstraintIndexWatermark`, `ConstraintIndexCatchUpBelow` and `ConstraintIndexCatchUpRefusing` columns on `EventSequences`, added by a namespace migration `NS-EventSequences-v<release>`. The first two are `decimal(20,0)`, or `TEXT` on SQLite, following `ClosedStreams/Migrations/v19_38_0.cs:33-40`. The third is a text column holding the constraint names as a JSON array. The add-column precedent is `Observers/Migrations/v19_15_0.cs:17-20`.
  - **InMemory.** The three values are kept in `EventSequenceStorage` under its lock, and `SaveState` stops being a no-op for them. Their lifetime is that of the cached constraint storages.
- **Why grain state and not constraint storage.**
  - The watermark covers two stores (claims and closures). Claims are spread over per-constraint collections and tables, and on SQL they sit in their own DbContext. No single constraint store could hold the watermark atomically with what it covers, and writing it afterwards is sufficient.
  - The state document or row already exists in every provider and is already read on activation (`EventSequencesStorageProvider.cs:40`).

### 2. Updating within the turn

`IUpdateConstraintIndex.Update` takes a `CancellationToken`, and so do the constraint storage calls it makes. `ConstraintValidationContext.Update` returns one outcome per updater instead of using `WhenAll` and surfacing the first exception. The grain classifies each outcome:

| Outcome | Examples | Retried | The constraint |
| --- | --- | --- | --- |
| Applied | write succeeded | - | advances |
| Settled | `DuplicateUniqueConstraintValue`: the store decided, as 0005 rules (logged as today) | no | advances |
| Settled with error | `InvalidClosingStreamScope`, `RedactedClosingEventTypeUnavailable`, or a deterministically failing event (item 4); can never succeed (logged as an error, counted) | no | advances |
| Failed | any other exception, or an attempt that ran past its timeout | yes | pending |

- **Per attempt.** Each storage attempt is cancelled after 1 second. An attempt that ignores cancellation is abandoned at that point and counts as Failed.
- **Retry.** The first failure of a constraint within a turn is retried up to twice, after 100 ms and 400 ms pauses, while the turn's budget allows. Retrying an updater is idempotent. Catch-up group writes (item 8) are the exception: they are attempted once. Later events for that constraint in the same turn are not attempted.
- **Turn budget.** All post-commit constraint work in one turn is capped at 2 seconds. When the cap is reached, the remaining updates are not attempted, and their constraints become pending from the first update not attempted.
- **After the last attempt:**
  - the append is still reported as successful;
  - the constraint is pending from the failed event;
  - while it is pending, its live updates are deferred to the repair: an appended event's update for it is never written live but left to the fold;
  - live updates for other constraints continue. The repair folds only pending constraints, so nothing a constraint applied live is written again below what it already applied.
- **Abandoned attempts.** A store can still apply an abandoned write after the grain has moved on. After an abandoned attempt, the constraint stays pending for one more timer turn. That turn folds the range again and rewrites any key that differs, which corrects a write that landed in the meantime. A write that lands later still is a residual (item 1).

### 3. Ordered repair from the log: the staged fold

**`ConstraintIndexReplay`** (`Core/Events/Constraints`) takes over the stored-event logic of `ReindexConstraintsStep` and is shared with it.

- **Reading.** It reads the range through `GetFromSequenceNumber` (`IEventSequenceStorage.cs:307`), filtered to the event types the folded constraints read: claim, removal, closing and reopening types. Redactions are added to the filter when a closing constraint is folded.
  - The filter matches by event type id across all generations, never only the current one. The replay passes one `EventType` per id, and the providers already match on the id alone (Context). A spec pins this.
  - If no constraint is folded, nothing is read. An empty filter would mean "do not narrow", so it is never passed.
- **Content.** It releases PII before hashing, as the reindex does (`ReindexConstraintsStep.cs:211-222`), and skips erased subjects (`:83-93`).

**The fold.** The replay applies each event, in log order, to a staged map in memory instead of to the store. It uses the updaters' semantics and the current definitions. It never writes intermediate states. Each staged key remembers the event that last decided it.

| Constraint kind | Staged key | Staged outcome |
| --- | --- | --- |
| Unique, per event source | constraint, scope key, event source | claimed value, or released |
| Unique, per value | constraint, scope key, event source, value | held, or released |
| Closing | owner, scope | closed (sequence number and the event's `Occurred`), or reopened |

- **Per-value mode.** When the fold first touches a source in per-value mode, it stages the values the store holds for that source. A full release (`Remove`) then releases all of them, and `RemoveValue` releases one.
- **Settled with error.** An event whose effect cannot be resolved (`InvalidClosingStreamScope`, `RedactedClosingEventTypeUnavailable`) is settled with error for that constraint, and the fold continues.

**The write.** When a segment's fold reaches its end point, the grain reads the current entry of every staged key in the segment. It keeps the keys whose staged outcome differs, which are the segment's **differences**, and drops the rest of the segment's staged map. Writing starts only after the differences of every segment are collected.

- **Reads.** A new kernel read `IUniqueConstraintsStorage.GetFor(eventSourceId, definition, scopeKey)` returns the values a source holds, in every provider. Closures are read through the existing `GetForOwner`.
- **Two phases.** Every store refuses a claim while another source holds the value. MongoDB does so in `ReplaceOneAsync` (`Storage.MongoDB/Events/Constraints/UniqueConstraintsStorage.cs:153-161`), InMemory before it releases the source's previous claim (`Storage.InMemory/Events/Constraints/UniqueConstraintsStorage.cs:68-73`), and SQL on the unique index (`Storage.Sql/.../UniqueConstraints/UniqueConstraintsStorage.cs:201-208`). A value must therefore be free before its new holder claims it, whatever order the log decided the two keys in. Writes run in two phases:
  1. **Releases.** First, every key whose current entry holds a value that the staged map no longer gives that key is released, across all segments, before any claim is written. That is `Remove` in per-event-source mode and `RemoveValue` in per-value mode.
  2. **Claims.** Then claims are written in ascending order of each key's deciding event. Closures are written in this phase too. No other key can refuse a closure.
- **A refused claim.** A claim refused with `DuplicateUniqueConstraintValue` counts as settled only when its holder is not a key the fold changes, that is, not among the differences. Otherwise it is retried once all releases are written, when the holder has given the value up. A refusal on that retry is settled.
- **What a settled refusal leaves.** The holder keeps the value. The refused source has no entry for it:
  - in per-value mode, the claim simply was not added;
  - in per-event-source mode, the release phase already removed the source's old value, which the log had released. This differs from the live path, where a refused `Save` leaves the old entry in place (0005).

  The refusal is counted.
- **Outcomes.** Write outcomes are classified as in item 2.
- **Budget.** If a turn's budget runs out during the write phase, the written keys stand and the rest are written in the next turn. The constraint stays pending until all of them are written. In steady state that is safe, because the pending constraint's governed appends are refused meanwhile (item 4). The catch-up has its own rule (item 8).

This is how `ClosesStreamConstraintReindexer` already stages scopes (`:51-114`).

**Why no key moves backwards.** A written key moves from its current entry to its state as of the fold's end point. A key that changes value passes through "released" on the way, which is not an older state. The state at the end point is the key's current truth when nothing for the key after the end point is missing from the fold:

- **Steady-state repair** (item 4) fixes its end point at the tail when the fold starts. It defers the pending constraint's live updates to the fold (item 2), so no event for a folded key after the end point has been applied. When the writes are done, the constraint is pending from the end point, and the next fold, usually a short one, starts there.
- **The catch-up** (item 8) folds to its boundary and skips every key that any event at or after the boundary touches.

**Memory.** The staged map and the collected differences together hold at most 100,000 keys per sequence, about 25 MB at roughly 250 bytes per key.

- **Segments.** When a fold would exceed the cap, it splits keys into segments by key hash, doubling the segment count each time. It keeps the staged map of the current segment only, and makes one pass over the range per segment, rereading the range each time. Each pass adds its segment's differences to the collected set.
- **Many differences.** If the collected differences reach half the cap before every segment is folded, the fold writes them in the two phases before it folds the remaining segments. A claim refused by a holder that a remaining segment may change is kept and retried once the remaining segments' releases are written. A refusal then is settled. This needs more than 50,000 differing keys in one sequence.
- **Re-folding.** Re-folding a range gives the same outcome, and the second write finds no difference.

**When a steady-state repair runs.** It folds the pending constraints from their first pending event to its end point, the tail when it starts:

- on activation, for every index-keeping constraint from the persisted watermark;
- after any failed update;
- after a sequence-number gap;
- after a reindex handoff (item 7).

Before the validation of an append that a pending constraint governs, it runs at most once per 5 seconds per sequence, whatever the outcome (item 4).

**Scheduling.**

- It runs inside the append's turn, right after `RefreshConstraintsIfChanged` (`EventSequence.cs:353`, `EventSequence.Validation.cs:58`).
- It also runs from a non-interleaving grain timer:
  - `Interleave = false`, unlike the pattern-capture timer at `EventSequence.cs:154`;
  - `KeepAlive = true` only while a constraint is pending or a catch-up is running, so the activation stays alive exactly while it has repair work;
  - a period of 1 second, measured from the end of the previous timer turn, so at most one timer turn is ever queued;
  - every timer turn is bounded (item 4).
- Appends therefore wait for it, while reads keep interleaving.
- Between append-turn runs, only the timer advances the fold.
- `OnActivateAsync` does not fold. It reads the persisted state, marks constraints pending and registers the timer. A failing store must not fail activation and, with it, every read.
- The fold's progress (read position, staged map and collected differences) stays in memory across turns, and each turn continues it toward its end point.

**Coverage start for closing constraints.** A closing constraint is never folded below the point the grain started covering it:

- the persisted watermark on activation;
- the watermark at the upgrade (item 8);
- the tail when a refresh first applies a newly registered definition.

Historical closing events are therefore never applied. There is one residual: a closing definition registered while the grain is inactive, followed by an activation with a lagging watermark. The fold then applies the definition to the lagging range's events that were appended before registration. That range is at most the lag. The coverage start is kept in memory only, so this residual can recur on any such activation. A closure it writes is permanent, because a scope closed by an event cannot be reopened by hand (Consequences).

**Residuals cleared only by a reindex** (item 10):

- phantom claims with no log basis, such as rows left by a reindex race (0005);
- duplicate holders from past damage, where the fold keeps the current holder rather than the first claimant in log order;
- writes from duplicate activations and late abandoned attempts (items 1 and 2).

### 4. Refusal, bounds and poison events

**Refusal.** While a constraint is pending, every append it validates is refused with the new well-known error `AppendError.ConstraintIndexUnavailable`, in the style of `Core/EventSequences/AppendError.cs:30`. Nothing is written. Such an append is never admitted on a partial state.

- **A pending unique constraint** governs the events it validates, which are its claim types (`UniqueConstraintValidator.cs:23-24`). Its removal types are not validated by it. They are admitted, and their update for it joins the fold.
- **A pending closing constraint** governs every event, because the closed-stream check validates every event (`ClosedStreamConstraintValidator.cs:24`) once closure dimensions are in use (`:29`). On a sequence with closures in use, that means every append.
- **If the range cannot be read at all,** every index-keeping constraint is pending.
- **Batches.** A batch is refused as a whole when any of its events is governed.
- **Other appends** proceed. They are validated only by constraints that are not pending, whose index is current up to the tail (item 2), so they cannot be decided wrongly.

**Client contract for `ConstraintIndexUnavailable`.** Nothing was stored. The append can be retried unchanged after a delay. It is not a constraint violation and says nothing about the value.

**Time budgets.** These are internal constants:

| Work | Bound |
| --- | --- |
| One storage attempt | 1 s, cancelled through the token (item 2) |
| Post-commit constraint work in one append turn | 2 s in total, including retries (item 2) |
| Steady-state repair before validation in one append turn | 5 s, run at most once per 5 s per sequence, measured from the end of the previous run, whatever its outcome, and only when the backoff (below) allows |
| Read-back after a storage exception (item 9) | 2 s |
| One timer turn, repair or catch-up | 2 s, and at most 1,000 constraint-relevant events for a catch-up. A catch-up write turn may run past that to finish the one write group it started, which is at most 2 writes, each attempted once without the item 2 retries, so it ends within 4 s (item 8) |

- **Rate of append-turn repairs.** An append turn runs the repair before validation at most once per 5 seconds per sequence, whether that run finishes, runs out of budget or fails. Between those runs, governed appends are refused at once without any store work, and only the timer advances the fold.
- **Arithmetic.** Ahead of any append there is at most one timer turn (4 s), because the timer's period starts when its previous turn ends, and one append turn that ran the repair (5 s repair, 2 s post-commit, 2 s read-back). The append then pays its own 2 s and 2 s. That is 17 s, which leaves 13 s of the 30-second default response timeout (`Clustering.cs:86`) for the validation reads, compliance and commits of those two appends.
- **Queued appends.** Appends queued behind each other otherwise pay only their own turns, as today. None of them pays the repair or retry cost again:
  - a pending constraint's governed appends are refused without a store call;
  - its deferred updates are not attempted live.
- **A smaller timeout.** An operator who lowers the response timeout below about 21 s loses this margin (#4906).

**How long governed appends are refused.** A pending range of N constraint-relevant events whose fold needs S segments stays pending for about S times the time it takes to read N events, plus the time to write the differences, because every segment rereads the range.

- **Without traffic,** timer turns fold 2 seconds out of every 3, which stretches that time by about half.
- **With governed traffic,** an append turn adds up to 5 seconds of folding every 5 seconds.
- **Example.** A range that fits in one segment and reads in 20 seconds keeps governed appends refused for about 30 seconds. With four segments, that becomes about two minutes.

A crash after a long outage can leave a range this large (item 12).

**When a repair does not finish.**

- **Budget.** If a turn's budget expires before the fold reaches its end point, nothing is written for the unfinished fold. The turn refuses the governed appends, and the timer continues the fold. If the budget expires during the write phase, the written keys stand, the rest are written in the next turn, and the constraint stays pending until all are written (item 3).
- **Backoff.** After an attempt fails with a storage error, attempts back off from 1 second, doubling to 60 seconds. Between attempts, governed appends are refused at once.
- **Without traffic.** The timer keeps retrying, so the sequence heals with no append needed.

**Poison events.** An event can fail the same way every time, for example through a missing schema or a serialization fault. Such an event is classified as settled with error for that constraint when all of these hold:

- it fails for the same constraint, with the same exception type, on 5 consecutive attempts;
- the provider does not classify the exception as transient (connection, timeout, cancellation, write concern, deadlock);
- other storage calls in those attempts succeeded, namely the fold's read of the range or writes for other keys.

**Events that cannot be read.** A deterministic fault while the fold reads an event, such as a missing schema or a deserialization error, fails the range read itself, so "the fold's read of the range succeeded" can never hold for it. Such a fault is therefore tied to its sequence number. It counts as a failure of that event for every folded constraint, and it is classified per event by the rule above, with the reads of the events before it as the other storage calls that succeeded. When the event is settled with error, the cursor resumes after it.

When an event is settled with error under this rule, the grain:

- logs an error naming the sequence, the event, the constraint and the exception. That log entry is the operator's record;
- counts the event;
- lets the constraint advance, so its governed appends are admitted again.

The index then lacks that event's effect until the cause is fixed and a reindex runs (item 10).

Why 5: the backoff spans about 31 seconds across 5 attempts (1 + 2 + 4 + 8 + 16 s). Within that span, a transient fault rarely repeats with the same exception type while reads and other writes succeed. The limit also caps at about half a minute the refusal that one bad event causes. Failing closed for good would turn one malformed event on a sequence with closures into a permanent outage of every append.

**Signals.**

- An error-level log on each failed attempt, carrying the sequence, the watermark, the tail and the first failing event.
- Counters for update failures, repairs, refused appends and poison events.
- The lag (tail minus watermark) as a measurement.

A first-class alert condition is not in this decision (#4904). Alert targets are observer-shaped (`Core/Alerts/AlertTarget.cs:23-28`; `ObserverId` is wire field 3 in `Contracts/Alerts/AlertTarget.cs:32-33`), so it needs a contract change.

The status quo already fails these appends when the store is down, because validation reads it. The refusal mainly adds safety for the case where writes fail while reads succeed, and for the time between recovery and repair.

### 5. Publication reconciliation

`ReconcileExistingPublication` no longer builds a constraint context from the retry's content (removing `EventSequence.Validation.cs:169-178`). It still advances the sequence number and the tails, and still enqueues the event.

- If the receipt is below the watermark, no constraint write happens.
- If it is at or above the watermark, the receipt is a gap (item 1). The fold folds from it using the **stored** events. A later change or removal that is already in the log therefore stands.

### 6. Revision, redaction and changed definitions

- **Revision.** `Revise` rewrites content in place and does not touch the index (`EventSequence.cs:416-450`).
  - Below the watermark, nothing changes. This is today's behavior and out of scope here (#4905).
  - Within a folded range, the revised content is used, as a reindex would use it.
- **Redaction.** Redaction replaces the event with `EventRedacted` (`ClosesStreamConstraintReindexer.cs:54`).
  - The fold treats a redacted unique event as no event, as the reindex does (`UniqueConstraintIndexUpdater.cs:46-49`).
  - Where the range covers a key's earlier events, the outcome equals what a reindex produces. For example, X claims v at 5 and w at 9, and 9 is redacted. A fold from 5 or earlier writes X→v.
  - Where the redacted event is the only one for the key in the range, the key keeps its current entry, which can be the live-written claim (X→w). Only a reindex resets it.
  - A redacted closing or reopening event is folded with its original type when its scope comes from the event context.
  - When the scope comes from a property, the fold uses the stored closure with the same sequence number when there is one, as the reindexer does (`ClosesStreamConstraintReindexer.cs:60-75`). Otherwise the event is settled with error and logged. The reindexer skips the whole owner instead, which a fold cannot do without leaving the constraint pending.
  - Manual closures are never touched, because updaters change only their own owner (`EventSequence.cs:540`).
- **Changed definitions.**
  - Folds use the definitions current at the time of the fold.
  - A **unique** definition change starts the reindex job for the affected sequences (`ConstraintIndexes.cs:42-45`, `:76-90`). That job is the one writer allowed below the watermark, because it rebuilds a scope from the log in order.
  - A **closing** definition change starts nothing (`ConstraintDefinitionComparison.cs:66-95`), as documented. Closures are rebuilt only by a job that names the constraint (`ReindexConstraintsStep.cs:130-131`). After this decision, only the operator command (item 10) starts such a job.
  - The watermark is not reset by a definition change. The job's notifications (item 7) cover the race.

### 7. Reindex job handoff

The job tells the grain before it removes protection, and again on every terminal outcome. Both calls are kernel grain methods, not wire calls, and both are idempotent.

- **Before clearing.** Before its first `ClearValues`, and before the closing reindexer's commit, `ReindexConstraintsStep` calls `IEventSequence.ConstraintIndexRebuildStarting(constraintNames)`. The grain:
  - marks the named constraints as rebuilding, so neither a fold nor the catch-up writes them while the job runs. Their live updates continue, as today;
  - for named unique constraints, persists a catch-up boundary (never a refusal marker) at the next sequence number unless a lower one is already set. If the job is lost, a later activation catches the cleared scopes up (item 8) instead of trusting a watermark that leads the index.
- **On resume.** The rebuilding mark is kept in memory only. A job step resumed after a restart can already be past its clears while a new activation's fold or catch-up writes the same constraint. The step therefore calls `ConstraintIndexRebuildStarting` again when it resumes, before it does any further write.
- **On every terminal outcome** (success, failure, cancellation), the step calls `IEventSequence.ConstraintIndexRebuilt(constraintNames, processedThrough)`. `processedThrough` is the last sequence number it processed, or `First - 1` if it processed none. Failure paths include `ReindexConstraintsStep.cs:181-200`. The step retries the call before it completes.
  - **If the job processed events,** the grain makes the named constraints pending from `processedThrough + 1` and repairs in that turn (items 3 and 4). The job's unique writes are in log order, so every key the job reached reflects the log through `processedThrough`. A scope the job had not reached was not cleared. The fold restores the rest, including claims and closures a live append wrote and the job then overwrote. That is the race in `ConstraintIndexes.cs:111-113` and `ClosesStreamConstraintReindexer.cs:21-22`.
  - **If the job processed nothing,** it cleared nothing, and the grain folds nothing.
  - **Afterwards,** the grain clears the boundary it set at the start, unless a catch-up was already pending before the job started.
- **Closing commits.** The closing reindexer's commit writes differences against its snapshot. It closes scopes that are new or changed and reopens stale ones, instead of `RemoveAllFor` followed by `Close` (`:90-101`). A failure partway through the commit leaves each scope in its old or its rebuilt state, never removed and not reinstated.
- **Remaining window.** The scope-clear window in 0005 item 5 remains while a job runs.

### 8. Existing stores: background catch-up of unique constraints

**On the first activation with a `null` watermark:**

- The watermark is set to the next sequence number. That is `First` for an empty sequence.
- If the sequence has events and a unique constraint applies, the catch-up boundary is set to the same number. Otherwise it stays `null`.
- Both are persisted at once.

**Closing constraints are not caught up.** Their coverage starts at the watermark set on upgrade, which is today's guarantee and keeps registration non-retroactive. Closures lost before the upgrade still need a reindex that names the constraint (item 10). Until that command ships:

- a missed close can be repaired by closing the scope by hand (`CompleteStream`), or by appending the closing event again;
- a missed reopen can be repaired by appending one of its `ReopenedBy` events (`Documentation/closing-streams/inspecting-and-repairing.mdx`).

**Process.**

- **Fold.** The catch-up folds `[First, boundary)` for the unique constraints that apply, except those a reindex job is rebuilding (item 7). It folds in timer turns of at most 2 seconds and 1,000 constraint-relevant events (item 4), using the segmented fold from item 3, and collects every segment's differences before it writes.
- **Excluded keys.** The grain keeps the set of keys that any event at or after the boundary touches. The set is filled from the log, whether or not the event's update succeeded.
  - **Per-value full releases.** A full release in per-value mode (`Remove`) carries no values, because the updater releases the source without naming them (`UniqueConstraintIndexUpdater.cs:41`). It therefore excludes the source as a whole: the constraint, the scope key and the event source, with every value under it. The catch-up never writes a key of an excluded source.
  - **When an event is appended,** its keys are added to the set.
  - **After activation,** the set is rebuilt by reading `[boundary, tail]` for keys only. That read runs in timer turns, never in `OnActivateAsync`, and no catch-up write happens before it completes.
- **When writes happen.** The catch-up writes only when three things hold:
  - every segment's differences are collected;
  - the excluded set is complete;
  - the constraint is not pending in steady state, so every excluded key's live updates have been applied.

  It skips every excluded key, and checks the set at each write. Between raises (below), the set only grows. A claim refused by an excluded holder is settled, because the holder's state is the live one.
- **Write groups.** Appends are admitted during the catch-up, so a value must never be free between turns while the log gives it to another source. A value's release and the claim that takes it therefore form one **write group**, written in the same turn, release first.
  - **Order of groups.** Groups are written in an order where a source's own release comes before its new claim. In per-event-source mode, a `Save` that replaces the source's old value would otherwise free that value early.
  - **Groups of one.** A release that no claim takes, or a claim of a value nobody holds, forms a group of one.
  - **No group is split.** A turn starts groups only within its first 2 seconds and finishes the group it started. A group holds at most 2 writes, so a write turn ends within 4 seconds (item 4).
  - **A failed group.** If a write in a started group fails, or is abandoned, after its release was written, the constraint's governed appends are refused with `ConstraintIndexUnavailable` until that group's claim is written. Catch-up group writes are attempted once, without the item 2 retries, so a write turn ends within 4 s; a failed write is retried in a later turn under refusal.
  - **The marker.** Appends are serialized, so none sees a turn's writes half done. The turns can end, or the silo can stop, between a group's release and its claim. A write turn therefore sets `ConstraintIndexCatchUpRefusing` for the constraints it writes, persisted before its first write. It clears the marker once every group the turn started has its claim written. A failed group leaves the marker set, and so do the three cases below, until their last write.
  - **On activation,** every constraint named by the marker is refused for its governed appends until the catch-up's writes for it complete. The activation reads the marker with the rest of the state, so a restart during a write under refusal cannot admit appends while a released value is still free. The catch-up then refolds from `First` and writes the missing claims. A marker with no catch-up boundary is stale and is cleared.
- **Writing under refusal.** Three cases cannot be written as pairs in one turn:
  - a group of more than 2 writes, such as a value with two holders from past damage;
  - sources that swap values with each other, which no order of groups can serve;
  - the many-differences case of item 3.

  These are written in the two phases of item 3, and the constraint's governed appends are refused with `ConstraintIndexUnavailable` from the first write until the last write and the last retried claim, and the marker names the constraint for that whole span.
- **Raising the boundary.** The excluded set holds at most 100,000 hashes of keys or sources (32 bytes each). When it reaches the cap, the grain raises the boundary to the next sequence number, persists it and empties the set.
  - **Writes are suspended** until the fold reaches the new boundary for every segment whose differences are not yet written. The current segment's fold continues to the new boundary. Segments already collected but not written are folded again to it, because their differences were taken at the old boundary and keys touched in `[old, new)` are no longer excluded.
  - **Keys already written** need no refold, because every later event for them was applied live.
  - **Why it is safe.** Once writes resume, keys touched before the new boundary are folded to it, and keys touched after it are excluded again.
- **Steady state continues.** Events at or after the boundary follow items 2 to 4 throughout, including refusal while a unique constraint is pending.
- **Restart.** A restart rereads the boundary, rebuilds the excluded set and starts the fold again from `First`. Keys already written show no difference, so a restart costs reads only.
- **Concurrency.** At most 4 sequences per silo catch up at a time, through a silo-wide limit. A timer turn that cannot take a slot skips. A sequence holds its slot from the start of a write under refusal until its last write; only folding turns compete for a slot.
- **Memory.** Per sequence, the staged map with the collected differences and the excluded set stay within about 30 MB. Per silo, catch-ups stay within about 120 MB.
- **Completion.** When every difference is written, the boundary is cleared and persisted.

**What is exposed during the catch-up.**

- Appends are admitted and validated against the index as it stands.
- For a key that no event at or after the boundary touches, that is its pre-upgrade entry until the catch-up writes it. The entry can be missing or stale, which is today's exposure.
- The catch-up only ever moves such a key from its pre-upgrade entry to its final state in the log. It never writes a key that a later event touches, so no key regresses, and nothing the live path wrote is overwritten.
- An admitted append never finds a value free between its release and the claim that takes it. Both are written in one turn. If a write fails, is abandoned or is cut off by a restart after the release, the constraint's governed appends are refused, from the persisted marker, until the claim is written.
- When a turn's budget expires before the boundary, the read position, the staged map and the collected differences stay in memory, and nothing is written until every segment is collected. Stopping before a write turn's first write therefore exposes nothing beyond what was exposed before the catch-up started. Stopping after it leaves the marker set, so the governed appends stay refused until the catch-up's writes complete.

**Deviation from the ruled direction.** The ruling says replays happen "before taking new appends". This applies only to this catch-up, for two reasons:

- **A blocking fold from `First` is an outage on large logs.** It reads every constraint-relevant event and writes every difference, sequentially, while a caller waits at most the 30-second response timeout.
- **A reindex job is worse for the duration.**
  - It clears each scope at its first event (`ReindexConstraintsStep.cs:69-72`). Every store, damaged or not, would lose protection for every value until the rebuild reaches it.
  - Closing rebuilds are not safe while appends are in flight (`ClosesStreamConstraintReindexer.cs:21-22`).

  The precedent of upgrade-wide reindexes (`Patches/15.38.5/RebuildConstraintIndexes.cs:58`, `Patches/16.4.1/RebuildConstraintIndexesForDecryptedValues.cs:64`) accepted that window. This catch-up never clears, and it only adds missing repairs.

**Cost.** The catch-up reads the constraint-relevant history once per segment, and again for the unwritten segments when the boundary is raised. It writes only differences, so a mostly correct index costs few writes.

**What it does not fix.**

- Phantom claims and duplicate holders from past damage (item 10).
- Closing closures lost before the upgrade (above).

### 9. Durable appends are never reported as failed (same release)

This ships in the same release as the watermark, as its own PR. Three reasons:

- The watermark is what makes "success despite failed post-commit work" safe, because the index catches up from the log.
- Without this change, the clearest double-append trigger (`EventSequence.cs:685`, `:703`) stays. The repair would then faithfully index the duplicate, because same-source reclaims are allowed.
- Both changes touch the same three call sites (`EventSequence.cs:714`, `:899`; `EventSequence.Validation.cs:178`).

**The commit point** is a successful storage result. A batch is one transaction (Context: MongoDB `EventSequenceStorage.cs:346-350` and `:428`, SQL `:357`). After it:

- nothing turns the result into a failure;
- post-commit steps log their exceptions;
- index failures follow item 2;
- a failed enqueue spills to catch-up, as today (`EventSequence.cs:958-965`).

**Incomplete acknowledgment.** `InvalidAppendAcknowledgment` is removed (`InvalidAppendAcknowledgment.cs:11-12`). An incomplete acknowledgment triggers a read-back.

**Storage exceptions.** Each provider reports a failure as **not committed** only when it can prove that nothing was committed:

- the exception was thrown before the write was issued;
- a duplicate key, which is already `DuplicateEventSequenceNumber`;
- on MongoDB, a transaction error labelled `TransientTransactionError` without the `UnknownTransactionCommitResult` label.

Every other exception leaves the outcome unknown.

**Read-back.** For an unknown outcome or an incomplete acknowledgment, the grain reads the attempted slots with `GetRange` (`IEventSequenceStorage.cs:319`).

- **Read concern.** MongoDB reads with majority read concern, so a write that a failover later rolls back does not count. SQL reads committed data.
- **Matching.** A slot matches when its correlation id, event type and event source are the submitted ones, and its **stored** content hash equals the submitted hash. The grain compares the stored hash field. It never recomputes the hash from read content. A slot whose stored hash is `EventHash.NotSet` never matches.

| Storage outcome | Read-back | Reported to the client |
| --- | --- | --- |
| Not committed | none | Failure, as today; the sequence number is unchanged |
| Unknown, or incomplete acknowledgment | every slot matches | Success, completed through the normal post-commit path |
| Unknown, or incomplete acknowledgment | anything else: empty, partial, other events, an unset hash, or the read fails | `AppendError.AppendOutcomeUnknown`; every constraint becomes pending from the first attempted slot, and the next append re-reads the tail |

An empty read-back after an unknown commit is not proof of absence, because a later commit can still land the events. It is reported as unknown, never as failure.

**Client contract for `AppendOutcomeUnknown`.** The batch may or may not be durable. Do not retry it blindly, because a blind retry can store it twice.

- First read the event source's events (`GetForEventSourceIdAndEventTypes`, or `GetFromSequenceNumber` filtered to the event source; `Clients/DotNET/EventSequences/IEventSequence.cs:129`, `:167`) and look for the attempt's correlation id in their context.
- Append again only if the events are absent.
- If 0007 lands, retry with the same key instead, which is deduplicated.

### 10. Operator-triggered reindex

- **In the patch (no contract).** The procedure has three steps:
  1. stop the kernel on every silo;
  2. clear the watermark, the catch-up boundary and the catch-up refusal marker (unset the fields in MongoDB, set the columns to `NULL` in SQL);
  3. start the kernel.

  Stopping first matters, because an active grain would write its in-memory state over the cleared value (item 1). The sequence then catches up its unique constraints as in item 8. This does not rebuild closures, does not remove phantom claims, and does not clear the residuals of items 1 to 3.
- **In the following minor.** A Core `[Command] ReindexConstraints(EventStore, Namespace, EventSequenceId, ConstraintNames)`, built like `ReplayObserver.cs:18-32`, which also generates a gRPC operation and a Workbench action.
  - It starts `IReindexConstraints` for every named or applicable unique and closing definition.
  - It hands off as in item 7.
  - This is the only way to rebuild closures from the log, remove phantom claims, clear the residuals, and restore "first claimant in log order".

### 11. Release

- **PRs 1-5 ship as a patch.**
  - The watermark, the boundary, the refusal marker (kernel-internal storage that no client reads), the `IUpdateConstraintIndex` signature, `IUniqueConstraintsStorage.GetFor` and the two grain methods all live in kernel projects, which are not packaged (`Directory.Build.props:9-10`).
  - No Core artifact is added, so no wire contract changes (`Core.csproj:47-53`).
  - The two new well-known errors travel in the existing string `Errors` field. The .NET client already models append errors as strings (`Clients/DotNET/EventSequences/AppendError.cs:10`).
  - SQL namespace migrations have shipped in patches before (`Projections/Migrations/v19_37_8.cs`), as did 0005's schema change.
- **PR 6 is a minor**, because it adds a public command and a wire operation.

### 12. Cost

| Situation | Events folded (constraint-relevant types only) | Governed appends |
| --- | --- | --- |
| Update failed in a running activation | from the failed event to the tail, normally that one append | one append turn per 5 s waits up to 5 s for the repair; the others are refused at once until the fold and its writes finish |
| Activation after a clean deactivation | none; state written at `EventSequence.cs:169` | - |
| Activation after a crash | from the persisted watermark: the appends since the last state write (at most 1,000 by default) plus any range that was pending when the state was written, which can be much larger after a long outage | as above, refused for about segments × range read time plus the writes |
| Reindex handoff | from the job's last processed event to the tail | as above |
| First activation after the upgrade | unique-constraint history, once per segment (again for unwritten segments when the boundary is raised), in the background | proceed (item 8), except while a group is written under refusal |

- **Expected refusal.** For a large pending range, governed appends are refused for about the number of segments times the time to read the range once, plus the time to write the differences, stretched by about half when no governed appends arrive. Every segment rereads the range (item 4). A range that fits one segment and reads in 20 seconds means about 30 seconds of refusal.
- **Reads** are already batched by the cursor.
- **Writes** are one per differing key. All releases come first, then the claims in deciding order; in the catch-up, each release is written with the claim that takes its value. A fold over a range that the live path mostly applied writes little.
- **No bulk write API.** A bulk write would have to reproduce 0005's per-claim settlement in every provider.

## Options considered

- **Write claims atomically with the event.** This is possible only on MongoDB. SQL claims live in a separate DbContext, and InMemory has no shared transaction. Not taken, as the issue concluded.
- **Replay events one by one into the live index** (this record's first draft). A replay that stops partway leaves keys at older states while appends are admitted. A source that moved from v to w is put back on v, so w is unprotected and v is wrongly refused. Not taken in favor of the staged fold (item 3).
- **Write the staged keys in the order of their deciding events** (this record's second draft). A claim decided before the release it depends on is refused while the old holder still holds the value. For example, X moves v to c at 5, Y claims v at 6 and X moves c to a at 9: Y's claim goes first and is refused, then X's move frees v, which is left unprotected. The same happens when a release and its claimant fall in different segments. Not taken in favor of two phases (item 3).
- **Persist the staged map.** It would survive restarts, but it needs a new store in every provider. A restart of the catch-up only costs reads. Not taken.
- **Track keys written live only in memory.** The set is lost on restart, and the catch-up would then overwrite live writes. Not taken; the excluded set is rebuilt from the log (item 8).
- **Catch closing constraints up from `First`.** It would close scopes for historical events of constraints registered after them, and closures by event cannot be reopened by hand. Not taken (item 8).
- **Watermark in constraint storage.** There is no single store to hold it, so it cannot be atomic. It would also need a new interface and table in every provider. Its one advantage is that dropping the constraint collections would drop the watermark with them; the reindex covers that case. Not taken.
- **A persisted watermark per constraint.** It would mean N writes per append. Definition changes are already per constraint through the job, and the ruling sets one watermark per sequence. Pending state per constraint is kept in memory instead. Not taken.
- **Sequence-ordered overwrites in the stores.** These close path 4 only for claims. Removals would need tombstones in every provider, and missed writes would still not be repaired. Not taken.
- **Fail the append when the update fails.** The event is already durable, so this causes double appends and contradicts 0005. Not taken.
- **Fail open when the repair fails** (today's behavior). It silently accepts duplicates. Not taken.
- **Refuse every append to the sequence** while a constraint is pending. It blocks events that no pending index can decide wrongly. Not taken in favor of item 4.
- **Fail closed for good on an event that always fails.** One malformed event would refuse every append on a sequence with closures, with no way out short of a reindex. Not taken in favor of the poison rule (item 4).
- **Existing stores: blocking fold from `First`, a forced reindex, or trusting the current index.** The first is an outage, and the second clears every scope (item 8). The third does not repair, so it breaks the requirement that existing stores repair themselves. Not taken in favor of the background catch-up.
- **File the double-append fix separately as high priority.** Rejected (item 9).
- **Expose retry, budget and cap numbers as configuration.** Configuration is public surface, which would make this a minor. They are kept as internal constants (#4906).

## Default if unanswered

Nothing ships. `CompleteDurableAppend` keeps logging and dropping failed updates, and a crash between the commit and the update keeps losing them. Users keep seeing:

- duplicate values accepted;
- released values refused for good;
- events entering closed streams;
- reopened streams staying closed.

Publication retries keep applying stale mutations. A failed reindex keeps leaving cleared scopes behind. Durable batches can still be reported as failed and duplicated by client retries. If 0007 lands, its keyed appends keep their acknowledged constraint gap.

## Timeline and scope

Six pull requests, in order. PRs 1-5 ship together in the next patch, and PR 6 in the next minor. Each new-behavior spec is red before its change.

1. **Persist the watermark, the boundary and the refusal marker in every provider (patch).**
   - Changes:
     - add `EventSequenceState.ConstraintIndexWatermark`, `ConstraintIndexCatchUpBelow` and `ConstraintIndexCatchUpRefusing`;
     - MongoDB fields;
     - SQL columns and migration;
     - real InMemory persistence;
     - `EventSequencesStorageProvider` carries the values through unchanged.
   - Specs:
     - Storage.InMemory.Specs, Storage.MongoDB.Specs and Storage.Sql.Specs (SQLite and PostgreSQL fixtures), under `for_EventSequenceStorage/`: `when_saving_state/and_a_constraint_index_watermark_is_set` and `and_a_catch_up_boundary_is_set` and `and_a_catch_up_refusal_is_set` (all round-trip), and `when_getting_state/and_the_state_predates_the_watermark` (`null`, not `First`);
     - Storage.MongoDB.Specs `when_getting_state/and_the_document_carries_an_unknown_field` (the registered conventions ignore it, which is what an older kernel meets after a downgrade);
     - Storage.Sql.Specs `for_NamespaceDbContext/when_migrating/and_the_event_sequences_table_has_rows` (the columns are added as nullable, and rows keep `NULL`);
     - Core.Specs `for_EventSequencesStorageProvider/when_reading_state/and_a_watermark_is_persisted`.
2. **The staged fold (patch, no change on the append path).**
   - Changes:
     - add `ConstraintIndexReplay` with the staged fold, the segmented memory cap, the collected differences, and two-phase writes: every release first, across all segments, then the claims in deciding order;
     - add `IUniqueConstraintsStorage.GetFor` in every provider;
     - thread a `CancellationToken` through `IUpdateConstraintIndex.Update` and the constraint storage calls;
     - per-updater outcomes;
     - `ReindexConstraintsStep` uses the shared release and erased-subject logic.
   - Core.Specs `Events/Constraints/for_ConstraintIndexReplay/when_folding/`:
     - one spec per missed operation: `and_a_claim_was_missed`, `and_a_removal_was_missed`, `and_a_move_was_missed`, `and_a_close_was_missed`, `and_a_reopen_was_missed`;
     - **`and_a_source_moved_later_in_the_range`** (only the final value is written; the earlier value is never written);
     - **`and_the_budget_expires_before_the_end_point`** (nothing is written, and the index keeps every newer entry);
     - `and_a_value_moves_between_sources` (released before it is claimed, so the store settles nothing);
     - **`and_the_releasing_source_moved_again_later_in_the_range`** (X moves v to c, Y claims v, X moves c to a: X's release is written before Y's claim, so Y holds v and X holds a);
     - **`and_a_release_and_its_claimant_are_in_different_segments`** (every segment's release is written before any claim, and the claimant holds the value);
     - `and_a_claim_is_refused_by_a_holder_the_fold_changes` (it is retried once all releases are written, and succeeds);
     - `and_the_differences_exceed_half_the_cap` (they are written before the remaining segments are folded; a refused claim is retried after the remaining releases);
     - `and_a_per_value_source_is_released_in_full` (staged stored values are released);
     - `and_the_range_overlaps_applied_updates` (no write at all);
     - `and_the_keys_exceed_the_cap` (segmented passes write the same result as one pass);
     - `and_an_event_is_redacted` (unique: the outcome comes from the range's other events for the key; closing with a context scope: applied; property scope without a stored closure: settled with error, warning);
     - `and_an_event_is_revised`;
     - `and_the_subject_was_erased`;
     - `and_content_is_encrypted` (the plaintext hash equals the append-time hash);
     - `and_the_event_has_an_older_generation` (it is read and folded);
     - `and_no_constraint_is_folded` (no read at all);
     - `and_a_closing_event_precedes_the_coverage_start` (not applied);
     - outcome classification: `and_the_store_reports_a_duplicate_claim` (settled, because the holder is not a key the fold changes; the refused source has no entry), `and_a_closing_scope_cannot_be_resolved` (settled with error), `and_the_store_throws` (nothing further written for that constraint).
   - Also:
     - Core.Specs `for_ConstraintValidationContext/when_updating/and_one_updater_fails` (all outcomes reported) and `and_the_attempt_times_out` (Failed);
     - Storage specs `for_UniqueConstraintsStorage/when_getting_for_a_source/` in every provider, for both modes;
     - the existing `for_ReindexConstraintsStep` specs stay green.
3. **Watermark, retries, steady-state repair, refusal, poison events, publication reconciliation and catch-up (patch).** Core.Specs `EventSequences/for_EventSequence/`:
   - Failed updates, `when_a_constraint_index_update_fails/`:
     - `and_a_retry_succeeds`;
     - `and_every_retry_fails` (the append succeeds, and the constraint is pending);
     - `and_the_store_reports_a_duplicate_claim` (the watermark advances);
     - `and_it_fails_in_the_middle_of_a_batch`;
     - **`and_the_store_hangs_for_a_batch_of_1000_events`** (the append returns within the 2-second post-commit budget, and the rest is deferred);
     - **`and_the_same_event_fails_deterministically`** (after 5 attempts it is settled with error, logged and counted, and governed appends are admitted again);
     - `and_the_failure_is_transient` (it stays pending past 5 attempts);
     - `and_an_event_in_the_range_cannot_be_read` (the read fault is classified for that event for every folded constraint; after 5 attempts it is settled with error, and the cursor resumes after it);
     - `and_an_attempt_is_abandoned_and_lands_late` (the next timer turn rewrites the key).
   - The next append, `when_appending_while_a_constraint_is_pending/`:
     - `and_the_repair_succeeds` (folded before validation, so a duplicate of the missed claim is refused);
     - **`and_the_repair_stops_at_the_budget`** (the governed append is refused, nothing is written, and no key moved backwards);
     - `and_the_budget_expires_during_the_writes` (the written keys stand, the rest are written in the next turn, and the constraint stays pending until then);
     - **`and_many_governed_appends_are_queued_behind_an_unfinished_fold`** (only the first append turn runs the repair; the other governed appends are refused without store work, and a queued non-governed append returns within the budget);
     - `and_appends_arrived_after_the_fold_started` (the fold writes the state at its fixed end point, then folds on from there);
     - `and_a_removal_for_the_pending_constraint_is_appended` (admitted; its update joins the fold);
     - `and_the_event_is_governed_by_another_constraint` (admitted and validated against that constraint's current index);
     - `and_another_constraint_was_applied_live` (the fold does not write it);
     - `and_closures_are_in_use_and_a_closing_update_failed` (every append is refused);
     - `and_the_backoff_has_not_elapsed` (refused without a store call);
     - `and_the_last_append_turn_repair_ended_less_than_5_seconds_ago` (refused without a store call, whatever that repair's outcome).
   - Activation, `when_activating/`:
     - `and_the_watermark_is_behind_the_tail` (only that range is folded, before the first governed append; a read is served meanwhile);
     - `and_the_watermark_is_at_the_tail`;
     - with `and_no_watermark_is_persisted/`:
       - `and_the_sequence_has_events` (watermark at the next number, boundary set, appends proceed);
       - **`and_a_closing_constraint_has_historical_closing_events`** (no closure is written for any event before the upgrade);
       - **`and_a_source_moved_after_the_boundary`** (the catch-up does not write that key, and an append validates against the live value);
       - **`and_a_per_value_source_is_fully_released_after_the_boundary`** (the whole source is excluded, and none of its released values is written back);
       - **`and_a_value_moved_between_sources_before_the_boundary`** (the release and the claim are written in one turn, so an append between turns never finds the value free);
       - `and_two_sources_swapped_values_before_the_boundary` (written under refusal; governed appends are refused until the last write);
       - **`and_a_group_claim_fails_after_its_release`** (governed appends are refused until the claim is written);
       - **`and_the_silo_restarted_during_a_write_under_refusal`** (governed appends are still refused after activation, until the catch-up's writes complete);
       - `and_the_excluded_set_is_still_being_rebuilt` (no catch-up write happens until it completes);
       - `and_the_catch_up_was_interrupted` (the fold restarts, and keys already written are not written again);
       - `and_the_excluded_set_reaches_the_cap` (the boundary is raised and persisted, and no unwritten difference is written before its segment is folded to the new boundary);
       - `and_the_sequence_is_empty`.
   - Without traffic: **`when_the_store_recovers/and_no_append_arrives`** (the timer, kept alive, repairs and persists the watermark).
   - Sequence-number gaps: `when_storage_renumbers_past_another_activations_event` (the gap is folded).
   - Publication retries:
     - **`when_appending_a_publication/and_the_receipt_is_below_the_watermark`** (no claim is mutated);
     - `and_the_receipt_is_above_the_watermark_after_a_later_removal` (the removal stands).
   - Deactivation: `when_deactivating/and_a_constraint_is_pending` (the persisted watermark stays below the failure).
   - Injected failures. Real InMemory constraint storage sits behind a decorator that fails writes after the commit; the sequence is then deactivated and activated again:
     - **`when_the_constraint_store_fails_after_commit/for_a_unique_constraint/and_the_sequence_is_reactivated`** (the claim is present, and a duplicate is refused);
     - **`.../for_a_closing_constraint/and_the_sequence_is_reactivated`** (the stream is closed, and the next event is refused);
     - `.../for_a_reopening_event/and_the_sequence_is_reactivated`.
   - Changed spec: `when_appending_a_publication/and_the_publication_is_already_durable_but_the_grain_never_completed_it` loses `should_update_the_constraint_index_with_the_durable_slot`.
   - Integration, `Integration/Kernel/Events/Constraints/for_ConstraintIndexRepair/`, on the MongoDB and SQL runs including SQL Server. Events are written directly to storage, as `when_reindexing_closing_constraints.cs` does, to simulate a kill between commit and update:
     - `when_an_event_was_stored_without_its_claim`;
     - `when_a_removal_was_stored_without_its_release`;
     - `when_an_event_was_stored_without_its_closure`;
     - `when_the_kernel_restarts_with_a_persisted_watermark`;
     - `when_a_publication_is_retried_after_a_later_removal`;
     - `when_a_store_without_a_watermark_is_upgraded`;
     - **`when_a_store_with_historical_closing_events_is_upgraded`** (no new closures).
   - Documentation:
     - `constraints/index.md:51`;
     - `closing-streams/index.mdx:101-102`;
     - `closing-streams/inspecting-and-repairing.mdx`, which gains the interim procedure from item 10, the manual repairs for closures lost before the upgrade (item 8), and the two client contracts (items 4 and 9).
4. **Durable appends are never reported as failed (patch).** Core.Specs:
   - **`when_appending_many/and_storage_returns_an_incomplete_acknowledgment`**, rewritten from `:19-20`: it reports success with every number, reads the batch back and indexes each event;
   - `when_appending_many/and_the_acknowledgment_is_incomplete_and_the_read_back_fails` (`AppendOutcomeUnknown`, every constraint pending);
   - **`when_appending_many/and_the_commit_result_is_unknown_and_the_slots_are_empty`** (`AppendOutcomeUnknown`, never Failure);
   - `when_appending_many/and_storage_throws_after_the_batch_is_durable` (success);
   - single-event cases under `when_appending_an_event/`:
     - `and_storage_throws_after_the_event_is_durable` (success);
     - `and_storage_reports_it_did_not_commit` (failure, number unchanged);
     - `and_storage_throws_and_the_slot_holds_another_event` (`AppendOutcomeUnknown`);
     - `and_the_stored_hash_is_not_set` (`AppendOutcomeUnknown`);
     - `and_storage_throws_and_the_probe_fails` (`AppendOutcomeUnknown`);
     - `and_a_post_commit_step_throws` (success).
   - Storage specs proving the throw is real and the classification holds:
     - Storage.MongoDB.Specs `for_EventSequenceStorage/when_appending/and_resolving_identities_fails_after_the_insert` (it throws, and the event is durable), and the same spec in Storage.Sql.Specs;
     - Storage.MongoDB.Specs `when_appending_many/and_the_commit_result_is_unknown` (not reported as not committed) and `and_a_transient_transaction_error_aborts` (reported as not committed);
     - Storage.MongoDB.Specs `when_reading_back/should_use_majority_read_concern`.
5. **Reindex handoff (patch).**
   - Core.Specs:
     - `for_ReindexConstraintsStep/when_performing/and_it_completes` (notifies before the first clear and reports the last processed number);
     - **`for_ReindexConstraintsStep/when_performing/and_it_fails_after_clearing`** (it still reports the last processed number);
     - `for_ReindexConstraintsStep/when_performing/and_it_is_cancelled`;
     - `for_ReindexConstraintsStep/when_resuming/and_it_had_already_cleared` (it calls `ConstraintIndexRebuildStarting` again before any further write);
     - `for_EventSequence/when_a_constraint_index_is_rebuilt/and_events_were_appended_during_the_rebuild` (the newer claim is restored);
     - `.../and_the_rebuild_failed_after_clearing` (the cleared scope is restored from the log);
     - `.../and_the_rebuild_processed_nothing` (nothing is folded);
     - `for_EventSequence/when_a_constraint_index_rebuild_starts/and_the_job_is_lost` (the persisted boundary makes the next activation catch up);
     - `for_ClosesStreamConstraintReindexer/when_committing/and_the_store_fails_partway` (every scope is old or rebuilt, none removed).
   - Integration: `for_ClosedStreamsConstraintStorage/when_reindexing_closing_constraints_with_appends_in_flight` (a live closure survives).
   - Documentation: `closing-streams/closing-events.mdx:63`.
6. **Operator-triggered reindex (minor).**
   - Changes:
     - the `ReindexConstraints` command;
     - the Workbench action;
     - documentation.
   - Specs:
     - Core.Specs `for_ReindexConstraints/when_handling/and_no_names_are_given` (all applicable unique and closing definitions);
     - `and_a_name_does_not_apply_to_the_sequence` (refused);
     - the `wire-compatibility.yml` gates;
     - Integration `when_an_operator_reindexes_a_sequence_with_a_phantom_claim` (the phantom is removed) and `when_an_operator_reindexes_a_closing_constraint` (closures lost before the upgrade are rebuilt).

**In scope:**

- the event sequence append and post-commit path;
- the three providers' sequence state;
- the staged fold and the new constraint storage read;
- the reindex handoff and the closing reindexer's commit;
- the documentation listed above.

**Out of scope:**

- `Revise` moving claims;
- a shadow-table reindex (0005);
- a sequence-level alert condition;
- catching up closing constraints from history;
- `IUniqueEventTypesConstraintsStorage`, which derives from the log and keeps no index.

## Resolutions

L0 resolved the open questions on 2026-10-14, on behalf of woksin:

- **Alert condition.** A sequence-level alert needs a wire change, because alert targets are observer-shaped. It is not part of this series; it is tracked in Cratis/Chronicle#4904.
- **Revision below the watermark.** `Revise` moving claims and closures gets its own small design, tracked in Cratis/Chronicle#4905.
- **Constants.** The defaults in this record stand. Making the budgets and caps configurable is low priority for a later minor, tracked in Cratis/Chronicle#4906.

## Verification

**Done when**, in every provider, for both unique and closing constraints:

- **After an injected post-commit failure**, a kernel restart between commit and update, or a publication retry after a later change or removal, the index ends up as a reindex of the log would make it for the keys involved:
  - a duplicate of a missed claim is refused;
  - a missed release frees its value;
  - a missed close refuses the next event;
  - a missed reopen admits it.
- **No regression.** No fold, steady-state or catch-up, ever writes a state older than the key's current entry, including when it stops at a budget.
- **Releases before claims.** A value that moved between sources ends with its final holder after any fold, including when the releasing source moved again later in the range and when the release and the claim fall in different segments. A claim is settled as refused only when its holder is not a key the fold changes.
- **Refusal.** While a constraint is pending, the appends it validates are refused with `ConstraintIndexUnavailable` and nothing is written. Other appends are admitted.
- **Budgets.** An append against a hanging constraint store returns within the stated budgets. With many governed appends queued behind an unfinished fold, only one append turn per 5 seconds runs the repair, and a queued non-governed append still returns within the budgets.
- **Poison events.** An event that fails deterministically is settled with error after 5 attempts, logged and counted.
- **Durable appends.** A batch storage acknowledged incompletely, or whose commit threw after it was durable, is reported as a success with one copy of each event. An unknown commit with an empty read-back is reported as `AppendOutcomeUnknown`, never as failure.
- **Publication retries.** A retry below the watermark writes no claim.
- **Reindex.** A reindex that fails after clearing leaves the index as the log makes it once the handoff has folded.
- **Existing stores.**
  - A store with no watermark catches up its unique constraints in the background, restarts after an interruption, and persists its watermark.
  - It writes no closure for history.
  - It never overwrites a key that an event after the boundary touched, and it writes back no value of a per-value source fully released after the boundary.
  - An append admitted during the catch-up never finds a value free between its release and the claim that takes it. If a group's write fails or the silo restarts after the release, the constraint's governed appends are refused until the claim is written.
  - After the boundary is raised, no difference taken at the old boundary is written.
- **Unrepairable index.** While the index cannot be repaired:
  - an error and the metrics are emitted;
  - the sequence heals when the store returns, with no append needed.

## Consequences

- **The index follows the log.** It converges after any post-commit failure, crash, gap, retry or failed reindex, without passing through older states. Publication retries never mutate claims from retry content. If 0007 lands, its keyed appends inherit this.
- **Cost on activation.** Activation after a crash folds from the persisted watermark: normally up to one state interval of constraint-relevant events, more if a range was pending. The state snapshot becomes correctness-relevant in one direction only: a lost write costs folding, never correctness.
- **The first activation after the upgrade** adds background load, bounded per silo. During catch-up:
  - pre-upgrade damage stays visible to validation until the catch-up writes the key, as today, but no key moves backwards;
  - phantom claims and duplicate holders from past damage remain until an operator reindex (PR 6), and the fold keeps the current holder rather than the first claimant in log order;
  - closing closures lost before the upgrade stay lost until repaired by hand or by a reindex that names the constraint.
- **New refusals.** A constraint store that rejects writes now refuses governed appends instead of silently accepting them. On a sequence with closures in use, that means every append, for at most the poison threshold if a single event is at fault.
- **How long refusals last.** Once the store works again, a large pending range keeps governed appends refused for about the number of segments times the time to read the range once, plus the writes, and about half again without governed traffic (item 4). A range that fits one segment and reads in 20 seconds means about 30 seconds of refusal. After a long outage followed by a crash, the range and with it the refusal can be much longer.
- **Refusal during the catch-up.** Value swaps, values with two holders from past damage, and more than 50,000 differing keys are written under refusal, so the constraint's governed appends are refused while they are written (item 8). A failed write in a catch-up group, or a restart in the middle of one, keeps them refused until the claim is written, and after a restart until the catch-up's writes complete.
- **Closing coverage start in memory.** A closing definition registered while the grain is inactive can still be applied to a lagging range's earlier events on activation after a crash (item 3). A closure written that way is permanent, because a scope closed by an event cannot be reopened by hand (`EventSequence.cs:561-564`). Persisting the coverage start with the definition would close this, and is not done here.
- **Residuals.** Writes from duplicate activations and late abandoned attempts can still leave a stale entry that only a reindex clears.
- **Wire, schema and kernel types.**
  - Two new well-known append errors, each with a client contract.
  - Three nullable SQL columns and three MongoDB fields. Downgrades read past them, which the PR 1 specs prove.
  - `InvalidAppendAcknowledgment` is removed from the kernel. `IUpdateConstraintIndex.Update` takes a cancellation token, and `IUniqueConstraintsStorage` gains `GetFor`.
- **`DuplicateUniqueConstraintValue`** stays diagnostic and counts as settled for the watermark.
- **Reindex.** A reindex no longer loses live claims or closures to the race, and a failed reindex no longer leaves cleared scopes behind. Its scope-clear window remains while it runs.

## Release note

Paste this into the pull request description. PR release notes use `##` headings, so they are kept in a fence here to leave the record's own outline intact.

```markdown
## Fixed

- Unique constraints and closing streams no longer drift out of step with the event log when a constraint update fails or the kernel stops right after an append, and a stored append is no longer reported as failed. Existing event stores repair their unique constraints in the background after the upgrade; closing-stream closures lost before the upgrade are not rebuilt. (#4895)

## Changed

- An append can now be refused with `ConstraintIndexUnavailable` while a constraint index it depends on cannot be written. Nothing is stored, and the same append can be retried later. An append can also return `AppendOutcomeUnknown` when the store cannot confirm whether it was stored; check for the events before appending them again. (#4895)
```
