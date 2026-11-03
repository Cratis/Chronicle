---
id: 0023-bulk-event-import
title: Import large volumes of events into an event sequence through a resumable import session that stages and validates every event before committing any, commits in ordered chunks through a job, hands observers one catch-up, and reads JSON Lines files on the client
status: proposed
stage: none
class: contract
reversibility: costly
proposed: 2026-11-03
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
  - Source/Workbench/Features/EventStore/Namespaces/Sequences/**
  - Documentation/events/**
---

<!-- markdownlint-disable-next-line MD025 -->
# Import large volumes of events into an event sequence through a resumable import session that stages and validates every event before committing any, commits in ordered chunks through a job, hands observers one catch-up, and reads JSON Lines files on the client

## Context

People need to bring existing history into Chronicle:

- migrating from another event store or a CRUD system
- moving an event store between environments
- restoring a Workbench export
- loading fixtures for load tests

The volumes are 300 000 events and more, often from a file. Today there is no import, bulk or streaming append API, and nothing reads JSON Lines.

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
- **Workbench export** (`ExportedEvent.ExportEvents`, `Core/Sequences/ExportedEvent.cs:72`) downloads one unpaged, pretty-printed JSON array. It lacks causation, subject, generation and event stream id, so an export cannot be imported back faithfully.
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
- **The commit job** (`ImportEvents`, a `Cratis.Orleans` job) commits staged events in staging order. It calls a new event sequence grain method, `AppendImportChunk`, with chunks of `Imports.CommitChunkSize` (proposed 1 000 events).
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

Each event may carry an `IdempotencyKey`, typically the source system's event id. The commit uses the keyed append path of [decision 0012](0012-keyed-appends-travel-on-their-own-rpcs.md), so re-running the same file against the same sequence reports `AlreadyAppended` for those events instead of duplicating them. Events without a key are not deduplicated.

### 4. Imported events reach every observer once, through the existing mechanics

Imported events are ordinary events. Every reactor, reducer and projection sees each of them exactly once, the same way it sees any other appended event. There is no import-specific observation flag, no opt-out, and nothing for `[OnceOnly]` to do, because the events are delivered as catch-up, not as a replay.

`AppendImportChunk` does not push the events through the live queue, `IAppendedEventsQueues`, one chunk at a time:

- When the commit job completes, each subscribed observer of the sequence whose event types intersect the import catches up once, with the existing `CatchUpObserver` job.
- With [decision 0022](0022-batched-delivery-to-client-reactors-and-reducers.md), that catch-up runs as paged, multi-partition delivery, so a reactor with an `EventBatch<T>` handler receives the import in large batches.

Live delivery of ordinary appends that interleave with the import continues as normal. The catch-up starts from each observer's own `NextEventSequenceNumber`, so nothing is skipped.

### 5. A JSON Lines file format

- The file format is UTF-8 JSON Lines, media type `application/x-ndjson`, extension `.jsonl`.
- There is one event per line. Blank lines and lines starting with `//` are ignored.
- A line:

  ```json
  {"eventType":"user-registered","generation":1,"eventSourceId":"3f2a…","eventSourceType":"Default","eventStreamType":"All","eventStreamId":"Default","occurred":"2024-03-01T10:15:00Z","correlationId":"…","subject":"…","causedBy":{"subject":"…","name":"…"},"tags":["migrated"],"namedTags":{"tenant":"acme"},"idempotencyKey":"legacy-123","sourceSequenceNumber":42,"content":{"name":"Jane"}}
  ```

- Only `eventType`, `eventSourceId` and `content` are required. `content` is a JSON object, not a JSON string.
- The Workbench export gains this format, emitting every field above with `sourceSequenceNumber` set, so **export, then import, round-trips**.

### 6. File import is a client feature, not a kernel file reader

- The client streams the file line by line, keeps a bounded number of chunks in flight (proposed 4), and calls `AddImportChunk`.
- Lines that fail to parse are reported by line number before anything is sent when `OnInvalid = Reject`.
- The kernel never opens a path or URL a caller names. This avoids a server-side file read with the kernel's identity and the authorization surface that would come with it.
- The Workbench upload uses the same session through the browser. A large-file upload of the raw file into kernel staging is a later, separate decision.

### 7. .NET client API

```csharp
// Convenience: everything in one call. Progress is optional.
ImportResult result = await eventStore.EventLog.Import(
    events,                              // IAsyncEnumerable<EventForEventSourceId> or IEnumerable<…>
    new ImportOptions { OnInvalid = OnInvalidEvent.Reject },
    progress);                           // IProgress<ImportProgress>?

ImportResult result = await eventStore.EventLog.ImportJsonLines("events.jsonl", options, progress);
ImportResult result = await eventStore.EventLog.ImportJsonLines(stream, options, progress);

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
  - `Rejections` (each with an index or line number, the event type and the reasons)
- `BeginImport(ImportId)` reattaches to an existing session, so a process that crashed mid-staging resumes from the last acknowledged chunk.
- `EventForEventSourceId` gains the per-event fields an import needs that it does not yet have: `Occurred`, `CorrelationId`, `CausedBy`, `Tags`, `NamedTags` and `Generation`.

## Options considered

- **Loop `AppendMany` in client-side chunks, as seeding does (not taken).** It needs no kernel change, but a rejected chunk 200 into a 300-chunk file leaves 199 committed and the rest not. There is no validate-first, no resume, no single catch-up, and every chunk goes through live fan-out.
- **One client-streaming gRPC `Import` RPC (not taken).** It is natural for streaming, but it is the first hand-written append contract outside the generated wire. A dropped stream loses its position unless chunk acknowledgements are added, which rebuilds the session anyway. It is also unusable from the Workbench and from HTTP. Unary chunk commands give the same throughput with pipelining and come out of the generator unchanged.
- **One giant transaction for atomic visibility (not taken).** MongoDB's 16 MB and 60 s transaction limits rule it out at this volume, and it would block every appender to the sequence for minutes. Validate-everything-first followed by a resumable, never-rolled-back commit gives "all or nothing *rejected*" without that.
- **Exclusive lock on the sequence during the commit, so imported events are contiguous (rejected, see the rulings below).** Importing must not lock the system in any way, so there is no exclusive mode, not even as an option.
- **An `Import` observation flag that `[OnceOnly]` skips, so reactors can ignore imported history (rejected, see the rulings below).** Imported events are delivered once through the ordinary mechanics, like any other event.
- **Let the caller choose `Replay` or `None` for observers after an import (not taken).** `Replay` would deliver earlier events to reactors a second time, and `None` would leave observers behind. Both break "each event is seen once".
- **Preserve source sequence numbers as the stored sequence numbers (not taken).** This is only possible into an empty sequence with no gaps. It creates a second meaning for sequence numbers, which observers and constraint watermarks rely on. The source number travels in causation instead.
- **Kernel reads a file path or URL (not taken now).** It is convenient for operators on the kernel host, but it gives callers a way to make the kernel read arbitrary files or fetch URLs with its identity. If it is needed, it belongs behind a separate decision with an allow-listed import directory.
- **Live-deliver imported events (not taken as the default).** At this volume the appended-events queue spills to catch-up anyway, after first loading every observer's live path. A single deliberate catch-up, or a replay, is cheaper and predictable.

## Default if unanswered

People import by looping `AppendMany` themselves, as seeding does. Large imports then end partially committed on the first rejected chunk, with no resume. The event sequence grain and the appended-events queue absorb 300 000 events of live fan-out. Exports cannot be re-imported faithfully. Every team that migrates writes its own fragile loader.

## Timeline and scope

- **In scope:**
  - import session commands and the observable query in `Source/Kernel/Core/Imports`, generated onto the wire
  - staging storage for MongoDB, SQL and in-memory, with the same semantics in each (framework rule on alternate implementations)
  - the `ImportEvents` commit job
  - `AppendImportChunk` on the event sequence grain
  - the observer catch-up hand-off
  - the JSON Lines format, and export in that format
  - the .NET client API
  - documentation under `Documentation/events/importing.mdx`
- **Out of scope:**
  - kernel-side file or URL reading
  - cross-sequence or cross-store imports in one session (one import targets one event sequence)
  - importing into the outbox
  - SDKs outside this repository
- **Release:** additive, `minor`.
- **Horizon:** holds for the current major. Revisit if decision 0004 replaces observer cursors, because the catch-up hand-off in point 4 would become a lane re-read.

## Verification

- **Done when:**
  - Importing a 300 000-event JSON Lines file into an empty MongoDB-backed store completes with every event committed once, `Occurred` and causation preserved, and one catch-up per affected observer.
  - Killing the kernel during staging and during the commit, then resuming, yields the same log with no duplicates.
  - A file with one schema-invalid line commits nothing and reports that line.
  - Re-importing the same file with idempotency keys reports every event `AlreadyAppended`.
  - Export followed by import into a second store reproduces content, metadata and per-type order.
- **Verify by:**
  - Integration specs under `Integration/` for:
    - reject-all on an invalid event
    - resume after a staging restart and after a commit restart
    - idempotent re-import
    - interleaved ordinary appends during a commit
    - observer catch-up after commit
    - round-trip of export and import
  - These run for the MongoDB, SQL and in-memory storage providers.
  - A benchmark under `Benchmarks/` importing 300 000 events, reporting staging and commit throughput.

## Consequences

- **Easier:**
  - Migrations and environment copies become one call or one file.
  - A failed import leaves the log untouched.
  - Long imports survive restarts.
  - Ordinary writers keep working during an import.
  - Export becomes a backup format that can be imported again.
- **Harder:**
  - A new staging store to provision and expire in every provider.
  - Constraint validation over a whole import has to hold the import's claims, bounded by staging rather than memory.
  - The imported range interleaves with other appends.
  - Because nothing is locked, an event can pass validation at `CompleteImport` and then lose a unique-constraint race at commit. It is reported as a rejection, which makes "nothing is committed if anything is invalid" hold at validation time only.
- **Forecloses:**
  - Atomic visibility of a whole import to readers. Events become visible chunk by chunk during the commit; readers that must not see a partial import should wait for `Completed`.
- **Open questions:**
  - How PII in imported events is keyed when the subject does not yet exist.
  - Staging retention and size quotas per namespace.

## Rulings from review

Rulings given by Einar on 2026-11-03 while the record is still proposed. The rest of the record stays open for a verdict.

- **Observers see imported events once, through the existing mechanics.** Reactors and reducers handle imported events as normal. There is no import flag and no extra need for `[OnceOnly]`.
- **Importing locks nothing.** No exclusive mode, no lock on the event sequence, no held-back writers.
- **The C# client takes an `IAsyncEnumerable` of events.** The client-side API in point 7 is endorsed.
