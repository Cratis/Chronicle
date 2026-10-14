---
id: 0009-durable-application-occurrences-and-schedules
title: Record application occurrences and UTC schedules durably, deliver them at least once under fenced leases, and settle effects through atomic receipts
status: accepted
stage: none
class: contract
reversibility: costly
decided: 2026-10-14
decider: L0 (woksin's delegation)
applies-to:
  - Source/Kernel/Core/Occurrences/**
  - Source/Kernel/Core/Schedules/**
  - Source/Kernel/Core/Sequences/**
  - Source/Kernel/Core/EventSequences/**
  - Source/Kernel/Core/Setup/ChronicleServerStartupTask.cs
  - Source/Kernel/Core/WellKnownServices.cs
  - Source/Kernel/Storage/Occurrences/**
  - Source/Kernel/Storage/EventSequences/**
  - Source/Kernel/Storage.InMemory/Occurrences/**
  - Source/Kernel/Storage.InMemory/EventSequences/**
  - Source/Kernel/Storage.MongoDB/Occurrences/**
  - Source/Kernel/Storage.MongoDB/EventSequences/**
  - Source/Kernel/Storage.Sql/EventStores/**/Occurrences/**
  - Source/Kernel/Storage.Sql/EventStores/Namespaces/EventSequences/**
  - Source/Kernel/Contracts/**
  - Source/Kernel/Protobuf/**
  - Source/Kernel/Grpc/Clients/ConnectionService.cs
  - Source/Clients/DotNET/Occurrences/**
  - Source/Clients/DotNET/Transactions/**
  - Source/Clients/Testing/**
  - Integration/Clustering/**
  - Integration/Kernel/Occurrences/**
  - Documentation/occurrences/**
---

<!-- markdownlint-disable-next-line MD025 -->
# Record application occurrences and UTC schedules durably, deliver them at least once under fenced leases, and settle effects through atomic receipts

Accepted on 2026-10-14 by L0 on woksin's delegation for Cratis/Chronicle#4911, with the open questions answered in [Decisions on the open questions](#decisions-on-the-open-questions). The semantics come from Cratis/Stage#343, agreed through L0 on woksin's delegation. Cratis/Arc#3288 is the command-side counterpart.

## Context

**Who needs this.**

- **Screenplay decision 0022** admits these reaction triggers into ESM v6:
  - clock triggers: `every`, `at`, a day of the week, a day of the month;
  - application triggers;
  - the built-in `Startup` and `Shutdown` triggers.
- **Its reference evaluator** fires each due clock occurrence exactly once, in time order. Intervals count from the Unix epoch and schedules are UTC. It "does not model a scheduler's at-least-once delivery" (Screenplay `Source/DotNET/Screenplay/Semantics/Execution/SemanticClock.cs:9-14`).
- **Stage** refuses these triggers with STAGE-ESM-024. The corpus reactions `WeeklyDigest` (a schedule), `PaymentImporter` (an application trigger) and `StartupBody` (a startup trigger) need them.
- **Stage#343 agreed these semantics:**
  - at-least-once delivery, with atomic event and command receipts;
  - fenced durable ownership across instances;
  - every due occurrence caught up, in bounded and resumable work, with visible lag and an opt-in skip;
  - a stable per-trigger token plus a stored revision, with explicit cursor migration;
  - startup and shutdown identity as a logical activation that the host supplies, with per-process identity as an explicit option;
  - shutdown as best effort, with durable intent for essential cleanup;
  - application triggers server-only by default, with an optional policy-gated endpoint.
- **Arc#3288** needs a completion receipt "enrolled with the successful Chronicle command commit". On a duplicate effect key, Arc must return "the recorded outcome".

**Why not a hosted service in the generated app.** Every instance would fire it. Anything that fell due during a restart would be lost. Fixing that inside each app means building a durable cursor, leader election and fencing, without access to the commit of the event log. That is this decision, rebuilt in every app.

**Why not domain tick events.** Screenplay 0022 rejected them because they invent facts and pollute `then` comparisons. In Chronicle they would also stay in the log forever, reach every observer and every replay, and still carry neither a lease nor a receipt.

**What the kernel has today.**

- **Reminders** are registered at `Source/Kernel/Grpc/Setup/ChronicleServerSiloBuilderExtensions.cs:100`, and every provider backs them:
  - MongoDB: `Storage.MongoDB/MongoDBChronicleBuilderExtensions.cs:79`;
  - SQL: `Storage.Sql/SqlChronicleBuilderExtensions.cs:52`;
  - InMemory: `Storage.InMemory/InMemoryChronicleBuilderExtensions.cs:43`.

  They are coarse wake-ups. Observer retries have a one-minute floor (`Core/Observation/Observer.Failing.cs:18, 57, 256-263`). A reminder carries no payload, only the current `TickStatus` (`Core/Captures/Capturer.cs:89`).
- **The nearest precedent, `Capturer`.** It recovers from a persistent reminder after a restart (`Capturer.cs:66, 113-135`). It then appends (`:173`) and saves its observation (`:176-178`) as two unfenced writes. A crash between them, or a second activation, appends again.
- **Grain timers** run periodic work inside one activation (`Core/EventSequences/EventSequence.cs:147-154`; `Core/Observation/Observer.Watchdog.cs:27-36`). The captures manager reconciles from a timer (`Core/Captures/CapturesManager.cs:46`).
- **Cratis.Orleans jobs** (version 1.16.3, `Directory.Packages.props:77`):
  - they are finite, prepared work, resumed at startup (Cratis/Orleans v1.16.3 `Source/Orleans/Jobs/JobsManager.cs:316-345`, called from `Core/Setup/ChronicleServerStartupTask.cs:168`);
  - a step "may be performed twice" by contract (`JobStep.cs:152-158`);
  - steps persist through the `JobSteps` provider (`Core/Events/Constraints/ReindexConstraintsStep.cs:35`);
  - there are no leases and no fencing tokens.
- **Grain storage is not fenced.**
  - Chronicle's providers write without any condition (`Core/Observation/FailedPartitionGrainStorageProvider.cs:33-45`).
  - Decision 0005 records two activations during SQL silo handover; with unconditional writes, the last writer wins.
  - None of the providers in `Core/WellKnownGrainStorageProviders.cs:9-125` is fenced.
- **Atomic identities on events.**
  - Publication identities are written in the same atomic write as the event (`Storage/EventSequences/IEventPublicationStorage.cs:8-33`; `Core/EventSequences/IEventSequence.cs:117`). Decision 0007 generalizes this to keyed appends; it is accepted but not implemented.
  - A MongoDB batch is one session transaction (`Storage.MongoDB/EventSequences/EventSequenceStorage.cs:346-428`).
  - A SQL batch is one `SaveChangesAsync` covering events and named tags (`Storage.Sql/EventStores/Namespaces/EventSequences/EventSequenceStorage.cs:321-322, 357`; `EventSequenceDbContext.cs:29-32`).
  - InMemory works under one lock (`Storage.InMemory/EventSequences/EventSequenceStorage.cs:39`).
- **The client's unit of work** commits one `AppendMany` to one sequence (`Source/Clients/DotNET/Transactions/UnitOfWork.cs:473-480`) and refuses to span sequences (`:585`). With no event sequence enrolled it appends nothing (`eventSequence is null`, `:466, 473`).
- **Retry and quarantine vocabulary.** Observers retry 10 times, starting at 1 s with a factor of 2 and a 600 s cap (`Core/Configuration/Observers.cs:51-66`). Partitions are quarantined in `Observer.Failing.cs:57-63`.
- **Security.**
  - With authentication enabled, every endpoint requires an authenticated principal unless it is `[AllowAnonymous]` (`Source/Kernel/Server/Authentication/ServiceCollectionExtensions.cs:174-178`).
  - With authentication disabled, everything is allowed (`:76-81`).
  - Operator commands also carry `[Authorize]` explicitly (`Core/Sequences/ReopenStreamScope.cs:26`).
- **Wire surface.**
  - Core artifacts generate the gRPC surface (`.cratis/ai/rules/project/the-wire-protocol-is-generated-from-core-don-t-think-about-the-protocol.md`).
  - Commands can return results (`Core/Captures/StartCapture.cs:15-28`). Queries can be observable (`Core/Jobs/JobSummary.cs:46, 59`).
  - Capabilities are advertised at `Grpc/Clients/ConnectionService.cs:142`, from `Contracts/Clients/KernelCapabilities.cs:14-19`.
- **Namespaces** record their creation time (`Storage/Namespaces/NamespaceState.cs:13`; `INamespaceStorage.cs:33`). Per-namespace rehydration runs at `ChronicleServerStartupTask.cs:141-171`.
- **Clustering** is `Localhost` or `MongoDB` only (`Core/Configuration/ClusteringType.cs:9-20`).

This decision adds a contract across every provider, the wire and the .NET client, and Stage and Arc build on it. Reversing it would be costly. Decision 0004 Q4 promises at-least-once delivery, not exactly-once effects, and this decision keeps that promise.

## Decision

Chronicle records **occurrences**, scoped to an event store and a namespace, in infrastructure storage. That storage is never the event log.

- **Sources.** Occurrences come from typed ingress (application and lifecycle triggers) or are materialized from UTC schedules.
- **Delivery.**
  - Each occurrence is delivered at least once, under a stable identity.
  - Workers hold renewable leases fenced by an epoch, so a worker whose epoch was superseded can never complete or fail an occurrence. Schedule cursors move only by a compare-and-set on their version, in the same transaction as the occurrences they cover.
  - Order is promised only among occurrences whose attempts succeed (section 2).
- **Effects** settle through a **per-effect completion receipt**. The receipt is written in the same storage transaction as the events its effect appends, or alone when the effect appends none. A redelivered occurrence finds its receipts and does not repeat its Chronicle effects.
- **Schedules** start from a durable `EffectiveFrom`. They materialize every due instant in bounded, resumable and atomic turns. Lag is visible, and skipping is opt-in and always recorded.

### 1. Occurrence ingress and authorization

- **Trigger definitions.**
  - The client registers each trigger with a stable `TriggerId`, a name and a JSON schema, as it registers event types (`Core/EventTypes/RegisterEventTypes.cs`).
  - A schema carrying compliance metadata is refused, as it is for sinks (`Core/Sinks/EventSequenceSinkFactory.cs:33-36, 48-53`). Payloads are stored in plaintext and are not crypto-shredded.
- **`RecordOccurrence(trigger, idempotencyId, payload)`.**
  - It validates the payload against the trigger's current schema.
  - **Identity.** The length-prefixed SHA-256 of (`TriggerId`, `idempotencyId`), in the `PublicationIdentity` encoding (`Core/Sinks/PublicationIdentity.cs:31-40`).
  - **Fingerprint.** A SHA-256 over the trigger id and the canonical JSON of the payload (`Core/EventSequences/EventHashCalculator.cs:44-75`).

| Case | Result |
| --- | --- |
| New identity | `Recorded`, durable before the reply |
| Same identity, same fingerprint | `AlreadyRecorded`, same occurrence, nothing written |
| Same identity, different fingerprint | refused with `OccurrenceIdConflict`, nothing written |
| Invalid payload | refused with `OccurrencePayloadInvalid` |

  The insert is insert-if-absent on the identity: a unique `_id` in MongoDB, a primary key in SQL, and the lock in InMemory. A lost acknowledgement can therefore always be retried.
- **What an occurrence stores:**
  - its identity, trigger and kind (`Application`, `Schedule`, `Startup` or `Shutdown`);
  - its payload, schema revision and recorded-at time;
  - for a schedule, its due instant and the definition revision;
  - its state, attempts, lease epoch, owner, expiry, last error and next attempt.
- **Authorization.**
  - Every occurrence and schedule command and query is a kernel API under the fallback policy: an authenticated principal when authentication is enabled (`ServiceCollectionExtensions.cs:174-178`), and anyone when it is disabled (`:76-81`), as for appends today. The kernel exposes Core artifacts over both gRPC and its HTTP API.
  - The operator commands `SkipPending`, `RetryQuarantinedOccurrence` and `RetireSchedule` also carry `[Authorize]`, as `ReopenStreamScope.cs:26` does. They record the authenticated principal in their skip or audit record.
  - The kernel adds no per-trigger roles.
  - "Server-only by default, with an optional policy-gated endpoint" (Stage#343) belongs to the application. The .NET client exposes `Record` only as an in-process API and generates no endpoint. Arc and Stage own any public trigger endpoint and its policy.

### 2. Delivery, fencing, ordering, retry, quarantine and status

- **Claim.** `ClaimOccurrences(trigger, workerId, leaseDuration, max)` takes claimable occurrences: those that are `Pending`, those whose retry is due, and those that are `Leased` with an expiry at or before the kernel's now. Each claim is one conditional write that:
  - raises `Epoch` by one;
  - sets `Leased`, the owner, and an expiry of kernel now plus the duration (default 60 s, capped at 10 min);
  - raises `Attempts` by one.

  It returns a `LeaseToken` of (occurrence id, epoch).
- **Ordering, the only rule.**
  - Claims hand out claimable occurrences in due order for schedules and in recorded order otherwise. At most `MaxInFlight` occurrences per trigger are leased at once; the default is 1.
  - Order is promised only among occurrences whose attempts succeed. A failed occurrence waits out its backoff (tens of minutes in total at the defaults) without blocking later ones, and a quarantined occurrence parks only itself. A later occurrence can therefore complete before an earlier one that is retrying.
  - A handler that needs order across occurrences derives it from its own state, for example the due instant against a read model.
  - There is no head-of-line option in v1 (Options).
- **Fenced transitions.** `RenewLease(token)`, `Complete(token)` and `Fail(token, error)` are compare-and-set operations on (id, epoch, `Leased`):
  - MongoDB: an `UpdateOne` whose filter carries the epoch;
  - SQL: `ExecuteUpdate … WHERE Id=@id AND Epoch=@epoch AND State='Leased'`, checking the rows affected;
  - InMemory: under the lock.
- **Expired leases.** A transition succeeds with the current epoch even after the lease expired, as long as no claim has raised the epoch since. `RenewLease` then sets a new expiry.
- **Stale means superseded.** A call whose epoch is lower than the stored one gets `OccurrenceLeaseLost`, and nothing changes.
- **Idempotent completion.** A `Complete` that repeats the completing epoch gets `AlreadyCompleted`.
- **Time and disconnects.** Expiry uses only the kernel's clock. A client disconnect (`Core/Clients/ConnectedClients.cs:90`) expires that client's leases.
- **Retry and quarantine.**
  - A `Fail` and an expired lease both count as attempts. Backoff mirrors the observer defaults (`Observers.cs:51-66`) under a new `Occurrences` configuration section.
  - After `MaxAttempts` the occurrence becomes `Quarantined`, as a partition does (`Observer.Failing.cs:57-63`).
  - `RetryQuarantinedOccurrence` re-arms it.
- **Wake-ups.** A grain timer runs while the grain is active. A persistent reminder with a period of at least one minute (`Observer.Failing.cs:18`) brings retries back after a total restart. Reminders only wake the grain; storage decides what runs.
- **Status.**
  - `GetOccurrence(id)` returns the state, attempts, epoch, owner, expiry, last error, next attempt and receipts.
  - `ObserveOccurrences(trigger)` is the observable form, and it is also the workers' wake signal.

### 3. Effect receipts and the atomic append

> **2026-10-14 — amendment, accepted by L0 with L20.** A unit of work with
> protected reads and no events records its `NoEvents` receipt through
> `AppendManyWithEffectReceipt`, with zero events and the decision's concurrency
> scopes. This is a validate-only append: the `IEventSequence` grain checks the
> scopes, and a violation writes no receipt. Storage supports the zero-event
> receipt batch inside the same batch transaction. `RecordEffectReceipt` carries
> no scopes and is only for `Refused`, or for `NoEvents` without protected reads.
> A pipeline refusal (constraint, concurrency or validation) writes no kernel
> receipt; Arc records `Refused` explicitly. This qualifies the receipt-only and
> unit-of-work paths below so that a no-event decision cannot bypass its protected
> reads.

- **Effect key.** A non-empty name the handler chooses. Arc derives one per invoked command; Stage derives one per `produces` position.
- **Receipt identity.** `effect:` followed by the length-prefixed SHA-256 of (occurrence id, effect key).
- **Receipt contents:**
  - the identity;
  - the outcome: `Appended` with the sequence id and the first and last sequence numbers, `NoEvents`, or `Refused` with its reasons;
  - an optional `Response`: an opaque JSON value of at most 64 KiB, stored and returned verbatim. It is how Arc returns "the recorded outcome" of a command, response included. It is plaintext and not crypto-shredded, so it must not carry personal data; Arc decides what to record;
  - the lease token that wrote it, and the recorded-at time.
- **First writer wins.** A receipt never compares content, because a re-execution may legitimately produce different events or a different response. The recorded outcome stands.
- **The append path**, through the new grain method `IEventSequence.AppendManyWithEffectReceipt`:
  - It looks up the receipt before any validation, as `AppendPublication` does.
  - **On a hit,** it returns `EffectAlreadyRecorded` with the stored outcome and response, and raises the grain's counters as 0007 section 3 does.
  - **On a miss,** it runs the normal pipeline and writes the events and the receipt in one storage call.
  - **Collisions** are settled again by lookup, as in 0007 section 3 (`IdentitiesAlreadyDurable`, `TransactionConflicted`). A receipt still unsettled fails retryably with `EffectReceiptUnsettled`, and nothing is written.
- **Without events.** `RecordEffectReceipt` writes a `NoEvents` or `Refused` receipt on its own, with an optional `Response`.
- **Lookup.** `GetEffectReceipt(occurrence, effectKey)` lets Arc return the recorded outcome without executing again.
- **Deduplicated, not fenced.** A superseded worker's effect either finds the receipt or loses the unique write (Options).

**Storage guarantee per provider:**

| Provider | Where the receipt lives | Atomic with the events | Survives |
| --- | --- | --- | --- |
| MongoDB | `effect-receipts` collection in the namespace database (`EventSequenceStorage.cs:47`), unique `_id` | yes: inserted in the batch session transaction (`:346-428`), with the batch's write concern. It needs the replica set the batch path already requires. A commit whose result is unknown is settled by the next lookup, because the receipt is present exactly when the events are | kernel restart, failover |
| SQL (SQLite, PostgreSQL, SQL Server) | `EffectReceipts` table mapped on `EventSequenceDbContext` beside `Events` and `NamedTags` (`:29-32`), primary key on the identity hash, added by a namespace migration | yes: the same `SaveChangesAsync` (`EventSequenceStorage.cs:357`). A unique violation is classified by lookup, never by its predicate (0007) | kernel restart, failover |
| InMemory | a map under the storage lock (`Storage.InMemory/EventSequences/EventSequenceStorage.cs:39`) | yes, within the process | the process only; events and receipts vanish together |

- **Not atomic, by design:**
  - receipts against occurrence state, which redelivery reconciles;
  - anything outside Chronicle.
- **Capability gate.** The receipt travels as a nullable `EffectReceipt` field at the end of the Core artifacts `Append`, `AppendMany` and `AppendManyForEventSources`, and of their named-tag variants (`Core/Sequences/AppendMany.cs:28-41`). The client sends it only when the kernel advertises `effect-receipts`, because an old kernel would drop the field without saying so.
- **Unit of work.** It carries the receipt into its single commit (`UnitOfWork.cs:473-480`). When nothing is enrolled (`:466, 473`), it carries the receipt into `RecordEffectReceipt` instead.

### 4. Scope

- **Namespace scope (the default).**
  - The definition is registered once per event store. Occurrences, cursors and receipts live in each namespace's storage, through a new `IOccurrencesStorage` on `IEventStoreNamespaceStorage` (`Storage/IEventStoreNamespaceStorage.cs:30-110`).
  - A namespace added later gets its own cursor, as section 6 describes. `NamespaceAdded` (`ChronicleServerSiloBuilderExtensions.cs:94`) only speeds that up; correctness never depends on receiving it.
- **Application scope (declared).** One cursor and one stream of occurrences per event store, in event-store-level storage beside `Captures` (`Storage/IEventStoreStorage.cs:91`). Its effects name their namespace, and their receipts live there.

### 5. Schedule rules (UTC only)

The rules are the Screenplay reference rules, unchanged:

| Rule | Shape | Due instants |
| --- | --- | --- |
| Interval | `every` N seconds, N ≥ 1 | multiples of N from the Unix epoch, floored before 1970 (`SemanticClock.cs:62-79`) |
| Daily | `at`, a second of the day from 0 to 86 399 | each UTC date plus `at` |
| Weekly | `at` and a day of the week from 0 to 6, Sunday 0 | that weekday plus `at` |
| Monthly | `at` and a day of the month from 1 to 31 | that day plus `at` |

The ranges come from Screenplay `SemanticModelValidator.Automation.cs:189-195` and `SemanticReactions.cs:84-99`. Weekly and monthly cannot be combined.

- **Due window.** `(cursor, now]`: exclusive below, inclusive above.
- **Month end.**
  - A month without the requested day has **no** occurrence. The 31st falls due seven times a year, and the 29th falls in February only in Gregorian leap years (2000 is one, 2100 is not).
  - The day is never clamped to the last day of a shorter month. That is not a skip, because nothing fell due (`for_SemanticClock/when_finding_boundary_occurrences.cs:26, 38`).
  - "Last day of the month" would need a Screenplay language change first.
- **No time zones.** Everything is UTC, so there is no daylight saving time: `09:00` is 09:00 UTC all year. Unix seconds have no leap seconds.
- **Range.** Enumeration stops at 9999-12-31 without overflow, and an interval of `long.MaxValue` is valid (`:36-37`). A schedule past its last instant reports `Exhausted`.

### 6. Effective-from, cursor, catch-up, lag and skipping

- **Definition and `EffectiveFrom`.**
  - The definition lives in event-store-level storage.
  - The first registration of a token writes `EffectiveFrom` in the same insert-if-absent write as the definition. Its value is `StartsAt` when the definition carries one, and otherwise the kernel instant of that write.
  - Registering again never moves it; only `RestartAt` does (section 7). A `StartsAt` in the past is an explicit backfill under the catch-up policy.
- **The cursor.** One per (namespace, token), or one per token under application scope. It holds:
  - the last materialized due instant, in epoch seconds;
  - `RegisteredRevision`, the newest revision registration has announced to this cursor;
  - `AppliedRevision`, the revision whose directive this cursor has applied;
  - a write `Version`. Every cursor write is a compare-and-set on `Version`.

  There is no separate ownership epoch. The version compare-and-set is what serializes turns.
- **The initial cursor.**
  - It is created insert-if-absent by whichever path needs it first: the startup step `EnsureSchedules`, the `NamespaceAdded` handler, the manager's reconciliation timer (in the style of `CapturesManager.cs:46`), or any `ISchedule` activation.
  - Its value is `max(EffectiveFrom, NamespaceState.Created)` (`NamespaceState.cs:13`), or `EffectiveFrom` under application scope. It is never the Unix epoch (Screenplay 0022, status note of 2026-10-02).
  - The value is computed from durable facts, not from the moment the cursor is created. A crash between registration and cursor creation, or a missed `NamespaceAdded` that reconciliation repairs later, therefore only delays materialization. Catch-up then covers every instant since the initial cursor, and no `SkippedRange` is needed.
- **A materialization turn is one atomic write.** Occurrences, cursors and skip records live in the same occurrence storage, so each turn commits as one MongoDB session transaction, one SQL `SaveChangesAsync`, or one InMemory lock section. In it, the turn:
  1. enumerates at most 1 000 due instants, or scans at most 3 700 calendar days for calendar rules, in `(cursor, now]`;
  2. writes each occurrence if absent, with the identity SHA-256 of (token, due epoch-second):
     - MongoDB uses an upsert with `$setOnInsert`, so an existing occurrence does not abort the transaction;
     - SQL looks up the existing identities first, and retries the turn on a unique violation;
  3. writes any `SkippedRange` its policy requires;
  4. compare-and-sets the cursor's `Version`, and applies any pending migration (section 7).

  The result is either all of it or none of it:
  - a turn that loses the compare-and-set writes nothing;
  - a duplicate or stale activation can never insert occurrences or move the cursor. Accepting a new revision sets `RegisteredRevision` on every existing cursor and bumps its `Version` before registration returns; it never touches `AppliedRevision`. A turn computed under an older revision therefore loses its compare-and-set and writes nothing. A turn whose loaded definition differs from the cursor's `RegisteredRevision` reloads the definition and starts again;
  - a crash leaves the turn wholly applied or wholly absent;
  - because the identity does not depend on the revision, an instant due under two revisions fires once.
- **Catch-up** repeats turns from a timer until the cursor reaches now. Every missed instant is materialized in order, and catch-up resumes after any restart. Delivery then follows section 2.
- **Lag is visible.** `GetScheduleStatus(token)` and its observable form report:
  - the revision, `EffectiveFrom`, the cursor and the next due instant;
  - pending, leased and quarantined counts;
  - **materialization lag**: now minus the cursor, while behind;
  - **delivery lag**: now minus the due instant of the oldest occurrence not yet completed;
  - the skip records.

  Both lags are also meters, through `AddChronicleMeters` (`ChronicleServerSiloBuilderExtensions.cs:112`), with a warning above a configurable threshold.
- **Skipping is opt-in and always recorded.**
  - The default policy is `CatchUp.All`.
  - `CatchUp.SkipOlderThan(duration)`, declared on the definition, materializes nothing older than now minus the duration. The turn records a `SkippedRange` (token, revision, from, through, count, policy) in the same transaction.
  - `SkipPending(token, through)` marks pending occurrences `Skipped`, with a record that names the principal.
  - Nothing else skips. Quarantine is not a skip.

### 7. Revisions and migration

- **Token and revision.**
  - The client supplies a stable **trigger token**. Stage derives it from the reaction's semantic identity and the trigger's position.
  - The kernel stores a fingerprint over (rule, catch-up policy, `StartsAt`, scope). The **revision** rises whenever that fingerprint changes.
  - Each occurrence keeps the revision that materialized it.
- **A changed definition must name a directive.** Without one, registration is refused with `ScheduleMigrationRequired`. The directive is stored with the revision, and each cursor applies it once, inside a turn's transaction, when `AppliedRevision` is below `RegisteredRevision`, and then sets `AppliedRevision` to it. A cursor created after the registration starts with both revisions at the current one and applies no directive. A crash mid-migration therefore resumes per namespace. The directives:
  - **`Continue`** keeps the cursor, applies the new rule after it, and keeps pending occurrences. It is refused across a scope change (`ScheduleScopeChangeRequiresRestart`), because application and namespace cursors live in different storage.
  - **`RestartAt(instant)`** sets `EffectiveFrom` to the instant, and sets each cursor to `max(instant, NamespaceState.Created)`.
    - It cannot fire again an instant that was already materialized. Identities depend only on token and instant, so such an instant deduplicates against the stored occurrence, whether that occurrence is completed or not.
    - This holds as long as occurrences are retained, which is the life of the namespace (decision 1 under the open questions). A future compaction must keep the identities so that this still holds.
  - **`DiscardPending`** also marks pending occurrences of earlier revisions `Skipped`, with a record.
  - **An optional `ExpectedRevision`** refuses the directive with `ScheduleRevisionMismatch` when the stored revision differs. A directive left in the code then never applies to a later, unrelated change.
- **No implicit directive.**
  - The .NET client sends no directive by default. A changed definition fails registration at startup, naming the token, the stored and the new revision, and the directives.
  - Stage always emits a directive from its model diff.
- **Retirement and renames.**
  - `RetireSchedule(token)` stops materialization. Pending occurrences stay deliverable.
  - Registering a retired token again is a change, and it needs a directive.
  - A renamed token is a new schedule, with a new `EffectiveFrom`.

### 8. Lifecycle triggers

- **Identity.**
  - The host or the orchestrator supplies an `ActivationId`, such as a rollout id, so the N instances of one logical activation record **one** occurrence.
  - `ActivationScope.PerProcess` is the explicit alternative: a fresh id for each process start.
  - A host that registers lifecycle handlers without either fails fast at start.
- **Startup.** At host start, the client records `Startup` with the identity SHA-256 of (`startup`, `ActivationId`).
- **Scope and fan-out.**
  - **Application scope (the default)** produces one occurrence per activation.
  - **`AllNamespaces` scope** produces one occurrence per namespace that exists when the occurrence is recorded, with the identity SHA-256 of (kind, `ActivationId`, namespace).
    - The kernel first writes an event-store-level fan-out intent, insert-if-absent. Reconciliation completes that intent after a crash.
    - Namespaces created later in the same activation do not get the occurrence.
  - **Stage renders a startup reaction that produces into every namespace with `AllNamespaces`.** A failure then retries or quarantines only that namespace's occurrence, and lag is reported per namespace. An application-scoped occurrence that writes to many namespaces would retry all of them on any failure: receipts keep the finished namespaces idempotent, but quarantine would cover them all.
- **Shutdown.**
  - **Logical activation:** the host or orchestrator ends the activation explicitly with `EndActivation(ActivationId)`, and that records `Shutdown` once. A graceful stop of one instance, such as a scale-in, never fires the shutdown trigger.
  - **`ActivationScope.PerProcess`:** on `ApplicationStopping`, the client records `Shutdown` for that process and tries to handle it within the host's shutdown timeout.
  - Once recorded, it is delivered at least once, by any live worker or by the next activation.
  - If the process dies before recording it, nothing fires. Essential cleanup must therefore be **durable intent recorded while the app runs**, as an application occurrence or a schedule. Chronicle never infers an ungraceful death.

### 9. Mapping onto the kernel

**Grains** (in `Core/Occurrences` and `Core/Schedules`):

- **`IOccurrenceQueue`**, keyed by (event store, namespace or application, trigger id).
  - It is non-reentrant and handles ingress, claims, leases, completion, failure, retry, quarantine and lifecycle fan-out.
  - Status reads are `[AlwaysInterleave]`, and it is `IRemindable`.
- **`ISchedule`**, keyed by (event store, namespace, token).
  - It owns the cursor, materialization, migration and skips.
  - It uses the `schedule-wake` reminder and recovers the way `Capturer` does (`Capturer.cs:89-135`), with a timer for rules shorter than a minute.
- **`ISchedulesManager`**, one per event store. It owns the definitions and `EffectiveFrom`, ensures cursors, and reacts to `NamespaceAdded`. A reconciliation timer covers every namespace (`CapturesManager.cs:46`).
- **`IEventSequence`** gets two new grain methods, `AppendManyWithEffectReceipt` and `RecordEffectReceipt`. A mixed-version cluster therefore fails closed (0007 section 2).
- **Startup steps** `EnsureSchedules` and `RehydrateOccurrences` join `EnsureCaptures` and `RehydrateJobs` (`ChronicleServerStartupTask.cs:135, 168`).

**Storage.**

- Fencing needs compare-and-set, and the grain storage providers cannot do it, so there is **no new `WellKnownGrainStorageProviders` entry**. Grains call `IOccurrencesStorage` and `IScheduleStorage` directly.
- Both are implemented in MongoDB, in SQL with namespace migrations, and in InMemory.
- Receipts extend the event-sequence storage (section 3).

**Wire.**

- The new `WellKnownServices.Occurrences` and `WellKnownServices.Schedules`.
- `[Command]`s: `RegisterTriggers`, `RecordOccurrence`, `ClaimOccurrences`, `RenewOccurrenceLease`, `CompleteOccurrence`, `FailOccurrence`, `RetryQuarantinedOccurrence`, `RecordEffectReceipt`, `RegisterSchedules`, `RetireSchedule` and `SkipPending`.
- `[ReadModel]` queries: `GetOccurrence`, `ObserveOccurrences`, `GetEffectReceipt`, `GetScheduleStatus` and `ObserveScheduleStatus`.
- The `EffectReceipt` field appended to the append artifacts.
- Everything below Core is generated. Capabilities `occurrences`, `effect-receipts` and `schedules` are advertised at `ConnectionService.cs:142`.

**.NET client.**

- `IEventStore.Occurrences`, beside `Reactors` (`Source/Clients/DotNET/IEventStore.cs:82`).
- `[Trigger("id")]` record types, recorded with `Record(trigger, idempotencyId)`, in process only.
- `IOccurrenceHandler<TTrigger>`, discovered the way reactors are. The client runs the claim, renew and complete loop, and wakes on `ObserveOccurrences`.
- `[Schedule("token")]` with `[Every]`, `[DailyAt]`, `[WeeklyOn]` or `[MonthlyOn]`, plus `CatchUp`, `Scope` and `Migration` (with an optional `ExpectedRevision`).
- **`OccurrenceContext`** exposes the id, trigger, due or recorded time, revision, attempt and lease token, and `EffectKey(name)`, which is the key handlers pass to external endpoints.
- **`IUnitOfWork.UseEffectReceipt(key, response)`.**
- **Lifecycle hooks** with `ActivationId`, `ActivationScope` and the lifecycle scope.
- **A deterministic time driver** in Chronicle.Testing.

**Reused machinery:**

- reminders and timers, as wake-ups;
- the `PublicationIdentity` encoding;
- the settle-again path and storage failure classification from 0007;
- each provider's batch transaction boundaries;
- the observer backoff and quarantine vocabulary;
- `ConnectedClients` disconnects;
- startup rehydration;
- the fallback authorization policy;
- Core-to-wire generation.

Cratis.Orleans jobs are not used (Options).

### 10. Not promised, and out of the domain

- **Not promised:**
  - exactly-once external effects (endpoints get the effect key);
  - firing at the due second (lateness is reported as lag);
  - order beyond section 2's rule: order holds only among successful attempts, and a retrying or quarantined occurrence does not block later ones;
  - order across triggers or tokens, including Screenplay's semantic-identity order for instants that fall due together;
  - local time, cron syntax and sub-second rules;
  - shutdown before it is recorded;
  - lifecycle occurrences for namespaces created after the occurrence was recorded.
- **Out of the domain.**
  - Occurrences, leases, receipts, cursors, `EffectiveFrom` and skip records are never events.
  - No event types are registered for them, and no observer or projection sees them.
  - Screenplay specifications keep asserting facts under `given clock` and `when clock` (0022). Scheduler state never becomes a specification expectation.

## Options considered

- **A hosted service or timer in each generated app.** It fires once per instance and misses restarts. Making it correct means rebuilding sections 2 and 6 in every app, with no atomic link to the commit. Not taken.
- **Domain tick events in the log.** Screenplay 0022 rejected them because they invent facts. They would also stay forever, reach every observer and replay, and carry no lease or receipt. Not taken.
- **Orleans reminders as the scheduler.** They have a one-minute floor (`Observer.Failing.cs:18`), no payload, no record of the instants they missed, and no receipts. Kept only as a wake-up.
- **One Cratis.Orleans job per schedule or occurrence.** Jobs are finite, a step may run twice (`JobStep.cs:152-158`), there is no lease or fence, and rehydration is wholesale. Not taken.
- **Orleans persistent grain state.** Writes are unconditional (`FailedPartitionGrainStorageProvider.cs:33-45`), so the last writer wins. Not taken.
- **0007 keyed appends as the only receipt.** No-event and refused effects leave no row, and there is no response to return. A re-executed command with different events conflicts permanently, and Arc could not skip re-execution. Not taken; receipts sit beside 0007.
- **Fencing every effect append against the lease.** That needs a transaction spanning the occurrence store and the event store, which no provider offers. Receipt uniqueness gives the same result for Chronicle effects. Not taken.
- **A schedule ownership epoch next to the cursor.** It is redundant once the cursor and its occurrences commit in one transaction, compare-and-set on `Version`. Not taken.
- **The initial cursor as the moment of cursor creation.** A crash or a missed `NamespaceAdded` between registration and cursor creation would lose due instants, without any record that they were skipped. Not taken; `EffectiveFrom` instead.
- **Strict head-of-line order per trigger.** One poisoned occurrence would stop a schedule until an operator acts, against "every due occurrence, bounded". Not in v1; it can be added later as an explicit per-trigger option.
- **Clamping the day of the month to the month's end.** It diverges from the reference vectors. Not taken.
- **An implicit `Continue` in the .NET client.** It would turn a deliberate migration into a default. Not taken.
- **An external scheduler (Quartz.NET, Hangfire).** It needs its own store per provider, has no atomic receipt, has no namespace scope, and adds a dependency to every app. Not taken.

## Default if unanswered

- Stage keeps refusing clock, application, startup and shutdown triggers with STAGE-ESM-024.
- Arc#3288 has no receipt to enroll.
- Generated apps hand-roll timers, which fire twice across instances and lose occurrences across restarts.
- The unfenced append-then-save of `Capturer` remains the only precedent.

## Timeline and scope

Ingress ships before schedules, which matches Stage's order: startup and application triggers first, then schedules, then shutdown. Every PR is a minor, additive change, and every new-behavior spec is red before its change.

**Fault injection.** Test-only `CommitThenFail` decorators over `IEventSequenceStorage`, `IOccurrencesStorage` and `IScheduleStorage` perform the real write and then throw. That simulates a commit before acknowledgement on the kernel side. A client-side `DropReplyInterceptor` discards a successful gRPC reply. Kill windows stop a silo, or the whole cluster, between two steps through a test hook.

1. **Storage for occurrences, leases, cursors and receipts, in all providers.**
   - **Change:**
     - insert-if-absent writes;
     - the epoch compare-and-set;
     - the versioned cursor with atomic turn writes;
     - `EffectiveFrom`;
     - receipts inside the batch transaction;
     - the SQL migrations.
   - **Verify** with provider specs on InMemory, MongoDB (`MongoDBFixture`) and SQL (SQLite and PostgreSQL):
     - ingress replay and payload mismatch;
     - two claims racing;
     - a superseded epoch refused, and an expired but unclaimed epoch accepted;
     - two storage instances racing a turn on the same database, standing in for duplicate activations on SQL: one turn writes, and the other writes nothing;
     - two receipts racing, including concurrent MongoDB transactions;
     - after `CommitThenFail` on every write, a receipt present exactly when its events are.
2. **The kernel occurrence queue, receipts and wire.**
   - **Change:** `IOccurrenceQueue`, `AppendManyWithEffectReceipt`, `RecordEffectReceipt` with `Response`, the status queries, authorization, the `Occurrences` service, and the capabilities.
   - **Verify** with Core.Specs:
     - `AlreadyRecorded` and `OccurrenceIdConflict`;
     - ordering with a retrying earlier occurrence: the later one completes first;
     - backoff, quarantine and re-arm;
     - a superseded `Complete` returns `OccurrenceLeaseLost`;
     - operator commands record the principal.
   - **Verify the crash windows:**
     - ingress committed with the reply dropped;
     - a claim committed with the reply dropped;
     - **an effect committed before acknowledgement** (`CommitThenFail`): the retry returns `EffectAlreadyRecorded` with the stored response, and exactly one set of events exists;
     - an effect committed, then the worker killed before `Complete`;
     - `Complete` committed with the reply dropped.
   - **Gates:** wire compatibility.
3. **.NET client, lifecycle triggers and Testing.**
   - **Change:** triggers, handlers, the receipt in the unit of work, lifecycle hooks including `AllNamespaces` fan-out, and the time driver.
   - **Verify on MongoDB** with two kernel silos and two app instances in `Integration/Clustering`. That is MongoDB-only (`ClusteringFixture.cs:34-40`; `ClusteringType.cs:9-20`), following `for_ScaledOutClients/when_two_client_instances_are_connected.cs`:
     - one startup occurrence per logical activation, and two per process;
     - an application occurrence handled once while one instance is killed mid-lease;
     - a **total restart** with an occurrence leased: it is reclaimed at epoch + 1 and completed once;
     - fan-out completed after a kill midway.
   - **Verify on SQL** in `Integration/Kernel/Occurrences`, with one silo and two app instances, plus the PR 1 storage races:
     - the same scenarios;
     - a total restart.
   - **Verify against an old kernel:** no receipt is ever sent.
4. **The schedule engine.**
   - **Change:** rules, `EffectiveFrom` and the initial cursor, atomic bounded turns, catch-up, revisions with directives and `ExpectedRevision`, skip records, lag, the `Schedules` service and the `schedules` capability.
   - **Verify the boundary vectors shared with Stage and the Screenplay reference:** a fractional start, the floor before the epoch, offset normalization, the 31st across February, 29 February in 2028 and in 2100, the last calendar day, and a huge interval. All must equal `SemanticClock`.
   - **Verify the crash windows:**
     - a kill after the definition and `EffectiveFrom` commit but before any cursor exists: after the restart, cursors start at `EffectiveFrom` and every instant since is materialized, with no `SkippedRange`;
     - `NamespaceAdded` suppressed: reconciliation creates the cursor at `max(EffectiveFrom, Created)` and catches up;
     - `CommitThenFail` and a kill mid-turn: each turn is wholly present or wholly absent;
     - a stale activation computing with an older revision loses its compare-and-set after the new revision is registered, and inserts nothing.
   - **Verify catch-up and directives:**
     - after 30 days down, an every-60 s schedule materializes all 43 200 instants in bounded turns, with lag falling monotonically, and resumes after a kill;
     - each directive, including `RestartAt` deduplicating an instant that was already completed, and a scope change refused under `Continue`;
     - `SkipOlderThan` records its range in the same transaction.
5. **.NET schedules and documentation.**
   - **Change:** the attributes and options, the startup failure on a missing directive, and `Documentation/occurrences`.
   - **Verify** with an integration spec: a two-instance app with a weekly rule, driven by the time driver, on MongoDB and on SQL.

**Out of scope:**

- Arc's command execution (Arc#3288);
- Stage rendering and its policy-gated endpoint;
- Workbench views;
- non-.NET clients beyond the generated contracts;
- local-time rules;
- SQL multi-silo clustering.

## Decisions on the open questions

1. **Retention: the life of the namespace**, consistent with 0007.
   - Completed occurrences are stored compactly: identity and outcome.
   - Receipts and payloads follow the same rule for now.
   - A follow-up, Cratis/Chronicle#4913, covers compaction that drops the bulk but keeps the deduplication identities, and with them `RestartAt` safety.
2. **Shutdown of a logical activation: explicit `EndActivation`** from the host or orchestrator, so a scale-in never fires a shutdown trigger.
   - "First graceful stop" applies only to `ActivationScope.PerProcess`.
   - The documentation states both.
3. **Shared boundary vectors: a JSON fixture published from Screenplay's canonical corpus**, consumed by hash in Chronicle and Stage.
   - Screenplay owns the clock rules (`SemanticClock`) and publishes the fixture.
   - PR 4 (schedules) waits for that fixture rather than pinning a temporary copy.

## Verification

**Done when:**

- On MongoDB with two silos, and on SQL with one silo and racing storage instances, every due or recorded occurrence identity fires. Its Chronicle effects are durable exactly once per effect key across two app instances, a total restart and every listed crash window.
- A worker whose epoch was superseded can neither complete nor fail an occurrence.
- No turn moves a cursor without the occurrences it covers.
- Every scheduled instant since `EffectiveFrom` is materialized, or covered by a skip record, including after a crash before any cursor existed.
- The rules match the shared vectors exactly.
- Lag is reported.
- No occurrence state appears in any event sequence.

**Verify by:** the PR 1 provider specs, the Core.Specs and crash-window specs of PRs 2 and 4 (`CommitThenFail`, `DropReplyInterceptor` and kill hooks), the `Integration/Clustering` (MongoDB) and `Integration/Kernel/Occurrences` (MongoDB and SQL) specs, the vector spec against the Screenplay corpus, and the wire-compatibility gates.

## Consequences

- **Easier:**
  - Stage can render clock, application, startup and shutdown triggers faithfully.
  - Arc gets an atomic receipt that carries the recorded response, plus a lookup.
  - Applications get one stable key to pass to external endpoints.
- **Harder:**
  - Every provider gains a fenced occurrence store, atomic schedule turns and a receipt table inside the event-sequence transaction. SQL needs migrations.
  - On MongoDB, every receipted effect and every schedule turn is a transaction.
  - The kernel clock becomes part of the semantics, so silo clocks must stay NTP-synchronized; a clock running fast fires early.
  - Developers must state a migration whenever a schedule changes.
- **Ruled out:**
  - exactly-once external effects;
  - strict head-of-line ordering, in v1;
  - local-time schedules, without a new decision;
  - infrastructure state in domain events or in specifications.
