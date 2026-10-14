---
id: 0008-repair-constraint-indexes-from-the-log
title: Keep constraint indexes in step with the event log through a per-sequence watermark
status: proposed
stage: none
class: contract
reversibility: costly
proposed: 2026-10-14
decider: woksin
applies-to:
  - Source/Kernel/Core/EventSequences/**
  - Source/Kernel/Core/Events/Constraints/**
  - Source/Kernel/Storage/EventSequences/**
  - Source/Kernel/Storage/Events/Constraints/**
  - Source/Kernel/Storage.InMemory/EventSequences/**
  - Source/Kernel/Storage.MongoDB/EventSequences/**
  - Source/Kernel/Storage.Sql/EventStores/Namespaces/EventSequences/**
  - Integration/Kernel/Events/Constraints/**
  - Documentation/constraints/**
  - Documentation/closing-streams/**
---

<!-- markdownlint-disable-next-line MD025 -->
# Keep constraint indexes in step with the event log through a per-sequence watermark

Proposed on 2026-10-14 for Cratis/Chronicle#4895; woksin is the intended decider, pending acceptance. This extends [Decision 0005](0005-settle-unique-constraint-claims-in-every-store.md) and closes the gap [Decision 0007](0007-idempotent-keyed-appends.md) defers to #4895.

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
  - **Validators.** The factory builds one validator per definition that applies to the sequence, plus a closed-stream check that applies to every event (`Core/Events/Constraints/ConstraintValidationFactory.cs:26-43`).
  - **Updaters.** The context takes one updater from each validator that keeps an index (`ConstraintValidationContext.cs:46`). `Update` runs them with `Task.WhenAll` (`:125-128`), so when two fail, only the first exception surfaces.
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

    The closing reindexer handles redacted events through their `originalEventType` (`ClosesStreamConstraintReindexer.cs:54-81`).
- **Store semantics: the last write for a key wins.** Each index entry has a key, and the last write to that key replaces it.
  - MongoDB per-event-source claims replace the source's entry (`Storage.MongoDB/Events/Constraints/UniqueConstraintsStorage.cs:147-162`). A held value raises `DuplicateUniqueConstraintValue` and the old claim stays.
  - InMemory does the same under one lock (`Storage.InMemory/Events/Constraints/UniqueConstraintsStorage.cs:62-91`).
  - Closures are upserted or deleted by owner and scope (`Storage.MongoDB/Events/Constraints/ClosedStreamsConstraintStorage.cs:57-69`).
  - No store checks sequence order.
- **Same-source claims.** `IsAllowed` lets a source claim a value it already holds (`Storage.MongoDB/.../UniqueConstraintsStorage.cs:62`; InMemory `:43-50`).

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
   - **Commit errors.** A MongoDB batch commit error is rethrown after an abort attempt (`EventSequenceStorage.cs:428`, `:440-444`).
   - **Effect.** The client is told that a durable batch failed. Because the same source may reclaim its own value (above), a retry is admitted and appended a second time.
   - **Not a post-commit path.** The issue also cites `:1037`. That catch belongs to `GetValidAndCompliantEvent`, which runs before the commit, so it is not a post-commit path.
3. **An ungraceful kill** between the commit (`:685/714` batch, `:884-899` single) and the update.
4. **Publication retries apply stale mutations.** `ReconcileExistingPublication` builds a context from the retry's content (`EventSequence.Validation.cs:169-178`) and writes claims at the receipt's number without regard to later events.

### Why nothing repairs it

- **Activation** rebuilds only the sequence number and the per-type tails (`EventSequencesStorageProvider.cs:33-44`).
- **`RebuildStaleIndexes`** starts a reindex only when definitions change, and only if unique definitions exist (`ConstraintIndexes.cs:42-45`, `:76-90`). Closing constraints are never reindexed automatically (`ClosesStreamIndexUpdater.cs:16-20`).
- **The documentation** states the gap (`Documentation/constraints/index.md:51`, `Documentation/closing-streams/index.mdx:101-102`).

### Facts the design rests on

- **Non-reentrancy.** `IEventSequence` (`IEventSequence.cs:22`) is non-reentrant; only reads are `[AlwaysInterleave]` (`:61, 72, 83, 97`). Every append, update and repair is therefore serialized.
- **The state snapshot.**
  - `EventSequenceState` holds the next sequence number and the per-type tails (`Storage/EventSequences/EventSequenceState.cs:11-22`).
  - It is written through `SaveState`:
    - MongoDB `EventSequenceStorage.cs:74-86`;
    - SQL `:81-87`;
    - InMemory, where `SaveState` is a no-op (`Storage.InMemory/EventSequences/EventSequenceStorage.cs:101`) and `GetState` returns only the next sequence number (`:87-98`).
  - The snapshot is written on the first append after activation, then every `StatePersistenceInterval` appends (default 1,000, `Core/Configuration/Events.cs:45`), and on deactivation (`EventSequence.cs:165-174`, `:1223-1236`).
  - InMemory caches the event-sequence storage and the constraint storages per sequence for the life of the process (`Storage.InMemory/EventStoreNamespaceStorage.cs:115-135`).
- **Packaging.** Kernel projects are not packaged, except `Contracts` (`Source/Kernel/Directory.Build.props:9-10`). Wire contracts are generated from Core Arc artifacts (`Core/Core.csproj:47-53`), for example `ReplayObserver` (`Core/Observation/ReplayObserver.cs:18-32`).

This crosses every provider and changes what an append reports after its commit. It adds a refusal and a persisted field, so it needs a durable decision.

## Decision

Each event sequence keeps a durable **constraint watermark**. The kernel retries a failed index update within the turn. When updates cannot be confirmed, it replays the stored events from the watermark to the tail, in log order, through the same updaters, before any append that depends on them. Index writes driven by the grain only ever happen at or above the watermark. An append whose events are durable is never reported as failed.

### 1. The watermark

- **Definition.** `EventSequenceState.ConstraintIndexWatermark` (`EventSequenceNumber?`) is exclusive: every event below it has had every index update succeed or settle (item 2).
  - It is the ruled "highest sequence number whose updates all succeeded", plus one.
  - It is stored the same way as `EventSequenceState.SequenceNumber` (`EventSequenceState.cs:13-16`), so an empty sequence is `First` and needs no sentinel.
  - `null` means it was never recorded (item 8).
- **Advancing.** The in-memory watermark advances only contiguously. It moves from `n` to `n + 1` only after every updater for event `n` has succeeded or settled in this activation, either live or by replay.
  - Any jump in the sequence number leaves a gap that must be replayed. Jumps come from:
    - duplicate renumbering (`EventSequence.cs:1159`, `:1178-1200`);
    - a publication receipt (`EventSequence.Validation.cs:162-165`);
    - a keyed `AlreadyAppended` hit (0007, section 3).
  - Events another activation wrote are therefore always replayed, never skipped.
- **Persistence.** The watermark is persisted inside the state snapshot. Two changes make that work:
  - in `CompleteDurableAppend`, the constraint updates move ahead of the snapshot write, so the snapshot carries the watermark they produced;
  - the state is also written after every completed repair.
- **Write order.** The watermark is written **after** the claims it covers and is never advanced ahead of an awaited write. A persisted watermark can lag, but never lead. A lag only costs replay (item 3), because replay is idempotent.
- **Providers.**
  - **MongoDB.** A `constraintIndexWatermark` field on the existing state document. `SaveState` adds a `$set`, and `GetState` maps a missing field to `null`.
  - **SQL.** A nullable `ConstraintIndexWatermark` column on `EventSequences`, added by a namespace migration `NS-EventSequences-v<release>`. The column is `decimal(20,0)`, or `TEXT` on SQLite, following `ClosedStreams/Migrations/v19_38_0.cs:33-40`. The add-column precedent is `Observers/Migrations/v19_15_0.cs:17-20`.
  - **InMemory.** The value is kept in `EventSequenceStorage` under its lock, and `SaveState` stops being a no-op for it. Its lifetime is that of the cached constraint storages.
- **Why grain state and not constraint storage.**
  - The watermark covers two stores (claims and closures). Claims are spread over per-constraint collections and tables, and on SQL they sit in their own DbContext. No single constraint store could hold the watermark atomically with what it covers, and "after" is sufficient.
  - The state document or row already exists in every provider and is already read on activation (`EventSequencesStorageProvider.cs:40`).

### 2. Updating within the turn

`ConstraintValidationContext.Update` returns one outcome per updater instead of using `WhenAll` and surfacing the first exception. The grain classifies each outcome:

| Outcome | Examples | Retried | Watermark |
| --- | --- | --- | --- |
| Applied | write succeeded | - | advances |
| Settled | `DuplicateUniqueConstraintValue`: the store decided, as 0005 rules (logged as today) | no | advances |
| Settled with error | `InvalidClosingStreamScope`, `RedactedClosingEventTypeUnavailable`: can never succeed (logged as an error, counted) | no | advances |
| Failed | any other exception | yes | held |

- **Retry.** A failed updater is retried up to three times within the turn (100 ms and 400 ms pauses). Retrying an updater is idempotent.
- **After the last attempt:**
  - the append is still reported as successful;
  - the watermark stays at the failed event;
  - the constraint is marked **unrepaired**;
  - later live updates for that constraint are deferred to the repair, so they are not applied out of order;
  - live updates for other constraints continue. They are re-applied, idempotently, when the repair replays them.

### 3. Ordered repair from the log

**`ConstraintIndexReplay`** (`Core/Events/Constraints`) takes over the stored-event logic of `ReindexConstraintsStep` and is shared with it:

- **Reading.** It reads `[from, tail]` through `GetFromSequenceNumber` (`Storage/EventSequences/IEventSequenceStorage.cs:307`), filtered to the event types the sequence's index-keeping constraints read: claim, removal, closing and reopening types. Redactions are added to the filter when a closing constraint applies.
  - If no such constraint applies, nothing is read.
  - An empty filter would mean "do not narrow", so it is never passed.
- **Content.** It releases PII before hashing, as the reindex does (`ReindexConstraintsStep.cs:211-222`), and skips erased subjects (`:83-93`).
- **Updates.** For each event, in order, it builds the context with the current definitions and runs the updaters. Outcomes are classified as in item 2.

**When the repair runs.** A repair runs from the watermark to the tail:

- on activation;
- after any failed update;
- after a sequence-number gap;
- after a reindex handoff (item 7).

It always runs before the next append's validation.

**How it is scheduled.**

- It runs inside the append's turn, right after `RefreshConstraintsIfChanged` (`EventSequence.cs:353`, `EventSequence.Validation.cs:58`).
- It also runs from a non-interleaving grain timer:
  - the timer is registered at activation and while a range is unrepaired;
  - it uses `Interleave = false`, unlike the pattern-capture timer at `EventSequence.cs:154`;
  - it keeps the activation alive while a range is unrepaired.
- Appends therefore wait for it, while reads keep interleaving.
- `OnActivateAsync` itself does not replay. A failing store must not fail activation and, with it, every read.

**Convergence.** Every updater operation sets or deletes the entry for one key, using only the event: per-event-source source, per-value value, or closure owner and scope (`UniqueConstraintIndexUpdater.cs:28-71`, `ClosesStreamIndexUpdater.cs:31-38`).

- **Why the end state is right.** Every event below the watermark has been applied. Applying the events from the watermark on, in order, therefore leaves each key touched in that range as the range's last event for that key dictates.
- **Replaying a range twice** after a lagging watermark gives the same final state. During the overlap, a settled store may refuse an older claim because a later holder is already present. That refusal is counted as settled, and the later events decide the key.
- **Two exceptions:**
  - phantom claims with no log basis, such as rows left by a reindex race (0005);
  - duplicate holders from past damage, where replay keeps the current holder rather than the first claimant in log order.

  Both are cleared only by a reindex (item 10).
- **Closure times.** Replayed closures take `ClosedAt` from the event's `Occurred`, as the reindexer does (`ClosesStreamConstraintReindexer.cs:80`).

### 4. If the repair keeps failing

The sequence fails closed, but only for appends whose validation reads an index that the unrepaired range can have changed. The append is refused with the new well-known error `AppendError.ConstraintIndexUnavailable` (retryable; same style as `Core/EventSequences/AppendError.cs:30`), and nothing is written. Such an append is one of these:

- an event that a unique constraint validates (`UniqueConstraintValidator.cs:22-23`), where that constraint's claim or removal types occur in the range;
- any event when closures are in use (`ClosedStreamConstraintValidator.cs:28`) and a closing or reopening type occurs in the range;
- if the range cannot be read at all, every event a unique constraint validates, and every event when closures are in use.

A batch is refused as a whole. Other appends proceed. They cannot be decided wrongly, and their updates follow the rules in item 2.

**Bounds.**

- No append waits for more than one repair budget, which is 5 seconds of repair per turn.
- After a failed attempt, attempts back off from 1 second, doubling to 60 seconds. Between attempts, appends that would be refused are refused at once.
- The timer keeps retrying, so the sequence heals without any traffic.

**Signals.**

- An error-level log on each failed attempt, carrying the sequence, the watermark, the tail and the first failing event.
- Counters for update failures, repairs and refused appends.
- The lag (tail minus watermark) as a measurement.

A first-class alert condition is not in this decision. Alert targets are observer-shaped (`Core/Alerts/AlertTarget.cs:23-28`; `ObserverId` is wire field 3 in `Contracts/Alerts/AlertTarget.cs:32-33`), so it needs a contract change (open question).

The status quo already fails these appends when the store is down, because validation reads it. The refusal mainly adds safety for the case where writes fail while reads succeed, and for the time between recovery and repair.

### 5. Publication reconciliation

`ReconcileExistingPublication` no longer builds a constraint context from the retry's content (removing `EventSequence.Validation.cs:169-178`). It still advances the sequence number and the tails, and still enqueues the event.

- If the receipt is below the watermark, no constraint write happens.
- If it is at or above the watermark, the repair replays from the watermark using the **stored** events. A later change or removal that is already in the log therefore stands.

### 6. Revision, redaction and changed definitions

- **Revision.** `Revise` rewrites content in place and does not touch the index (`EventSequence.cs:416-450`).
  - Below the watermark, nothing changes. This is today's behavior and out of scope here (open question).
  - Within a replayed range, the revised content is used, as a reindex would use it.
- **Redaction.** Redaction replaces the event with `EventRedacted` (`ClosesStreamConstraintReindexer.cs:54`).
  - Replaying a redacted unique event neither claims nor releases (`UniqueConstraintIndexUpdater.cs:46-49`). A claim written live keeps holding its value, and a missed claim is not reinstated, which matches the reindex.
  - A redacted closing or reopening event is replayed with its original type when its scope comes from the event context.
  - When the scope comes from a property, the replay leaves the rows untouched and logs a warning, which is the reindexer's rule (`ClosesStreamConstraintReindexer.cs:60-75`).
  - Manual closures are never touched, because updaters change only their own owner (`EventSequence.cs:540`).
- **Changed definitions.**
  - Replays use the definitions current at the time of replay.
  - A definition change still starts the reindex job for the affected sequences (`ConstraintIndexes.cs:76-90`). That job is the one writer allowed below the watermark, because it rebuilds a scope from the log in order.
  - The watermark is not reset by a definition change. The job's handoff (item 7) covers the race.

### 7. Reindex job handoff

When `ReindexConstraintsStep` finishes, it calls the new grain method `IEventSequence.ConstraintIndexRebuilt(rebuiltThrough)`, passing the last sequence number it processed. This is a kernel grain method, not a wire call.

- The grain lowers its watermark to `min(watermark, rebuiltThrough + 1)` and repairs in that turn.
- This restores claims and closures that a live append wrote and the job then overwrote. That is the race described in `ConstraintIndexes.cs:111-113` and `ClosesStreamConstraintReindexer.cs:21-22`.
- For closing constraints, the race shrinks to the job's commit window (`:90-101`).
- The scope-clear window in 0005 item 5 remains.

### 8. Existing stores: background catch-up from 0

**Starting value.** On the first activation with a `null` watermark:

- if the sequence has events and an index-keeping constraint applies, the watermark starts at `First`;
- if the sequence is empty, it starts at `First` with no replay;
- if no index-keeping constraint applies, it starts at the tail.

**Process.**

- The replay from `First` runs in chunks of up to 1,000 constraint-relevant events per timer turn.
- The watermark is persisted after every chunk, so a restart resumes where it stopped.
- **Appends are admitted between chunks** and validated against the index as it stands, which is what they are validated against today.
- A live failure during catch-up follows items 2 and 4.
- When the catch-up reaches the tail, the steady state takes over.

**Deviation from the ruled direction.** The ruling says replays happen "before taking new appends". This applies only to this one-time catch-up, for two reasons:

- **A blocking replay from 0 is an outage on large logs.** It costs one round trip per update per constraint-relevant event, sequentially. A million such events means minutes or more, while a caller waits at most the 30-second response timeout (`Core/Configuration/Clustering.cs:86`).
- **A reindex job is worse for the duration.**
  - It clears each scope at its first event (`ReindexConstraintsStep.cs:69-72`). Every store, damaged or not, would lose protection for every value until the rebuild reaches it.
  - Closing rebuilds are not safe while appends are in flight (`ClosesStreamConstraintReindexer.cs:21-22`).

  The precedent of upgrade-wide reindexes (`Patches/15.38.5/RebuildConstraintIndexes.cs:58`, `Patches/16.4.1/RebuildConstraintIndexesForDecryptedValues.cs:64`) accepted that window. This catch-up never clears.

**Cost.** The catch-up does the same total work as a full replay. It is spread out, filtered to constraint-relevant types, and resumable.

**What it does not fix.**

- A source that changes its value during the catch-up may see its older claim temporarily displace the newer one, until the catch-up reaches the newer event.
- Phantom claims are not removed (item 10).

### 9. Durable appends are never reported as failed (same release)

This ships in the same release as the watermark, as its own PR. Three reasons:

- The watermark is what makes "success despite failed post-commit work" safe, because the index catches up from the log.
- Without this change, the clearest double-append trigger (`EventSequence.cs:685`, `:703`) stays. The repair would then faithfully index the duplicate, because same-source reclaims are allowed.
- Both changes touch the same three call sites (`EventSequence.cs:714`, `:899`; `EventSequence.Validation.cs:178`).

The rules:

- **The commit point** is a successful storage result, and a batch is one transaction (0007, context). After it:
  - nothing turns the result into a failure;
  - post-commit steps log their exceptions;
  - index failures follow item 2;
  - a failed enqueue spills to catch-up, as today (`EventSequence.cs:958-965`).
- **Incomplete acknowledgment.** `InvalidAppendAcknowledgment` is removed (`InvalidAppendAcknowledgment.cs:11-12`). An incomplete acknowledgment triggers a read-back.
- **Read-back after a storage exception.** When storage throws (except `DuplicateEventSequenceNumber`), or acknowledges incompletely, the grain reads the attempted slots with `GetRange` (`IEventSequenceStorage.cs:319`). Each slot is matched on correlation id, event type, event source and content hash. The result is one of:

  | Read-back result | Reported to the client |
  | --- | --- |
  | All slots hold the submitted events | Success, completed through the normal post-commit path |
  | No slot does | Failure, as today; the sequence number is unchanged |
  | Partial, or the read fails | `AppendError.AppendOutcomeUnknown`; the range is marked unrepaired, and the next append re-reads the tail |

### 10. Operator-triggered reindex

- **In the patch (no contract).** The documented procedure is to clear the watermark (unset the field in MongoDB, set the column to `NULL` in SQL) and restart the sequence. It then catches up from 0 as in item 8.
- **In the following minor.** A Core `[Command] ReindexConstraints(EventStore, Namespace, EventSequenceId, ConstraintNames)`, built like `ReplayObserver.cs:18-32`, which also generates a gRPC operation and a Workbench action.
  - It starts `IReindexConstraints` for every named or applicable unique and closing definition.
  - It hands off as in item 7.
  - This is the only way to remove phantom claims and restore "first claimant in log order".

### 11. Release

- **PRs 1-5 ship as a patch.**
  - The watermark lives in kernel storage, which is not packaged (`Directory.Build.props:9-10`).
  - No Core artifact is added, so no wire contract changes (`Core.csproj:47-53`).
  - The two new well-known errors travel in the existing string `Errors` field. The .NET client already models them as strings (`Clients/DotNET/EventSequences/AppendError.cs:10`).
  - SQL namespace migrations have shipped in patches before (`Projections/Migrations/v19_37_8.cs`), as did 0005's schema change.
- **PR 6 is a minor**, because it adds a public command and a wire operation.

### 12. Cost

| Situation | Events replayed (constraint-relevant types only) | Appends |
| --- | --- | --- |
| Update failed in a running activation | from the failed event to the tail, normally that one append | wait |
| Activation after a clean deactivation | none; state written at `EventSequence.cs:169` | - |
| Activation after a crash | at most the appends since the last state write, at most 1,000 by default | wait (budget) |
| Reindex handoff | from the job's end to the tail | wait |
| First activation after the upgrade | whole history, in chunks | proceed |

- **Reads** are already batched by the cursor.
- **Writes** stay at one per updater per event, in log order; the updaters of one event may run in parallel.
- **No bulk write API.** A bulk write would have to reproduce 0005's per-claim settlement in every provider, and every steady-state range is small.

## Options considered

- **Write claims atomically with the event.** This is possible only on MongoDB. SQL claims live in a separate DbContext, and InMemory has no shared transaction. Not taken, as the issue concluded.
- **Watermark in constraint storage.** There is no single store to hold it, so it cannot be atomic. It would also need a new interface and table in every provider. Its one advantage is that dropping the constraint collections would drop the watermark with them; the reindex covers that case. Not taken.
- **A watermark per constraint.** It would mean N writes per append. Definition changes are already per constraint through the job, and the ruling sets one watermark per sequence. Not taken.
- **Sequence-ordered overwrites in the stores.** These close path 4 only for claims. Removals would need tombstones in every provider, and missed writes would still not be repaired. Not taken.
- **Fail the append when the update fails.** The event is already durable, so this causes double appends and contradicts 0005. Not taken.
- **Fail open when the repair fails** (today's behavior). It silently accepts duplicates. Not taken.
- **Refuse every append to the sequence** when the repair fails. It blocks events that no index can decide wrongly. Not taken in favor of item 4.
- **Existing stores: blocking replay from 0, a forced reindex, or trusting the current index.** The first is an outage and the second clears every scope (item 8). The third does not repair, so it breaks the requirement that existing stores repair themselves. Not taken in favor of the background catch-up.
- **File the double-append fix separately as high priority.** Rejected (item 9).
- **Expose retry and budget numbers as configuration.** Configuration is public surface, which would make this a minor. They are kept as internal constants (open question).

## Default if unanswered

Nothing ships. `CompleteDurableAppend` keeps logging and dropping failed updates, and a crash between the commit and the update keeps losing them. Users keep seeing:

- duplicate values accepted;
- released values refused for good;
- events entering closed streams;
- reopened streams staying closed.

Publication retries keep applying stale mutations. Durable batches can still be reported as failed and duplicated by client retries. 0007's keyed appends keep their acknowledged constraint gap.

## Timeline and scope

Six pull requests, in order. PRs 1-5 ship together in the next patch, and PR 6 in the next minor. Each new-behavior spec is red before its change.

1. **Persist the watermark in every provider (patch).**
   - Changes:
     - add `EventSequenceState.ConstraintIndexWatermark`;
     - MongoDB field;
     - SQL column and migration;
     - real InMemory persistence;
     - `EventSequencesStorageProvider` carries the value through unchanged.
   - Specs:
     - Storage.InMemory.Specs, Storage.MongoDB.Specs and Storage.Sql.Specs (SQLite and PostgreSQL fixtures), under `for_EventSequenceStorage/`: `when_saving_state/and_a_constraint_index_watermark_is_set` (round-trips) and `when_getting_state/and_the_state_predates_the_watermark` (`null`, not `First`);
     - Storage.MongoDB.Specs `when_getting_state/and_the_document_carries_an_unknown_field` (the registered conventions ignore it, which is what an older kernel meets after a downgrade);
     - Storage.Sql.Specs `for_NamespaceDbContext/when_migrating/and_the_event_sequences_table_has_rows` (the column is added as nullable, and rows keep `NULL`);
     - Core.Specs `for_EventSequencesStorageProvider/when_reading_state/and_a_watermark_is_persisted`.
2. **Replay from stored events (patch, no change on the append path).**
   - Changes:
     - add `ConstraintIndexReplay`;
     - per-updater outcomes;
     - `ReindexConstraintsStep` uses the shared release and erased-subject logic.
   - Core.Specs `Events/Constraints/for_ConstraintIndexReplay/when_replaying/`:
     - one spec per missed operation: `and_a_claim_was_missed`, `and_a_removal_was_missed`, `and_a_move_was_missed`, `and_a_close_was_missed`, `and_a_reopen_was_missed`;
     - `and_the_range_overlaps_applied_updates` (the final index is unchanged);
     - `and_an_event_is_redacted` (unique: no change; closing with a context scope: applied; property scope: rows untouched, warning);
     - `and_an_event_is_revised`;
     - `and_the_subject_was_erased`;
     - `and_content_is_encrypted` (the plaintext hash equals the append-time hash);
     - `and_no_index_keeping_constraint_applies` (no read at all);
     - outcome classification: `and_the_store_reports_a_duplicate_claim` (settled), `and_a_closing_scope_cannot_be_resolved` (settled with error), `and_the_store_throws` (stops at that event).
   - Also: Core.Specs `for_ConstraintValidationContext/when_updating/and_one_updater_fails` (all outcomes reported). The existing `for_ReindexConstraintsStep` specs stay green.
3. **Watermark, retry, repair before appends, fail closed, publication reconciliation and catch-up (patch).** Core.Specs `EventSequences/for_EventSequence/`:
   - Failed updates, `when_a_constraint_index_update_fails/`:
     - `and_a_retry_succeeds`;
     - `and_every_retry_fails` (the append succeeds, and the watermark is held);
     - `and_the_store_reports_a_duplicate_claim` (the watermark advances);
     - `and_it_fails_in_the_middle_of_a_batch`.
   - The next append, `when_appending_after_a_failed_constraint_index_update/`:
     - `and_the_repair_succeeds` (replayed before validation, so a duplicate of the missed claim is refused);
     - with `and_the_repair_fails/`: `and_the_event_is_governed_by_the_unrepaired_constraint` (`ConstraintIndexUnavailable`, nothing written), `and_the_event_is_governed_by_another_constraint` (appended), `and_closures_are_in_use_and_a_closing_update_failed` (every append refused) and `and_the_backoff_has_not_elapsed`.
   - Activation, `when_activating/`:
     - `and_the_watermark_is_behind_the_tail` (only that range is replayed, before the first append; a read is served meanwhile);
     - `and_the_watermark_is_at_the_tail`;
     - with `and_no_watermark_is_persisted/`: `and_the_sequence_has_events` (chunked, persisted per chunk, appends proceed between chunks), `and_the_catch_up_was_interrupted` (resumes) and `and_the_sequence_is_empty`.
   - Sequence-number gaps: `when_storage_renumbers_past_another_activations_event` (the gap is replayed).
   - Publication retries:
     - **`when_appending_a_publication/and_the_receipt_is_below_the_watermark`** (no claim is mutated);
     - `and_the_receipt_is_above_the_watermark_after_a_later_removal` (the removal stands).
   - Deactivation: `when_deactivating/and_a_range_is_unrepaired` (the persisted watermark stays below the failure).
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
     - `when_a_store_without_a_watermark_is_upgraded`.
   - Documentation:
     - `constraints/index.md:51`;
     - `closing-streams/index.mdx:101-102`;
     - `closing-streams/inspecting-and-repairing.mdx`, which gains the interim procedure from item 10.
4. **Durable appends are never reported as failed (patch).** Core.Specs:
   - **`when_appending_many/and_storage_returns_an_incomplete_acknowledgment`**, rewritten from `:19-20`: it reports success with every number, reads the batch back and indexes each event;
   - `when_appending_many/and_the_acknowledgment_is_incomplete_and_the_read_back_fails` (`AppendOutcomeUnknown`, range unrepaired);
   - single-event cases under `when_appending_an_event/`: `and_storage_throws_after_the_event_is_durable` (success), `and_storage_throws_and_the_slot_is_empty` (failure, number unchanged), `and_storage_throws_and_the_slot_holds_another_event`, `and_storage_throws_and_the_probe_fails` (`AppendOutcomeUnknown`), `and_a_post_commit_step_throws` (success);
   - `when_appending_many/and_storage_throws_after_the_batch_is_durable` (success).
   - Storage specs proving the throw is real:
     - Storage.MongoDB.Specs `for_EventSequenceStorage/when_appending/and_resolving_identities_fails_after_the_insert` (it throws, and the event is durable);
     - the same spec in Storage.Sql.Specs.
5. **Reindex handoff (patch).**
   - Core.Specs:
     - `for_ReindexConstraintsStep/when_performing/and_it_completes` (reports the last processed number);
     - `for_EventSequence/when_a_constraint_index_is_rebuilt/and_events_were_appended_during_the_rebuild` (the newer claim is restored);
     - `.../and_the_watermark_is_below_the_rebuild` (unchanged).
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
     - Integration `when_an_operator_reindexes_a_sequence_with_a_phantom_claim` (the phantom is removed).

**In scope:**

- the event sequence append and post-commit path;
- the three providers' sequence state;
- the constraint replay;
- the reindex handoff;
- the documentation listed above.

**Out of scope:**

- `Revise` moving claims;
- a shadow-table reindex (0005);
- a sequence-level alert condition;
- `IUniqueEventTypesConstraintsStorage`, which derives from the log and keeps no index.

## Verification

**Done when**, in every provider, for both unique and closing constraints:

- **After an injected post-commit failure**, a kernel restart between commit and update, or a publication retry after a later change or removal, the index ends up as a reindex of the log would make it for the keys involved:
  - a duplicate of a missed claim is refused;
  - a missed release frees its value;
  - a missed close refuses the next event;
  - a missed reopen admits it.
- **Incomplete acknowledgment.** A batch storage acknowledged incompletely is reported as a success, with one copy of each event.
- **Publication retries.** A retry below the watermark writes no claim.
- **Existing stores.** A store with no watermark catches up on first activation, resumes after a restart, and persists its watermark.
- **Unrepairable index.** While the index cannot be repaired:
  - governed appends are refused with `ConstraintIndexUnavailable` and nothing is written;
  - an error and the metrics are emitted;
  - the sequence heals when the store returns, with no append needed.

## Consequences

- **The index follows the log.** It converges after any post-commit failure, crash, gap or retry. Publication retries never mutate claims from retry content. 0007's keyed appends inherit this.
- **Cost on activation.** Activation after a crash replays up to one state interval of constraint-relevant events. The state snapshot becomes correctness-relevant in one direction only: a lost write costs replay, never correctness.
- **The first activation after the upgrade** adds background load. During catch-up:
  - a source that changes its value can briefly lose its newer claim;
  - phantom claims and duplicate holders from past damage remain until an operator reindex (PR 6), and replay keeps the current holder rather than the first claimant in log order.
- **New refusals.** A constraint store that rejects writes now refuses governed appends instead of silently accepting them. On a sequence with closures in use, that means every append.
- **Wire, schema and kernel types.**
  - Two new well-known append errors.
  - One nullable SQL column and one MongoDB field. Downgrades read past both, which the PR 1 specs prove.
  - `InvalidAppendAcknowledgment` is removed from the kernel.
- **`DuplicateUniqueConstraintValue`** stays diagnostic and counts as settled for the watermark.
- **Reindex.** A reindex no longer loses live claims or closures to the race. Its scope-clear window remains.

## Release note

- Fixed unique constraints and closing streams drifting out of step with the event log when the kernel failed, or was stopped, right after an append. A failed or interrupted constraint update could let a duplicate value through, keep a released value refused for good, let events into a closed stream, or keep a reopened stream closed. The kernel now retries the update. Before it accepts further appends that depend on it, it replays from the event log any events whose updates did not complete. An append that was stored is no longer reported as failed, so retrying it no longer stores it twice. Existing event stores repair themselves: on the first activation after the upgrade, each event sequence replays its constraint-relevant history in the background.
