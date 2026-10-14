---
id: 0007-idempotent-keyed-appends
title: Make keyed appends idempotent in the event sequence, and key reactor side effects by delivery
status: proposed
stage: none
class: contract
reversibility: costly
proposed: 2026-10-14
decider: woksin
applies-to:
  - Source/Kernel/Core/Sequences/**
  - Source/Kernel/Core/EventSequences/**
  - Source/Kernel/Storage/EventSequences/**
  - Source/Kernel/Storage.InMemory/EventSequences/**
  - Source/Kernel/Storage.MongoDB/EventSequences/**
  - Source/Kernel/Storage.Sql/EventStores/Namespaces/EventSequences/**
  - Source/Kernel/Contracts/**
  - Source/Kernel/Protobuf/**
  - Source/Kernel/Grpc/Clients/ConnectionService.cs
  - Source/Clients/DotNET/EventSequences/**
  - Source/Clients/DotNET/Reactors/**
  - Source/Clients/Testing/Reactors/**
  - Documentation/reactors/**
  - Documentation/clients/where-the-clients-differ.md
---

<!-- markdownlint-disable-next-line MD025 -->
# Make keyed appends idempotent in the event sequence, and key reactor side effects by delivery

Proposed on 2026-10-14 for Cratis/Chronicle#4809; woksin is the intended decider, pending acceptance.

## Context

A reactor that returns events has them appended by a built-in side-effect handler: `EventResultHandler.cs:54-60` (`Append`), `EventsResultHandler.cs:62-68`, `EventsForEventSourceIdResultHandler.cs:36`, `MixedSideEffectsResultHandler.cs:77` and `EventsWithConcurrencyScopesResultHandler.cs:29-31` (`AppendMany`), all under `Source/Clients/DotNET/Reactors/SideEffects/`. When the partition fails after that append, for example because Arc's `EventsAndCommandsResultHandler` then has a command refused, recovery delivers the event again and the handler appends the same events a second time. Nothing on the append path can stop it:

- `IEventSequence.Append` and `AppendMany` (`Source/Clients/DotNET/EventSequences/IEventSequence.cs:215-225, 244-254, 267-271`) and `EventForEventSourceId` (`EventForEventSourceId.cs:15-84`) have no field a caller can set to identify an event.
- The kernel `Append`, `AppendMany` and `AppendManyForEventSources` artifacts carry no identity either (`Source/Kernel/Core/Sequences/Append.cs:41-58`, `AppendMany.cs:30-41`, `AppendManyForEventSources.cs:28-37`, `EventForEventSourceId.cs:26-37`, `EventToAppend.cs:17`).
- `[OnceOnly]` only takes effect during replay (`Source/Clients/DotNET/Reactors/OnceOnlyAttribute.cs:7-19`).
- `ReactorDelivery` (`Source/Clients/DotNET/Reactors/ReactorDelivery.cs:38-55`) is stable across recovery and replay (`DotNET.Specs/Reactors/for_ReactorDelivery/when_the_event_is_replayed.cs:13-22`). It explicitly suppresses nothing (`ReactorDelivery.cs:25-31`). Its `Id` joins components with `#` (`Source/Infrastructure/KeyHelper.cs:18, 27`), so it is ambiguous when a partition contains `#`.

The retry window is effectively unbounded. The defaults are 10 attempts with a 600-second backoff cap, `0` means retry forever (`Source/Kernel/Core/Configuration/Observers.cs:45-66`), and an operator can recover a failed partition at any later time.

The kernel already has an idempotent append, built for projection sinks:

- `EventSequence.AppendPublication` (`Source/Kernel/Core/EventSequences/EventSequence.Publications.cs:15-52`) looks up the identity before any validation and returns the stored sequence number on a hit (`EventSequence.Validation.cs:48-56`). On a hit it also finishes the grain-side work that a crashed activation never did (`EventSequence.Validation.cs:143-179`).
- `IEventPublicationStorage` (`Source/Kernel/Storage/EventSequences/IEventPublicationStorage.cs:8-33`) requires the identity and fingerprint to be written in the same atomic write as the event.
  - MongoDB stores them on the event document (`Storage.MongoDB/EventSequences/Event.cs:70-75`) under a unique sparse index (`EventSequenceStorage.cs:1075-1082`) and resolves races (`EventSequenceStorage.Publications.cs:15-96`).
  - SQL stores the identity, its SHA-256 hash and the fingerprint on the event row, with a unique filtered index on the hash (`EventEntry.cs:100-118`, `EventSequenceDbContext.cs:95-97`, `EventSequenceMigrator.cs:61-72, 207-215`), and translates unique violations (`EventSequenceStorage.Publications.cs:39-97`).
  - InMemory checks under its lock (`Storage.InMemory/EventSequences/EventSequenceStorage.Publications.cs:12, 24-80`).
- Redaction updates the row in place and keeps both fields (MongoDB `EventSequenceStorage.cs:1124-1134`; SQL `EventSequenceStorage.cs:368-395`).
- The identity is single-event only. Batch storage appends never write it, and every MongoDB duplicate-key error in a batch is treated as a sequence-slot collision and renumbered (`Storage.MongoDB/EventSequences/EventSequenceStorage.cs:432-439, 1215-1225`). The single-event MongoDB insert does the same (`EventSequenceStorage.cs:273, 316-326`).

The content check from #4510 (`Source/Kernel/Core/Sequences/VerifyContent.cs:50-197`) answers `Unavailable` in several cases: on SQL, which does not support revision tracking (`VerifyContent.cs:54-57`, `Storage.Sql/.../EventSequenceStorage.cs:43`); after a revision or redaction (`VerifyContent.cs:72-75`); and when a migration inspects protected values (`VerifyContent.cs:128-133`). Each comparison also releases protected values.

Appends serialize inside the non-reentrant `EventSequence` grain; only read methods are `[AlwaysInterleave]` (`Source/Kernel/Core/EventSequences/IEventSequence.cs:61, 72, 83, 97`). A batch is all-or-nothing: validation failures stop it (`EventSequence.cs:359-385`), the concurrency check runs next (`EventSequence.cs:387-391`), and then a single storage transaction runs (`EventSequence.cs:625-720`; MongoDB session `EventSequenceStorage.cs:346-428`; SQL one `SaveChangesAsync`, `EventSequenceStorage.cs:274-367`).

This crosses every storage provider, the wire contract, the public client API and reactor semantics on replay. It therefore needs a durable decision. [Decision 0004](0004-ordered-lanes-observer-delivery.md) Q4 promises at-least-once delivery, not exactly-once effects. This decision makes the *appended* effects idempotent and leaves that promise unchanged.

## Decision

An append may carry an **idempotency key** per event. The event sequence grain settles the key against an identity stored **on the event itself**, in the same atomic write. This reuses and generalizes the publication identity.

- Same key and same intent: the append is reported as already done, with the stored sequence number. No new event, no error.
- Same key and different intent: the append is refused with `IdempotencyKeyConflict`. Nothing is overwritten.

Built-in reactor side-effect handlers key every returned event by delivery and effect index.

### 1. Key shape and who computes it

- `IdempotencyKey` is a client concept, `ConceptAs<string>`, between 1 and 512 characters. It has **no** implicit conversion from `string`, because a string literal would become ambiguous with the `EventStreamType` parameter (`Source/Clients/DotNET/Events/EventStreamType.cs:27`).
- The caller computes the key; the kernel never derives one. A key is scoped to one event store, namespace and event sequence, which is where the index lives.
- Reactor effects use `ReactorDelivery.IdempotencyKeyFor(int effectIndex)`. It is a length-prefixed SHA-256 over: reactor, event store, namespace, observed sequence, partition, triggering sequence number and effect index. This is the same encoding as `PublicationIdentity` (`Source/Kernel/Core/Sinks/PublicationIdentity.cs:31-40`), so it does not inherit `DeliveryId`'s `#` ambiguity.
- The effect index is the zero-based position of the event in the value the handler returned.

### 2. Wire and client surface

- **Core artifacts.** A nullable `IdempotencyKey` string is added at the end of `Append`, `AppendWithNamedTags`, `Sequences.EventToAppend`, `EventToAppendWithNamedTags`, `EventForEventSourceId` and `EventForEventSourceIdWithNamedTags`. Building Core regenerates the Contracts sources (`Core.csproj:51-53`), but Protobuf is generated by the Contracts build (`Contracts.csproj:51-56`), which in the same invocation ran before those sources existed. The sequence is therefore: build Core, then build Contracts again to regenerate the `.proto` files and `chronicle.desc`, then rebuild consumers and run the stale-output and wire-compatibility checks. `ProtoMemberIndexReader` assigns the next free field numbers.
- **Kernel results.**
  - `AppendResult` gains `Outcome` (`AppendOutcome`: `Appended` or `AlreadyAppended`).
  - `AppendManyResult` gains `Outcomes`, one entry per request event, aligned with `SequenceNumbers`.
  - Both gain `IdempotencyConflicts`, a list of (key, existing sequence number).
  - The refusal text travels in the existing `Errors` field (`AppendResponse` field 9, `Source/Kernel/Contracts/Sequences/IEventSequences.cs:419-423`) as `AppendError.IdempotencyKeyConflict` or `IdempotencyKeyRepeatedInBatch`.
- **Grain.** Keyed single appends get a new grain method; `EventToAppend` gains an init property. Existing grain signatures are not changed.
- **Capability.** The kernel advertises `KernelCapabilities.IdempotentAppends` (`"idempotent-appends"`) through `ConnectionService.cs:142`.
- **.NET client.**
  - `EventForEventSourceId.IdempotencyKey { get; init; }`.
  - Distinctly named methods `AppendOnce(EventSourceId, object, IdempotencyKey, ...)` and `AppendManyOnce(EventSourceId, IEnumerable<object>, IdempotencyKey, ...)`. New overloads of `Append` and `AppendMany` are not used: existing calls such as `Append(id, @event, null)` and `AppendMany(id, events, default)` bind to the `EventStreamType?` parameter today (`IEventSequence.cs:215-225, 244-254`), and a second reference-typed third parameter would make them ambiguous. A compilation spec covers those existing `null` and `default` call shapes. The batch form expands the key per event to `key.ForIndex(i)`.
  - These are default interface members that throw `IdempotentAppendsNotSupported`, following the `AppendPrepared` pattern (`IEventSequence.cs:67-77`).
  - Client `AppendResult` and `AppendManyResult` gain `Outcome`/`Outcomes` and `IdempotencyConflicts`.
  - If the connected kernel lacks the capability, a keyed append throws `IdempotentAppendsNotSupported` **before** sending, so a key is never silently ignored.

### 3. Where dedup is enforced, and why it is race-safe

The check runs in the grain turn, before constraint, schema and concurrency validation, mirroring `EventSequence.Validation.cs:48-56`. The stored identity is `append-key:` followed by the key. Sink identities are uppercase hex (`PublicationIdentity.cs:39`), so the two domains can never collide.

- **Concurrent duplicate deliveries to one activation.** They serialize in the non-reentrant grain. The second one finds the first one's durable identity.
- **Two activations (silo handover or split brain).** Both may miss in the lookup. Validation runs after the lookup (`EventSequence.Validation.cs:48-56, 76-86`; batch `EventSequence.cs:376-391`), so the loser can be refused by the concurrency check because it now sees the winner's event (`ConcurrencyValidator.cs:28-49`), or by a constraint, before it ever reaches the unique index. A keyed append that missed in the first lookup therefore **settles its keys again before returning any validation failure**: if the key is now durable, the result is `AlreadyAppended` (equal fingerprint) or `IdempotencyKeyConflict` (different), not the validation error. Events whose keys are still absent keep their validation result. When both reach storage, the store's unique index (MongoDB sparse unique, SQL filtered unique on the hash) or the InMemory lock lets exactly one write win, and the loser resolves the identity to the winner's receipt and compares fingerprints, as `Storage.MongoDB/.../EventSequenceStorage.Publications.cs:76-94` and `Storage.Sql/.../EventSequenceStorage.Publications.cs:73-88` already do for single appends. Deterministic interleaving specs cover both the single and the batch path.
- **Kernel crash between event and key.** This window does not exist: the key is part of the event document or row, and a batch is one transaction.
- **Crash after the write but before the grain finished.** Keyed settlement does **no** side work on a hit: it returns the stored receipt and nothing else. It does not reuse `ReconcileExistingPublication` (`EventSequence.Validation.cs:149-180`), which rebuilds constraint updates from the retry's content; without a durable ordering for claims and removals, no retry can apply those safely. The grain's sequence number and per-type tails are rebuilt from durable events on activation (`EventSequencesStorageProvider.cs:40-43, 87-93`), and observers read durable events from storage. A unique-constraint claim missing after a crash between the event write and the claim update is a gap shared by every append, keyed or not, and by publication retries; it is fixed by Cratis/Chronicle#4895, not here. Within one activation, a hit sets the grain's next sequence number to `max(State.SequenceNumber, receipt.SequenceNumber.Next())` before returning (a write can commit while its turn fails), matching `EventSequence.cs:887-891`, so the next append does not collide with it.
- **Batch storage.** A new storage method `AppendManyWithIdentities` returns `Result<IEnumerable<AppendedEvent>, AppendManyStorageFailure>`, where the failure is either `DuplicateEventSequenceNumber` (dispatched to the existing slot-renumbering loop, `EventSequence.cs:731-744`) or `IdentitiesAlreadyDurable` with the receipts found. The existing batch methods (`IEventSequenceStorage.cs:145, 153`) keep their contract. On a unique violation, each provider classifies the failure by looking up the batch's identities and the colliding slots, never by the violation predicate alone: SQL's `IsPublicationUniqueViolation` matches every unique violation (`Storage.Sql/.../EventSequenceStorage.Publications.cs:91-97`). On `IdentitiesAlreadyDurable` the grain classifies the batch again (section 6) and retries once.

### 4. Same-content comparison

The kernel computes a fingerprint from the request, before protection, because encryption is nondeterministic. It is `v1:` plus the SHA-256 of length-prefixed parts:

- Included: event type id and generation; the content as canonical JSON (ordinal-sorted keys, the writer in `EventHashCalculator.cs:44-75`); event source id; event source type, stream type and stream id after `AppendRoute.Resolve`; the event source definition name; and the subject (explicit, or else the event source id).
- Excluded: tags, named tags, occurred, correlation, causation and caused-by. These describe the attempt, not the fact. A retry that differs only in them is already done, and the stored values stand.

Comparison is ordinal equality of the fingerprints. The fingerprint is fixed at write time, so the answer never becomes `Unavailable`. The version prefix lets a future canonicalization recompute under the stored version.

### 5. Outcomes surfaced to clients

| Case | Result | Events written |
| --- | --- | --- |
| New key | `Appended` | yes |
| Durable key, equal fingerprint | `AlreadyAppended`, stored sequence number, `IsSuccess` | no |
| Durable key, different fingerprint | `IsSuccess = false`, error `IdempotencyKeyConflict`, conflict lists key and existing number | no |
| Same key twice in one request | `IsSuccess = false`, error `IdempotencyKeyRepeatedInBatch` | no |

An `AlreadyAppended` outcome performs no concurrency check (`ConcurrencyCheckPerformed = false`) and no constraint validation. The event already holds its values, as `EventSequence.Validation.cs:147` reasons for publications. `ReactorSideEffectFailure.FromAppendResult` (`Source/Clients/DotNET/Reactors/SideEffects/ReactorSideEffectFailure.cs:74`) turns a conflict into a failed partition.

### 6. AppendMany is settled per event

The grain looks up every keyed event in one storage call and classifies each event as new, done or conflicting.

- **Any conflict or repeated key:** the whole batch is refused and nothing is written.
- **All events done:** success with the stored numbers and no concurrency check.
- **Mixed batch:** only the new events go through validation (with `ConstraintBatchClaims`, `EventSequence.cs:359`), the concurrency check and one storage transaction, keeping their relative order. `SequenceNumbers` and `Outcomes` follow the request order and mix stored and new numbers.
- **Concurrency scopes on a mixed batch:** scopes are validated for the remainder against the current tail, by the normal rule. A remainder whose scope was overtaken by the batch's own earlier events is refused with a concurrency violation; that is the correct answer once the stream has moved.
- **Unkeyed events in a keyed batch:** always new.

### 7. Storage per provider

Every provider stores the identity in the publication fields that already exist. There is no new table, no new index and no migration.

- **MongoDB.** `PublicationId` and `PublicationFingerprint` on the event, under the existing `publication_identity` unique sparse index. The batch path writes them inside its session transaction and tells an identity duplicate apart from a slot duplicate by looking up the identity.
- **SQL (SQLite, PostgreSQL, SQL Server).** `PublicationIdentityHash`, `PublicationId` and `PublicationFingerprint`, under the existing filtered unique index on every event-sequence table. The batch path catches `DbUpdateException` and classifies it by lookup, as section 3 describes.
- **InMemory.** The existing `_publications` map, extended to batches under the same lock. It lasts exactly as long as the events, which is the provider's existing contract.

The key therefore survives a kernel restart for as long as the event exists, on MongoDB and SQL. A batch lookup method, `TryGetPublications`, is added to `IEventPublicationStorage`.

### 8. Replay, revision and redaction

- **Keys across replay.** A replayed delivery produces the same key by design (`when_the_event_is_replayed.cs:13-22`), so replaying a reactor that returns events no longer duplicates them.
- **Revision and redaction.** Both keep the identity and fingerprint on the row, so a retry after a redaction cannot bring the redacted content back.
- **Changed output on replay.** Revision and redaction rewind affected partitions (`EventSequence.cs:451, 488, 515, 1340-1349`), and a reactor may then produce different content under the same key. The kernel refuses the whole batch, and the built-in handlers fail the partition for every delivery kind, replay included, as handlers propagate batch failures today (`EventsForEventSourceIdResultHandler.cs:36-43`, `ReactorInvoker.cs:229-234`). Treating a replay conflict as handled would drop any new effects refused in the same batch while reporting success. A separate replay policy is open question 1; a specification covers a conflict alongside a new effect.

### 9. Retention (the maintainer's call)

| Option | Survives retry window | Replay protected | Cost and risk |
| --- | --- | --- | --- |
| **A. Keep for the life of the event** (as publications do) | always | yes | about 130 bytes per keyed event plus a sparse or filtered index entry; no job |
| B. On the row, cleared after a configured window by a background job | only within the window | only within the window | a manual partition recovery after the window duplicates again; a new job in every provider |
| C. Separate key store with time-to-live (MongoDB TTL index, SQL sweep) | only within the time-to-live | only within the time-to-live | MongoDB single appends need a multi-document transaction to stay atomic; new schema in every provider; same window hazard |
| D. Kept until no failed partition refers to the triggering event | yes | no | ties event storage to observer state |

**Recommendation: A.** The retry window has no bound (Context), and only A keeps the guarantee unconditionally. Any expiry (B, C or D) introduces a deduplication horizon after which recovery and replay can duplicate again; adopting one later is a contract revision that must define that horizon and the behavior after it, through a superseding decision. The decision is the maintainer's.

## Options considered

- **Compare content with `VerifyContent` (#4510).** Its `Unavailable` answer must mean either "done", which is unsafe, or "refuse", which would make every SQL retry fail; it also releases protected values on each check. Not taken. Its canonical comparison remains for explicit verification.
- **A dedicated idempotency table or collection.** It needs a second atomic write per append (a multi-document transaction on MongoDB singles) for the same semantics the event row already gives. Not taken.
- **New columns separate from the publication fields.** That means a migration on every SQL table and a second unique index for an identical contract; the domain prefix already separates the two producers. Not taken.
- **Client-computed fingerprint.** Kotlin, TypeScript and Elixir would each have to reproduce canonical JSON exactly. Not taken.
- **One key per batch instead of per event.** A partially retried or reshaped batch could not be settled event by event. Not taken; the batch key is only a client convenience that expands per index.
- **Making `DeliveryId` the key.** It is ambiguous on `#` and identifies a delivery, not an effect. Not taken.
- **Refusing all keyed batches that contain an already-done event.** Simpler, but the issue requires that a partially retried batch is not appended again. Not taken.

## Default if unanswered

Nothing ships. Reactor partition recovery and replay keep appending returned events again, Arc reactors that both produce events and run commands keep producing permanent duplicates, and Stage keeps refusing such reactions with STAGE-ESM-037.

## Timeline and scope

Five pull requests, in order. Each lists the specs that prove it.

1. **Storage: identities in batch appends, all providers (patch).** Add `EventToAppendToStorage.Identity` and `TryGetPublications`, add `AppendManyWithIdentities` returning `AppendManyStorageFailure` (`DuplicateEventSequenceNumber` or `IdentitiesAlreadyDurable`, classified by lookup), leaving the existing batch methods unchanged. Specs live in `Storage.InMemory.Specs`, `Storage.MongoDB.Specs` and `Storage.Sql.Specs` (SQLite and PostgreSQL fixtures), under `for_EventSequenceStorage/when_appending_many_with_identities/`:
   - `and_the_identities_are_written` (receipts resolve)
   - `and_one_identity_is_already_durable` (nothing written, reported as identity, not as a slot)
   - `and_two_batches_race_for_the_same_identity`
   - `and_a_sequence_slot_collides` (still `DuplicateEventSequenceNumber`)
   - `and_the_event_is_redacted` (identity survives)
   - `and_a_non_identity_unique_index_is_violated` (SQL: not reported as an identity)
2. **Kernel settlement (minor).** `AppendFingerprint`, the `IdempotencyKey` concept, `AppendOutcome`, the grain classification and the new grain method. Core.Specs:
   - `for_AppendFingerprint`: `when_properties_are_reordered` (equal), `when_only_tags_differ` (equal), `when_content_differs`, `when_the_generation_differs`
   - `for_EventSequence/when_appending_with_an_idempotency_key/`: `and_the_key_is_new`; `and_the_key_is_durable_with_the_same_content` (no storage append, no concurrency check); `and_the_key_is_durable_with_different_content`; `and_the_key_is_durable_at_the_next_sequence_number` and `and_the_key_is_durable_beyond_the_next_sequence_number` (number advanced in both); `and_the_key_is_durable_with_a_later_constraint_change` (no constraint mutation); `and_another_activation_appended_it_first` (settles instead of returning the concurrency violation); `and_it_equals_a_publication_identity`
   - `for_EventSequence/when_appending_many_with_idempotency_keys/`: `and_every_key_is_durable`; `and_some_keys_are_durable` (only the new ones appended, numbers in request order); `and_one_key_conflicts` (nothing appended); `and_a_key_repeats`; `and_a_concurrency_scope_covers_the_remainder`; `and_another_activation_wins_the_race`; `and_another_activation_wins_before_validation`
3. **Wire (minor).** Add the Core artifact fields and result properties, build Core, then build Contracts again so the `.proto` files and `chronicle.desc` are regenerated from the new sources, and commit what generation produces; add `KernelCapabilities.IdempotentAppends` and advertise it. Proven by the `wire-compatibility.yml` gates (stale check, every released minor still served), validator specs for key length, and regenerated Workbench proxies.
4. **.NET client API (minor).** The concept, `AppendOnce`/`AppendManyOnce`, `EventForEventSourceId.IdempotencyKey`, outcomes and the capability guard. DotNET.Specs `for_EventSequence/when_appending_with_an_idempotency_key/`:
   - `and_the_kernel_lacks_the_capability` (throws, nothing sent)
   - `and_a_batch_key_is_given` (expanded per index)
   - `and_the_response_reports_already_appended`
   - `for_IEventSequence/when_calling_existing_overloads_with_null_or_default` (compiles and binds as before)
5. **Reactors and testing parity (minor).** `ReactorContext.Delivery`, set by `ReactorInvoker` at `ReactorInvoker.cs:223-226, 254-257` from `ResolveDelivery` (`:183-193`); `ReactorDelivery.IdempotencyKeyFor`; keyed built-in handlers, used only when the capability is present; conflicts failing the partition on every delivery kind (section 8); and `ReactorScenario` parity, where `RecordingReactorSideEffectHandlers` records a redelivered keyed effect once and `ProducedEvent` carries the key. Specs:
   - DotNET.Specs `for_ReactorDelivery/when_deriving_an_effect_key/`: `and_the_partition_is_recovered` (same key); `and_the_event_is_replayed` (same key); `and_the_index_differs`; `and_the_partition_contains_the_separator` (unambiguous)
   - `for_EventsResultHandler/when_handling/and_keys_are_supported` (indices 0..n-1)
   - `for_EventsResultHandler/when_handling/and_a_replayed_effect_conflicts_beside_a_new_one` (partition fails, nothing written)
   - `for_ReactionInvoker/when_a_partition_fails_and_is_recovered/and_the_handler_returned_events` (same keys twice)
   - Testing.Specs `for_ReactorScenario/when_redelivering/with_returned_events`
   - Integration (`Integration/Client`, MongoDB and SQL runs, including SQL Server):
     - `when_a_reactor_partition_fails_after_appending_its_events`: one event per effect after recovery
     - `when_the_kernel_restarts_between_attempts`: the key still settles

   Documentation: `reactors/delivery-identity.mdx` (the built-in handlers are now idempotent), appending docs (keys and outcomes), and `clients/where-the-clients-differ.md` (the other clients have the wire fields but no derivation yet).

In scope: the event sequence append path, the three storage providers, the wire contract, the .NET client, reactor side effects and Chronicle.Testing.

Out of scope:

- Repairing unique-constraint claims after a crash between an event write and its claim update, and the stale constraint mutations publication retries apply today. Both are tracked in Cratis/Chronicle#4895.
- Arc's `EventsAndCommandsResultHandler`. Arc adopts `ReactorContext.Delivery` in its own repo; command idempotency stays Arc's concern.
- The transactional unit-of-work commit path. It inherits the per-event key through `EventForEventSourceId` but gets no new API.
- Key exposure on reads and event export.
- Derived APIs for the Kotlin, TypeScript and Elixir clients.

## Verification

**Done when:** in every provider, a keyed append retried after a partition failure, a kernel restart, a duplicate concurrent delivery or a lost response produces exactly one event and reports `AlreadyAppended` with its sequence number; the same key with different content is refused with nothing written; a keyed batch retried with extra events appends only the new ones; and a released-minor client against the new kernel and a new client against an old kernel both behave as they do today. Each new-behavior spec listed above is red before its change; the existing-behavior regression specs (such as the overload-binding spec) are green before and after.

## Consequences

- Reactors that return events become idempotent across recovery and replay without code changes, once the client and kernel both carry this. Arc and Stage get the seam they asked for. STAGE-ESM-037 can be lifted when the kernel advertises `idempotent-appends` and Arc keys its events.
- **Public API:** additive only, so the client and kernel both ship as minor. New `AppendOnce`/`AppendManyOnce` methods are default interface members (existing overload resolution unchanged), `EventForEventSourceId` gets an init property, results get new properties, and `ReactorContext` gets a new init property.
- **Wire:** new optional fields only. An old client never sends a key and sees no new behavior. An old kernel never advertises the capability, so a new client refuses a keyed append before sending; the built-in handlers fall back to today's unkeyed appends and log once per reactor.
- **Replay behavior changes.** A replayed reactor that once appended duplicates now appends nothing, and a reactor whose replayed output differs fails its partition with `IdempotencyKeyConflict` instead of appending a contradicting event; an operator resolves it, or a later replay policy (open question 1) does.
- **Storage:** keyed events carry about 130 bytes more and an index entry, kept for the life of the event under option A. No migration is needed.
- **Privacy:** the fingerprint is a plain SHA-256 that includes plaintext content. After crypto-shredding it could confirm a guessed low-entropy value. Publications already accept this (`EventSequenceSink.cs:137-143`), but reactor effects carry personal data more often (open question 3).
- **Analyzers:** none required. A reactor that appends through an injected `IEventLog` instead of returning events stays unprotected; documentation covers this, and an analyzer is optional (open question 5).
