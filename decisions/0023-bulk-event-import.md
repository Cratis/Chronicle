---
id: 0023-bulk-event-import
title: Import large volumes of events into an event sequence through a resumable, chunked import session that stages and validates every event before committing any, commits in ordered chunks through a job without locking, and hands observers one catch-up
status: accepted
stage: none
class: contract
reversibility: costly
proposed: 2026-11-03
decided: 2026-11-08
decider: Einar
applies-to:
  - Source/Kernel/Core/EventSequences/**
  - Source/Kernel/Core/Sequences/**
  - Source/Kernel/Core/Imports/**
  - Source/Kernel/Core/Observation/**
  - Source/Kernel/Storage/EventSequences/**
  - Source/Kernel/Storage/Imports/**
  - Source/Kernel/Storage.MongoDB/EventSequences/**
  - Source/Kernel/Storage.Sql/EventStores/Namespaces/EventSequences/**
  - Source/Kernel/Storage.InMemory/EventSequences/**
  - Source/Kernel/Contracts/**
  - Source/Kernel/Protobuf/**
  - Source/Clients/DotNET/EventSequences/**
  - Source/Clients/DotNET/Imports/**
  - Documentation/events/**
---

<!-- markdownlint-disable-next-line MD025 -->
# Import large volumes of events into an event sequence through a resumable, chunked import session that stages and validates every event before committing any, commits in ordered chunks through a job without locking, and hands observers one catch-up

## Context

People need to bring existing history into Chronicle:

- migrating from another event store or a CRUD system
- moving an event store between environments
- loading fixtures for load tests

The volumes are 300 000 events and more. Today there is no import, bulk or streaming append API.

### What exists today

- **The append APIs are unary RPCs into one grain call.**
  - `IEventSequence.AppendMany` and its overloads (`Source/Clients/DotNET/EventSequences/IEventSequence.cs:305-637`) build a single `AppendManyRequest` and never chunk it (`EventSequence.cs:1275-1290`).
  - The kernel `AppendManyForEventSourcesValidator` sets no maximum count.
- **One `AppendMany` is all-or-nothing in memory on the single-threaded event sequence grain** (`EventSequence.BatchValidation.cs:72-139`). For each event it awaits:
  - schema validation
  - PII encryption
  - constraint checks
  - migration to every generation, plus hashing
- **Storage commits a batch in one transaction.**
  - MongoDB uses `StartTransaction` and one `InsertManyAsync` (`Storage.MongoDB/EventSequences/EventSequenceStorage.Batch.cs:48,127`).
  - SQL uses one `SaveChangesAsync` (`Storage.Sql/.../EventSequenceStorage.Batch.cs:118`).
- **Fan-out after the commit pushes the whole batch through live delivery.** It goes into `IAppendedEventsQueues` (`EventSequence.cs:970-993`), which spills lagging observers into catch-up jobs.
- **Limits a 300 000-event `AppendMany` hits:**
  - 100 MB gRPC messages (`ChronicleOptions.cs`)
  - the 30 s Orleans response timeout (`Configuration/Clustering.cs:128`)
  - MongoDB's 16 MB transaction and 60 s transaction lifetime
  - holding the event sequence grain, and so every other appender to that sequence, for the whole call
- **Seeding loops `AppendMany` in chunks of 100** (`Core/Seeding/EventSeeding.cs:39,202`). It skips and logs a rejected chunk, so its result is a partial import with no report.
- **Append contracts are generated from Arc `[Command]` records in `Source/Kernel/Core/Sequences`.** The generator emits unary commands, plus observable queries for server-to-client streams. A client-streaming RPC would be the first hand-written append contract, against the project rule that the wire is derived from Core.

## Decision

Chronicle gets an **import session**. An import is a durable object with an id. Events are added to it in chunks, every event is validated before any is committed, and the commit then runs as a job.

### 1. Lifecycle

`Open` → `Staging` → `Validating` → `Committing` → `Completed`, or `Failed` or `Abandoned`.

- **`BeginImport`** records the target event sequence and the options. It returns an `ImportId` and the server limits (maximum chunk events and bytes).
- **`AddImportChunk(ImportId, ChunkIndex, events)`** stages one chunk. It is idempotent on `(ImportId, ChunkIndex)`, so a client can resend after a network failure. The chunks are stored in import staging storage, not the event log.
  - Staging does the per-event work that does not depend on the order of the log: resolve the event type and generation, validate the schema, and resolve the event source.
  - It runs on a stateless worker, outside the event sequence grain, so it parallelizes.
  - Per-event rejections come back on the chunk's result, with the client's line or index.
- **`CompleteImport(ImportId, expectedChunks, expectedEvents)`** closes staging and fails if the counts differ.
  - It then validates constraints over the whole import in staging order, against the persisted indexes plus the import's own claims. This is the batch rule `ConstraintBatchClaims` already applies, extended to the import.
  - If anything is rejected and `OnInvalid = Reject` (the default), the import fails and **nothing is committed**.
  - Otherwise it starts the commit job.
- **The commit job** (`ImportEvents`, a `Cratis.Orleans.Jobs` job) commits staged events in staging order. It calls a new event sequence grain method, `AppendImportChunk`, with chunks of `Imports.CommitChunkSize` (proposed 1 000 events).
  - Each chunk follows the existing path: PII encryption, migration, hashing, numbering, one storage transaction, and constraint index updates.
  - Nothing is locked. Constraints are checked again on every chunk, because an ordinary writer can claim a unique value between `CompleteImport` and the chunk's commit. An event that loses such a race is not committed and is reported as a rejection on the `ImportResult`. The rest of the import continues, since the committed chunks are never rolled back.
  - The grain is released between chunks, so ordinary appends interleave with the import instead of waiting behind it.
  - The job checkpoints the last committed chunk and the sequence numbers it was given. A kernel restart resumes from the checkpoint. Each committed chunk records its `(ImportId, ChunkIndex)` on the stored events, which makes "was this chunk committed" answerable from the log after a crash. The commit therefore never double-appends and never rolls back.
- **`AbandonImport`** discards staging before the commit starts. An import left open past `Imports.StagingRetention` (proposed 24 h) is abandoned automatically.
- **`ObserveImport(ImportId)`** is an observable query reporting state, counts, rejections and committed sequence-number ranges. The Workbench and clients use it for progress.

### 2. What an imported event keeps

An imported event keeps everything the source says:

- `Occurred`
- correlation id
- causation chain
- caused-by identity
- `Subject`
- tags
- event source type, stream type and stream id
- the generation it was written in

The event gets a **new sequence number** in the target sequence. Original sequence numbers are carried as an optional `sourceSequenceNumber` in the event's causation (`Causation` type `Import`, with properties `importId` and `sourceSequenceNumber`). Lineage stays queryable without a second numbering scheme. The causation chain also always records the import as its root.

### 3. Duplicates

Each event may carry an `IdempotencyKey`, typically the source system's event id. The commit uses the keyed append path of [decision 0012](0012-keyed-appends-travel-on-their-own-rpcs.md), so re-running the same import against the same sequence reports `AlreadyAppended` for those events instead of duplicating them. Events without a key are not deduplicated.

### 4. Imported events reach every observer once, through the existing mechanics

Imported events are ordinary events. Every reactor, reducer and projection sees each of them exactly once, the same way it sees any other appended event. There is no import-specific observation flag, no opt-out, and nothing for `[OnceOnly]` to do, because the events are delivered as catch-up, not as a replay.

`AppendImportChunk` does not push the events through the live queue, `IAppendedEventsQueues`, one chunk at a time:

- When the commit job completes, each subscribed observer of the sequence whose event types intersect the import catches up once, with the existing `CatchUpObserver` job.
- With [decision 0022](0022-observers-receive-batches-on-every-path.md), that catch-up delivers pages, so an observer in batch mode receives the import in large batches.

Live delivery of ordinary appends that interleave with the import continues as normal. The catch-up starts from each observer's own `NextEventSequenceNumber`, so nothing is skipped.

### 5. Reading the source is the client's business

The kernel takes events, not files. Where the events come from (a file in any format, another database, an HTTP feed) is up to the client, which turns the source into an `IAsyncEnumerable` of events. The kernel never opens a path or URL a caller names.

### 6. Streaming over chunked calls

The .NET client streams: it reads the caller's `IAsyncEnumerable` lazily, cuts it into chunks within the server's limits, and keeps a bounded number of `AddImportChunk` calls in flight (proposed 4). On the wire each chunk is its own call, acknowledged by its `ChunkIndex`. A dropped connection therefore loses nothing, and the import continues from the last acknowledged chunk.

### 7. .NET client API

```csharp
// Convenience: everything in one call. Progress is optional.
ImportResult result = await eventStore.EventLog.Import(
    events,                              // IAsyncEnumerable<EventForEventSourceId> or IEnumerable<…>
    new ImportOptions { OnInvalid = OnInvalidEvent.Reject },
    progress);                           // IProgress<ImportProgress>?

// Explicit session, for producers that are not a single enumerable (several threads, a resumable ETL).
await using IEventImport import = await eventStore.EventLog.BeginImport(options);
await import.Add(batchOfEvents);                 // chunked and pipelined by the client
ImportResult result = await import.Complete();   // waits for the commit unless options.WaitForCommit is false
IObservable<ImportProgress> progress = import.Progress;
```

- `ImportId` is a `ConceptAs<Guid>`.
- `ImportResult` carries:
  - `ImportId`, `State`
  - `EventsStaged`, `EventsCommitted`, `AlreadyAppended`
  - `FirstSequenceNumber`, `LastSequenceNumber`
  - `Rejections` (each with the event's index in the import, the event type and the reasons)
- `BeginImport(ImportId)` reattaches to an existing session, so a process that crashed mid-staging resumes from the last acknowledged chunk.
- `EventForEventSourceId` gains the per-event fields an import needs that it does not yet have: `Occurred`, `CorrelationId`, `CausedBy`, `Tags`, `NamedTags` and `Generation`.

### 8. Every client takes a non-blocking stream

Each client takes its language's native lazy, asynchronous stream of events. It reads the stream as it uploads, so the caller is never blocked while the whole source is read into memory, and a 300 000-event import never holds 300 000 events at once. Chunking, pipelining and resume behave as in point 6. A language with no asynchronous stream also takes its lazy synchronous sequence.

| Client | Takes | Returns |
| --- | --- | --- |
| .NET | `IAsyncEnumerable<EventForEventSourceId>` (or `IEnumerable<…>`) | `Task<ImportResult>` |
| TypeScript | `AsyncIterable<EventForEventSourceId>` (or `Iterable<…>`) | `Promise<ImportResult>`, cancellable with an `AbortSignal` |
| Kotlin | `Flow<EventForEventSourceId>` (or `Sequence<…>`) | `suspend` → `ImportResult` |
| Java | `Flow.Publisher<EventForEventSourceId>` (or `Stream<…>`) | `CompletableFuture<ImportResult>` |
| Elixir | any `Enumerable`, a lazy `Stream` included | `{:ok, ImportResult}`; `import_async` runs it under a `Task` |
| Go | `iter.Seq2[EventForEventSourceId, error]` with a `context.Context` | `(ImportResult, error)`; cancelled through the context |

Each client also has the explicit session from point 7 (`BeginImport`, `Add`, `Complete`, reattach by `ImportId`) for producers that are not one stream, and reports progress in its own idiom: `IProgress`/`IObservable` in .NET, a callback or async iterable in TypeScript, a `Flow` in Kotlin, a callback in Java, messages to a process in Elixir and a callback in Go.

## Options considered

- **Loop `AppendMany` in client-side chunks, as seeding does (not taken).** It needs no kernel change, but a rejected chunk 200 into a 300-chunk import leaves 199 committed and the rest not. There is no validate-first, no resume, no single catch-up, and every chunk goes through live fan-out.
- **One client-streaming gRPC `Import` RPC (not taken, see the rulings below).** It is natural for streaming, but it is the first hand-written append contract outside the generated wire. A dropped stream loses its position unless chunk acknowledgements are added, which rebuilds the session anyway. It is also unusable from the Workbench and from HTTP. Unary chunk commands give the same throughput with pipelining and come out of the generator unchanged. The client API still streams.
- **One giant transaction for atomic visibility (not taken).** MongoDB's 16 MB and 60 s transaction limits rule it out at this volume, and it would block every appender to the sequence for minutes. Validate-everything-first followed by a resumable, never-rolled-back commit gives "all or nothing *rejected*" without that.
- **Exclusive lock on the sequence during the commit, so imported events are contiguous (rejected, see the rulings below).** Importing must not lock the system in any way, so there is no exclusive mode, not even as an option.
- **An `Import` observation flag that `[OnceOnly]` skips, so reactors can ignore imported history (rejected, see the rulings below).** Imported events are delivered once through the ordinary mechanics, like any other event.
- **Let the caller choose `Replay` or `None` for observers after an import (not taken).** `Replay` would deliver earlier events to reactors a second time, and `None` would leave observers behind. Both break "each event is seen once".
- **Preserve source sequence numbers as the stored sequence numbers (not taken).** This is only possible into an empty sequence with no gaps. It creates a second meaning for sequence numbers, which observers and constraint watermarks rely on. The source number travels in causation instead.
- **A JSON Lines file format and file import in the client (dropped, see the rulings below).** A file format would fix one shape for every source. Leaving the source to the client keeps the API to one thing: an enumerable of events.
- **Kernel reads a file path or URL (not taken).** It would give callers a way to make the kernel read arbitrary files or fetch URLs with its identity.
- **Live-deliver imported events (not taken as the default).** At this volume the appended-events queue spills to catch-up anyway, after first loading every observer's live path. A single deliberate catch-up, or a replay, is cheaper and predictable.

## Default if unanswered

People import by looping `AppendMany` themselves, as seeding does. Large imports then end partially committed on the first rejected chunk, with no resume. The event sequence grain and the appended-events queue absorb 300 000 events of live fan-out. Every team that migrates writes its own fragile loader.

## Timeline and scope

- **In scope:**
  - import session commands and the observable query in `Source/Kernel/Core/Imports`, generated onto the wire
  - staging storage for MongoDB, SQL and in-memory, with the same semantics in each (framework rule on alternate implementations)
  - the `ImportEvents` commit job
  - `AppendImportChunk` on the event sequence grain
  - the observer catch-up hand-off
  - the .NET client API
  - documentation under `Documentation/events/importing.mdx`
- **Out of scope:**
  - file formats and file reading, in the kernel or the client
  - cross-sequence or cross-store imports in one session (one import targets one event sequence)
  - importing into the outbox
  - landing the TypeScript, Kotlin/Java, Elixir and Go client APIs in this repository's release; each lands in its own repository against the published contracts, with the stream types in point 8
- **Release:** additive, `minor`.
- **Horizon:** holds for the current major. Revisit if decision 0004 replaces observer cursors, because the catch-up hand-off in point 4 would become a lane re-read.

## Verification

- **Done when:**
  - Importing 300 000 events from an `IAsyncEnumerable` into an empty MongoDB-backed store completes with every event committed once, `Occurred` and causation preserved, and one catch-up per affected observer.
  - Killing the kernel during staging and during the commit, then resuming, yields the same log with no duplicates.
  - An import with one schema-invalid event commits nothing and reports that event by index.
  - Re-importing the same events with idempotency keys reports every event `AlreadyAppended`.
- **Verify by:**
  - Integration specs under `Integration/` for:
    - reject-all on an invalid event
    - resume after a staging restart and after a commit restart
    - idempotent re-import
    - interleaved ordinary appends during a commit
    - observer catch-up after commit
    - resuming staging after a dropped connection, from the last acknowledged chunk
  - These run for the MongoDB, SQL and in-memory storage providers.
  - A benchmark under `Benchmarks/` importing 300 000 events, reporting staging and commit throughput.

## Consequences

- **Easier:**
  - Migrations and environment copies become one call.
  - A failed import leaves the log untouched.
  - Long imports survive restarts.
  - Ordinary writers keep working during an import.
- **Harder:**
  - A new staging store to provision and expire in every provider.
  - Constraint validation over a whole import has to hold the import's claims, bounded by staging rather than memory.
  - The imported range interleaves with other appends.
  - Because nothing is locked, an event can pass validation at `CompleteImport` and then lose a unique-constraint race at commit. It is reported as a rejection, which makes "nothing is committed if anything is invalid" hold at validation time only.
- **Forecloses:**
  - Atomic visibility of a whole import to readers. Events become visible chunk by chunk during the commit; readers that must not see a partial import should wait for `Completed`.
- **Settled at acceptance:**
  - PII in imported events is keyed exactly as for an ordinary append. The commit runs the existing encryption step, which creates a subject's key on its first encrypted value, so a subject that does not exist yet gets its key when its first imported event is committed. Staging stores the payloads as received and never encrypts, so an abandoned import leaves no keys behind.
  - Staging retention is `Imports.StagingRetention`, 24 h by default. There is no size quota per namespace in this release; `BeginImport` reports the server's chunk limits, and an operator who needs a quota can lower `Imports.StagingRetention` or abandon imports through the Workbench.

## Rulings from review

Rulings given by Einar on 2026-11-03 and 2026-11-04 while the record was proposed. Einar accepted the record on 2026-11-08 by asking for the remaining work to be delivered and shipped.

- **Observers see imported events once, through the existing mechanics.** Reactors and reducers handle imported events as normal. There is no import flag and no extra need for `[OnceOnly]`.
- **Importing locks nothing.** No exclusive mode, no lock on the event sequence, no held-back writers.
- **The C# client takes an `IAsyncEnumerable` of events.** The client-side API in point 7 is endorsed.
- **Chunked unary calls on the wire, streaming in the client.** Chunking is kept for continuation after a dropped connection; there is no client-streaming RPC.
- **No file import.** The JSON Lines format and the file-reading client API are dropped. The client chooses how to read its source.
- **Every client imports from a streaming API where its language has one** (given 2026-11-04). Streaming keeps the import non-blocking: the client reads the source lazily while it uploads. Point 8 lists the type each client takes.
