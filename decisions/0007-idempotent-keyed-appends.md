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
  - Source/Kernel/Core/Schemas/IJsonSchemaMetadataValueHandler.cs
  - Source/Kernel/Core/Compliance/GDPR/PIICompliancePropertyValueHandler.cs
  - Source/Kernel/Core/ProtectedValues/**
  - Source/Kernel/Storage/EventSequences/**
  - Source/Kernel/Storage.InMemory/EventSequences/**
  - Source/Kernel/Storage.MongoDB/EventSequences/**
  - Source/Kernel/Storage.Sql/EventStores/Namespaces/EventSequences/**
  - Source/Kernel/Contracts/**
  - Source/Kernel/Protobuf/**
  - Source/Kernel/Grpc/Clients/ConnectionService.cs
  - Source/Clients/DotNET/EventSequences/**
  - Source/Clients/DotNET/Reactors/**
  - Source/Clients/DotNET.CodeAnalysis/**
  - Source/Clients/Testing/Reactors/**
  - Documentation/reactors/**
  - Documentation/clients/where-the-clients-differ.md
---

<!-- markdownlint-disable-next-line MD025 -->
# Make keyed appends idempotent in the event sequence, and key reactor side effects by delivery

Proposed on 2026-10-14 for Cratis/Chronicle#4809; woksin is the intended decider, pending acceptance. L0 resolved the open questions on woksin's behalf on 2026-10-14 (see Resolutions). The record stays Proposed until L0 accepts it.

## Context

A reactor that returns events has them appended by a built-in side-effect handler: `EventResultHandler.cs:54-60` (`Append`), `EventsResultHandler.cs:62-68`, `EventsForEventSourceIdResultHandler.cs:36`, `MixedSideEffectsResultHandler.cs:77` and `EventsWithConcurrencyScopesResultHandler.cs:29-31` (`AppendMany`), all under `Source/Clients/DotNET/Reactors/SideEffects/`. When the partition fails after that append, for example because Arc's `EventsAndCommandsResultHandler` then has a command refused, recovery delivers the event again and the handler appends the same events a second time. Nothing on the append path can stop it:

- `IEventSequence.Append` and `AppendMany` (`Source/Clients/DotNET/EventSequences/IEventSequence.cs:215-225, 244-254, 267-271`) and `EventForEventSourceId` (`EventForEventSourceId.cs:15-84`) have no field a caller can set to identify an event.
- The kernel `Append`, `AppendMany` and `AppendManyForEventSources` artifacts carry no identity either (`Source/Kernel/Core/Sequences/Append.cs:41-58`, `AppendMany.cs:30-41`, `AppendManyForEventSources.cs:28-37`, `EventForEventSourceId.cs:26-37`, `EventToAppend.cs:17`).
- `[OnceOnly]` only takes effect during replay (`Source/Clients/DotNET/Reactors/OnceOnlyAttribute.cs:7-19`).
- `ReactorDelivery` (`Source/Clients/DotNET/Reactors/ReactorDelivery.cs:38-55`) is stable across recovery and replay (`DotNET.Specs/Reactors/for_ReactorDelivery/when_the_event_is_replayed.cs:13-22`). It explicitly suppresses nothing (`ReactorDelivery.cs:25-31`). Its `Id` joins components with `#` (`Source/Infrastructure/KeyHelper.cs:18, 27`), so it is ambiguous when a partition contains `#`.

The retry window is effectively unbounded. The defaults are 10 attempts with a 600-second backoff cap, `0` means retry forever (`Source/Kernel/Core/Configuration/Observers.cs:45-66`), and an operator can recover a failed partition at any later time.

The kernel already has an idempotent append, built for projection sinks:

- `EventSequence.AppendPublication` (`Source/Kernel/Core/EventSequences/EventSequence.Publications.cs:15-52`) looks up the identity before any validation and returns the stored sequence number on a hit (`EventSequence.Validation.cs:48-56`). On a hit it also finishes the grain-side work that a crashed activation never did (`EventSequence.Validation.cs:143-179`).
- `IEventPublicationStorage` (`Source/Kernel/Storage/EventSequences/IEventPublicationStorage.cs:8-33`) requires the identity and fingerprint to be written in the same atomic write as the event, and compares fingerprints inside the store (SQL `EventSequenceStorage.Publications.cs:29-33`).
  - MongoDB stores them on the event document (`Storage.MongoDB/EventSequences/Event.cs:70-75`) under a unique sparse index (`EventSequenceStorage.cs:1075-1082`) and resolves races (`EventSequenceStorage.Publications.cs:15-96`).
  - SQL stores the identity, its SHA-256 hash and the fingerprint on the event row, with a unique filtered index on the hash (`EventEntry.cs:100-118`, `EventSequenceDbContext.cs:95-97`, `EventSequenceMigrator.cs:61-72, 207-215`), and translates unique violations (`EventSequenceStorage.Publications.cs:39-97`).
  - InMemory checks under its lock (`Storage.InMemory/EventSequences/EventSequenceStorage.Publications.cs:12, 24-80`).
- Redaction updates the row in place and keeps both fields (MongoDB `EventSequenceStorage.cs:1124-1134`; SQL `EventSequenceStorage.cs:368-395`).
- The identity is single-event only. Batch storage appends never write it, and every MongoDB duplicate-key error in a batch is treated as a sequence-slot collision and renumbered (`Storage.MongoDB/EventSequences/EventSequenceStorage.cs:432-439, 1215-1225`). The single-event MongoDB insert does the same (`EventSequenceStorage.cs:273, 316-326`).
- Publication fingerprints never cover protected values: a publication target whose schema carries compliance metadata is refused (`Source/Kernel/Core/Sinks/EventSequenceSinkFactory.cs:31-36, 48-53`).

The content check from #4510 (`Source/Kernel/Core/Sequences/VerifyContent.cs:50-197`) answers `Unavailable` in several cases: on SQL, which does not support revision tracking (`VerifyContent.cs:54-57`, `Storage.Sql/.../EventSequenceStorage.cs:43`); after a revision or redaction (`VerifyContent.cs:72-75`); and when a migration inspects protected values (`VerifyContent.cs:128-133`). Each comparison also releases protected values.

Compliance runs on the append path. `MakeEventCompliant` (`EventSequence.cs:1063-1069`) applies every protected property through `IJsonSchemaMetadataManager.Apply`, under the subject, or the event source id when no subject is set.

- `[PII]` values are encrypted under a per-subject key (`PIICompliancePropertyValueHandler.cs:48-57`).
- `[Encrypted]` values are encrypted under subject, namespace or global keys in a separate identifier space that has no erasure path (`EncryptedValueKeyIdentifiers.cs:10-33`, `EncryptedSubjectValueHandler.cs:19-26`, `PIIManager.cs:91-100`).
- Encryption is randomized. Every value gets a fresh AES-256-GCM data key and nonce (`Source/Kernel/Core/Compliance/Encryption.cs:45-67`, data key `:50`, nonce `:53`), and the data key is wrapped with RSA-OAEP (`:33, 107-114`).
- Crypto-shredding first records an erasure fence that keeps the destroyed key's public fingerprint, then deletes the key material (`PIIManager.cs:50-51`; `EncryptionKeyErasure.cs:27-33`; `EncryptionKey.cs:18-26`). After that, provisioning a key for the subject throws until a new lifecycle is explicitly allowed (`IEncryptionKeyStorage.cs:41, 53-56, 153-170`).
- By default the keys are stored in the kernel's own database (`Storage.MongoDB/Encryption/EncryptionKeyStorage.cs:20`).

Appends are serialized inside the non-reentrant `EventSequence` grain; only read methods are `[AlwaysInterleave]` (`Source/Kernel/Core/EventSequences/IEventSequence.cs:61, 72, 83, 97`). A batch is all-or-nothing:

- validation failures stop it (`EventSequence.cs:359-385`);
- the concurrency check runs next (`EventSequence.cs:387-391`);
- then a single storage transaction runs (`EventSequence.cs:625-720`; MongoDB session `EventSequenceStorage.cs:346-428`; SQL one `SaveChangesAsync`, `EventSequenceStorage.cs:274-367`).

This crosses every storage provider, the wire contract, the public client API, reactor semantics on replay and the guarantees of crypto-shredding, so it needs a durable decision. [Decision 0004](0004-ordered-lanes-observer-delivery.md) Q4 promises at-least-once delivery, not exactly-once effects. This decision makes the *appended* effects idempotent and leaves that promise unchanged.

## Decision

An append may carry an **idempotency key** per event. The event sequence grain settles the key against an identity stored **on the event itself**, in the same atomic write. This reuses and generalizes the publication identity.

- Same key and same intent: the append is reported as already done, with the stored sequence number. No new event, no error.
- Same key and different intent: the append is refused with `IdempotencyKeyConflict`. Nothing is overwritten.
- Same key after the subject was crypto-shredded: the parts that do not depend on erased keys are compared. If they are equal, the append is reported as already done (section 4).

Built-in reactor side-effect handlers key every returned event by delivery and effect index.

### 1. Key shape and who computes it

- `IdempotencyKey` is a client concept, `ConceptAs<string>`, between 1 and 512 characters. It has **no** implicit conversion from `string`, because a string literal would then be ambiguous with the `EventStreamType` parameter (`Source/Clients/DotNET/Events/EventStreamType.cs:27`).
- The caller computes the key; the kernel never derives one. A key is scoped to one event store, namespace and event sequence, which is where the index lives.
- Reactor effects use `ReactorDelivery.IdempotencyKeyFor(int effectIndex)`. It is a length-prefixed SHA-256 over: reactor, event store, namespace, observed sequence, partition, triggering sequence number and effect index. This is the same encoding as `PublicationIdentity` (`Source/Kernel/Core/Sinks/PublicationIdentity.cs:31-40`), so it does not inherit `DeliveryId`'s `#` ambiguity.
- The effect index is the zero-based position of the event in the value the handler returned.
- The key is stored in plaintext for the life of the event and is not affected by crypto-shredding. **A key must not contain personal data.** The reactor derivation hashes delivery components only, and the documentation states this rule for caller-chosen keys.

### 2. Wire and client surface

- **Core artifacts.** A nullable `IdempotencyKey` string is added at the end of `Append`, `AppendWithNamedTags`, `Sequences.EventToAppend`, `EventToAppendWithNamedTags`, `EventForEventSourceId` and `EventForEventSourceIdWithNamedTags`.
  - Building Core regenerates the Contracts sources (`Core.csproj:51-53`). Protobuf is generated by the Contracts build (`Contracts.csproj:51-56`), which ran earlier in the same invocation, before those sources existed.
  - The sequence is therefore: build Core, then build Contracts again to regenerate the `.proto` files and `chronicle.desc`, then rebuild consumers and run the stale-output and wire-compatibility checks.
  - `ProtoMemberIndexReader` assigns the next free field numbers.
- **Kernel results.**
  - `AppendResult` gains `Outcome` (`AppendOutcome`: `Appended`, `AlreadyAppended` or `AlreadyAppendedAfterErasure`).
  - `AppendManyResult` gains `Outcomes`, one entry per request event, aligned with `SequenceNumbers`.
  - Both gain `IdempotencyConflicts`. Each entry holds the key, the existing sequence number, the stored event type and generation, the requested generation, and a reason: `Content`, `Generation` or `Unverifiable`.
  - Refusals travel in the existing `Errors` field (`AppendResponse` field 9, `Source/Kernel/Contracts/Sequences/IEventSequences.cs:419-423`) as a code followed by one explanatory sentence (section 5). The codes are `AppendError.IdempotencyKeyConflict`, `IdempotencyKeyRepeatedInBatch`, `IdempotencyKeyUnverifiable` and `IdempotencyFingerprintUnsupported`.
- **Grain.** Keyed single appends get a new grain method; `EventToAppend` gains an init property. Existing grain signatures are not changed.
- **Capability.** The kernel advertises `KernelCapabilities.IdempotentAppends` (`"idempotent-appends"`) through `ConnectionService.cs:142`.
- **.NET client.**
  - `EventForEventSourceId.IdempotencyKey { get; init; }`.
  - Distinctly named methods `AppendOnce(EventSourceId, object, IdempotencyKey, ...)` and `AppendManyOnce(EventSourceId, IEnumerable<object>, IdempotencyKey, ...)`. New overloads of `Append` and `AppendMany` are not used:
    - existing calls such as `Append(id, @event, null)` and `AppendMany(id, events, default)` bind to the `EventStreamType?` parameter today (`IEventSequence.cs:215-225, 244-254`);
    - a second reference-typed third parameter would make those calls ambiguous;
    - a compilation spec covers those existing `null` and `default` call shapes.
  - The batch form expands the key per event to `key.ForIndex(i)`.
  - These are default interface members that throw `IdempotentAppendsNotSupported`, following the `AppendPrepared` pattern (`IEventSequence.cs:67-77`).
  - Client `AppendResult` and `AppendManyResult` gain `Outcome`/`Outcomes` and `IdempotencyConflicts`. Both `AlreadyAppended` outcomes count as `IsSuccess`.
  - If the connected kernel lacks the capability, a keyed append throws `IdempotentAppendsNotSupported` **before** sending, so a key is never silently ignored.

### 3. Where dedup is enforced, and why it is race-safe

The check runs in the grain turn, before constraint, schema, compliance and concurrency validation, mirroring `EventSequence.Validation.cs:48-56`. The stored identity is `append-key:` followed by the key. Sink identities are uppercase hex (`PublicationIdentity.cs:39`), so the two domains can never collide. **Fingerprints of keyed appends are compared in the grain, never in storage** (section 4). Storage returns the stored fingerprint, event type and appended generation with the receipt. Keyed single appends use the same storage method as keyed batches, with one event, so `AppendPublication`'s in-store ordinal comparison stays with publication sinks.

- **Concurrent duplicate deliveries to one activation.** They serialize in the non-reentrant grain. The second one finds the first one's durable identity.
- **Two activations (silo handover or split brain).** Both may miss in the lookup.
  - Validation runs after the lookup (`EventSequence.Validation.cs:48-56, 76-86`; batch `EventSequence.cs:376-391`). The loser can therefore be refused by a constraint, or by the concurrency check because it now sees the winner's event (`ConcurrencyValidator.cs:28-49`), before it ever reaches the unique index.
  - A keyed append that missed in the first lookup therefore **settles its keys again before returning any validation failure**. If the key is now durable, the result is the settlement outcome of section 4, not the validation error. Events whose keys are still absent keep their validation result.
  - When both appends reach storage, the store's unique index (MongoDB sparse unique, SQL filtered unique on the hash) or the InMemory lock lets exactly one write win.
  - The loser resolves the identity to the winner's receipt, as `Storage.MongoDB/.../EventSequenceStorage.Publications.cs:76-94` and `Storage.Sql/.../EventSequenceStorage.Publications.cs:73-88` already do for single appends, and the grain compares fingerprints by section 4.
  - Deterministic interleaving specs cover both the single and the batch path.
- **Kernel crash between event and key.** This window does not exist: the key is part of the event document or row, and a batch is one transaction.
- **Crash after the write but before the grain finished.** Keyed settlement does **no** side work on a hit. It returns the stored receipt, and provisions no encryption key.
  - It does not reuse `ReconcileExistingPublication` (`EventSequence.Validation.cs:149-180`). That method rebuilds constraint updates from the retry's content, and without a durable ordering for claims and removals no retry can apply those safely.
  - The grain's sequence number and per-type tails are rebuilt from durable events on activation (`EventSequencesStorageProvider.cs:40-43, 87-93`), and observers read durable events from storage.
  - A missing unique-constraint claim after a crash between the event write and the claim update affects every append, keyed or not, and publication retries too. Cratis/Chronicle#4895 fixes it; this decision does not.
  - Within one activation, a hit sets the grain's next sequence number to `max(State.SequenceNumber, receipt.SequenceNumber.Next())` before returning, matching `EventSequence.cs:887-891`. A write can commit while its turn fails, so without this the next append could collide with it.
- **Batch storage.** A new storage method `AppendManyWithIdentities` returns `Result<IEnumerable<AppendedEvent>, AppendManyStorageFailure>`. The failure is one of:
  - `DuplicateEventSequenceNumber`, dispatched to the existing slot-renumbering loop (`EventSequence.cs:731-744`);
  - `IdentitiesAlreadyDurable`, with the receipts found (sequence number, stored fingerprint, event type, appended generation).

  The existing batch methods (`IEventSequenceStorage.cs:145, 153`) keep their contract. On a unique violation, each provider classifies the failure by looking up the batch's identities and the colliding slots, never by the violation predicate alone, because SQL's `IsPublicationUniqueViolation` matches every unique violation (`Storage.Sql/.../EventSequenceStorage.Publications.cs:91-97`). On `IdentitiesAlreadyDurable` the grain classifies the batch again (section 6) and retries once.

### 4. Same-content comparison, and what survives crypto-shredding

The kernel computes the fingerprint from the request before protection. It is fixed at write time, so the answer never becomes `Unavailable` the way `VerifyContent` can.

**What it covers.**

- Included:
  - event type id and generation;
  - the content as canonical JSON (ordinal-sorted keys, the writer in `EventHashCalculator.cs:44-75`);
  - event source id;
  - event source type, stream type and stream id after `AppendRoute.Resolve`;
  - the event source definition name;
  - the subject (explicit, or else the event source id).
- Excluded: tags, named tags, occurred, correlation, causation and caused-by. These describe the attempt, not the fact. A retry that differs only in them is already done, and the stored values stand.

**How protected values enter it.** The content is walked with the traversal compliance itself uses (`JsonSchemaMetadataManager.cs:261-276, 297-384`). Every value at a protected boundary is replaced in the canonical JSON by a marker naming its protection, and set aside with its JSON path. The fingerprint then has two kinds of part:

- **The plain part.** A SHA-256 over the length-prefixed fields listed above, with the masked content. It records the path and protection kind of every protected value, but no protected value.
- **One protected part per protecting key.** An HMAC-SHA-256 over the length-prefixed (path, canonical value) pairs that key protects, in document order.
  - The HMAC key is derived with HKDF-SHA-256 from the private part of that encryption key (`EncryptionKey.cs:15`), with the info label `cratis-chronicle/append-fingerprint/v1`. It is derived when needed and never stored.
  - Each protected part is stored with a reference to its key: the key's public fingerprint (`EncryptionKey.cs:18-26`), the same value an erasure fence records (`EncryptionKeyErasure.cs:28`). The reference names key material without holding any.
  - `[PII]` values belong to the subject's PII key (identifier as in `EventSequence.cs:1065`). `[Encrypted]` values belong to their subject, namespace or global key (`EncryptedValueKeyIdentifiers.cs`).
  - Each value handler reports where its key lives through a new `IJsonSchemaMetadataValueHandler` member. That member reads with `TryGetFor` and never provisions.
  - Some boundaries cannot be fingerprinted: a handler that reports no key location, or a boundary under more than one protection (which `ValidateComparisonBoundary` also refuses, `JsonSchemaMetadataManager.cs:277-291`, and CHR0053 flags at compile time). A keyed append whose event type has such a boundary is refused with `IdempotencyFingerprintUnsupported` before anything is written.

The stored form is `v1:` and the plain part in hex. Then, for each protecting key, in the fixed order `pii`, `encrypted-subject`, `encrypted-namespace`, `encrypted-global`, it appends `;`, the kind, `:`, the key reference, `=` and the HMAC in hex. Content without protected values stores only the plain part. On a miss, the fingerprint is computed after `MakeEventCompliant`, under the key the values were just encrypted with. Nothing is provisioned for the fingerprint alone. The version prefix lets a future canonicalization recompute under the stored version.

**Comparison on a hit.** The grain compares in this order:

1. The plain parts must be equal and the set of protection kinds must match. Otherwise the result is `IdempotencyKeyConflict`.
2. For each stored protected part, the grain reads the current key for that location with `TryGetFor`. It never calls `EnsureKeyFor` or `GetOrAddFor`, so a hit never provisions a key.
   - If the current key's public fingerprint equals the stored reference, the grain recomputes the HMAC over the retry's values. Equal means the part is **verified**; different means `IdempotencyKeyConflict`.
   - Otherwise, if an erasure is recorded for that key's identifier (`IEncryptionKeyStorage.GetErasureFor`, `:131-132`), the part is **erased**. This covers a shredded subject and a subject that has since been given a new lifecycle (`AllowNewKeyFor`), whose new key cannot reproduce the old part.
   - Otherwise the key is missing or replaced and no erasure is recorded, and the part is **unverifiable**.
   - A key storage failure (for example `EncryptionKeyStorageUnavailable`) propagates as an ordinary append failure. Nothing is written, and the caller retries later.
3. The outcome follows from the parts:
   - every part verified: `AlreadyAppended`;
   - plain part equal, at least one part erased, the rest verified: `AlreadyAppendedAfterErasure`;
   - any part unverifiable: refused with `IdempotencyKeyUnverifiable`.

**Why a retry after shredding reports success.** After erasure, nobody can tell whether the retry's personal values equal the erased ones; that is what shredding is for. Everything else about the event matched exactly. Both alternatives are worse:

- Refusing would fail the partition permanently over data the law requires to be unknowable.
- Appending would write the subject's personal data again, which undoes the erasure. It is also refused anyway unless a new lifecycle was authorized (`IEncryptionKeyStorage.cs:41`).

The answer is the same whatever personal values the retry carries, so it confirms nothing about them. The kernel logs the settlement with the key and sequence number and no values.

**Why this construction (ruling 3).** Each candidate was checked against "the fingerprint must not survive crypto-shredding".

| Construction | Stable for identical input | After the subject is shredded | Verdict |
| --- | --- | --- | --- |
| Plain SHA-256 over plaintext (the earlier text) | yes | anyone who can read the row can confirm a guessed value | refused by the ruling |
| Hash of the stored, encrypted form | **no**: fresh data key and nonce per value (`Encryption.cs:50, 53`) and randomized OAEP wrap (`:33, 113`), so every retry would conflict. Recomputing would also need encryption, which is refused after erasure (`IEncryptionKeyStorage.cs:41`) | no comparison is defined | not viable |
| HMAC keyed per event store | yes | the key is not erased with the subject's. The kernel, and anyone who holds the database and the key store (by default the same database), can still confirm guesses. The kernel itself becomes an online oracle: a keyed retry answers `AlreadyAppended` or a conflict for a guessed value, and reactor keys can be computed from delivery components | **weaker than the requirement** |
| Plain part plus HMAC under a key derived from each protecting key | yes | the derived key goes with the subject's key, so nobody can recompute the part, and the outcome is the same for every guess | **chosen** |

Costs of the chosen construction:

- A key read (cached, `CacheEncryptionKeyStorage.cs:158-179`) on each hit that has protected values, plus an erasure lookup when the reference no longer matches.
- A new member on the four value handlers.
- About 135 bytes per protecting key on the fingerprint.
- A third outcome on the wire.
- After shredding, the comparison covers only the plain part, so a retry whose personal values differ is not detected. This is by design.

Residual exposure:

- Before shredding, a holder of the subject's key can confirm a guess, but that holder can decrypt the ciphertext anyway.
- A client allowed to append with the key can probe equality, and the same client can already read the event with its values released (`EventCompliance.cs:21-33`).
- A key-store backup that still holds the key undoes shredding for the ciphertext and the fingerprint alike.

### 5. Outcomes surfaced to clients

| Case | Result | Events written |
| --- | --- | --- |
| New key | `Appended` | yes |
| Durable key, every fingerprint part verified | `AlreadyAppended`, stored sequence number, `IsSuccess` | no |
| Durable key, plain part equal, protected part erased with the subject's key | `AlreadyAppendedAfterErasure`, stored sequence number, `IsSuccess` | no, and no key is provisioned |
| Durable key, different generation | `IsSuccess = false`, `IdempotencyKeyConflict`, reason `Generation` | no |
| Durable key, any other difference | `IsSuccess = false`, `IdempotencyKeyConflict`, reason `Content` | no |
| Durable key, protecting key missing and no erasure recorded | `IsSuccess = false`, `IdempotencyKeyUnverifiable` | no |
| Same key twice in one request | `IsSuccess = false`, `IdempotencyKeyRepeatedInBatch` | no |
| Event type with an unsupported protected boundary | `IsSuccess = false`, `IdempotencyFingerprintUnsupported` | no |

An `AlreadyAppended` or `AlreadyAppendedAfterErasure` outcome performs no concurrency check (`ConcurrencyCheckPerformed = false`) and no constraint validation. The event already holds its values, as `EventSequence.Validation.cs:147` reasons for publications. `ReactorSideEffectFailure.FromAppendResult` (`Source/Clients/DotNET/Reactors/SideEffects/ReactorSideEffectFailure.cs:74`) turns every refusal into a failed partition, and its messages (`:47-51`) carry the kernel's sentence verbatim.

**Refusal sentences.** These are exact templates; specs assert them.

- Generation (ruling 4): `Idempotency key '{key}' already appended event type '{eventType}' generation {storedGeneration} at sequence number {sequenceNumber}; this append is generation {requestedGeneration}. Keyed appends are not migrated between generations. To repeat the recorded effect, append generation {storedGeneration}; for a new effect, use a new idempotency key. Nothing was written.`
- Content: `Idempotency key '{key}' already appended event type '{eventType}' generation {storedGeneration} at sequence number {sequenceNumber} with a different event source, route or content. Nothing was written.`
- Unverifiable: `Idempotency key '{key}' already appended event type '{eventType}' at sequence number {sequenceNumber}, but the key that protected its {pii|encrypted} values is no longer in key storage and no erasure is recorded for it, so this retry cannot be compared. Nothing was written. Restore the key from a backup, or, for personal data that is meant to be gone, erase the subject; then recover the partition.` Erasing records a fence even when no key is present (`EncryptionKeyErasure.cs:49-51`).

No sentence contains a protected value. The reactor key is a hash, and caller keys must not hold personal data (section 1).

### 6. AppendMany is settled per event

The grain looks up every keyed event in one storage call and classifies each event as new, done (including done after erasure), conflicting or unverifiable.

- **Any conflict, unverifiable key, unsupported boundary or repeated key:** the whole batch is refused and nothing is written.
- **All events done:** success with the stored numbers and no concurrency check.
- **Mixed batch:** only the new events go through validation (with `ConstraintBatchClaims`, `EventSequence.cs:359`), the concurrency check and one storage transaction, keeping their relative order. `SequenceNumbers` and `Outcomes` follow the request order and mix stored and new numbers.
- **Concurrency scopes on a mixed batch:** scopes are validated for the remainder against the current tail, by the normal rule. A remainder whose scope was overtaken by the batch's own earlier events is refused with a concurrency violation; that is the correct answer once the stream has moved.
- **Unkeyed events in a keyed batch:** always new.

### 7. Storage per provider

Every provider stores the identity in the publication fields that already exist. There is no new table, no new index and no migration.

- **MongoDB.** `PublicationId` and `PublicationFingerprint` on the event, under the existing `publication_identity` unique sparse index. The batch path writes them inside its session transaction and distinguishes an identity duplicate from a slot duplicate by looking up the identity.
- **SQL (SQLite, PostgreSQL, SQL Server).** `PublicationIdentityHash`, `PublicationId` and `PublicationFingerprint`, under the existing filtered unique index on every event-sequence table. `PublicationFingerprint` is an unbounded string column (`EventSequenceMigrator.cs:151, 212`), so the longer protected form fits. The batch path catches `DbUpdateException` and classifies it by lookup, as section 3 describes.
- **InMemory.** The existing `_publications` map, extended to batches under the same lock. It lasts exactly as long as the events, which is the provider's existing contract.

The key therefore survives a kernel restart for as long as the event exists, on MongoDB and SQL. A batch lookup method, `TryGetPublications`, is added to `IEventPublicationStorage`. For `append-key:` identities it returns the receipt with the stored fingerprint, event type (`EventEntry.cs:39`) and appended generation (`EventEntry.cs:44`; MongoDB `Event.cs:49`), and compares nothing.

### 8. Replay, revision and redaction

- **Keys across replay.** A replayed delivery produces the same key by design (`when_the_event_is_replayed.cs:13-22`), so replaying a reactor that returns events no longer duplicates them.
- **Revision and redaction.** Both keep the identity and fingerprint on the row, so a retry after a redaction cannot bring the redacted content back.
- **Changed output on replay (ruling 2).** Revision and redaction rewind affected partitions (`EventSequence.cs:451, 488, 515, 1340-1349`), and a reactor may then produce different content under the same key.
  - The kernel refuses the whole batch. The built-in handlers fail the partition for every delivery kind, replay included, as handlers already propagate batch failures (`EventsForEventSourceIdResultHandler.cs:36-43`, `ReactorInvoker.cs:229-234`). This fails closed.
  - The failed partition's messages carry the refusal sentence of section 5, so the reason names the key, the stored sequence number and, for a generation change, both generations.
  - A conflict is deterministic, so retries repeat it until an operator acts.
  - Treating a replay conflict as handled would drop any new effects refused in the same batch while reporting success. A specification covers a conflict alongside a new effect.

### 9. Retention (resolved: life of the event)

| Option | Survives retry window | Replay protected | Cost and risk |
| --- | --- | --- | --- |
| **A. Keep for the life of the event** (as publications do) | always | yes | about 140 bytes per keyed event, plus about 135 per protecting key, plus a sparse or filtered index entry; no job |
| B. On the row, cleared after a configured window by a background job | only within the window | only within the window | a manual partition recovery after the window duplicates again; a new job in every provider |
| C. Separate key store with time-to-live (MongoDB TTL index, SQL sweep) | only within the time-to-live | only within the time-to-live | MongoDB single appends need a multi-document transaction to stay atomic; new schema in every provider; same window hazard |
| D. Kept until no failed partition refers to the triggering event | yes | no | ties event storage to observer state |

**Resolved: A** (ruling 1). The retry window has no bound (Context), and only A keeps the guarantee unconditionally. Any expiry (B, C or D) would introduce a deduplication horizon after which recovery and replay can duplicate again. Adopting one later is a contract revision that must define that horizon and the behavior after it, through a superseding decision.

### 10. Analyzer

A concrete, precise rule exists today, so one ships (ruling 5): **CHR0062**, `ReactorAppendsWithoutIdempotencyKey`, Warning. CHR0061 is the last id in use (`DiagnosticIds.cs:310`). The number is confirmed when the analyzer is added.

- **Where it reports.** On an invocation inside a reactor handler method, including lambdas and local functions in it. A reactor handler method belongs to a type implementing `IReactor` (`WellKnownTypes.ImplementsIReactor`) and has an event type as its first parameter, which is the discovery rule in `ReactorMethodAnalyzer.cs:76-103`.
- **What it reports.** An append-family method declared on `IEventSequence`, which includes `IEventLog` (`IEventLog.cs:9`): `Append`, `AppendMany`, `AppendThroughEventSource`, `AppendManyThroughEventSource`, `AppendWithNamedTags`, `AppendManyWithNamedTags`, `AppendPrepared` and `AppendPreparedThroughEventSource`.
- **What it excludes.** `AppendOnce`, `AppendManyOnce`, and every overload taking `IEnumerable<EventForEventSourceId>` (`IEventSequence.cs:267-271, 407-412`), whose items may carry keys the analyzer cannot see.
- **Message.** `Reactor '{0}' appends directly with '{1}' in handler '{2}'. A recovered or replayed delivery runs the handler again and appends again. Return the events from the handler, or call AppendOnce with a key from ReactorDelivery.IdempotencyKeyFor.` Returning events works against every kernel. `AppendOnce` needs a kernel that advertises `idempotent-appends`.
- **Why it is not noisy.** Every hit is an unkeyed append in code that Chronicle runs again on recovery and replay. `[OnceOnly]` does not exempt a hit, because it does not cover recovery. The rule cannot see appends in helpers or injected services; those are false negatives, not noise. In this repository it fires once, on `Integration/Client/for_EventAppendCollection/AReactorThatDirectlyAppendsUniqueEvent.cs:11-14`, which exercises a direct append on purpose and gets a suppression.
- **What it does not do.** It does not flag constructor injection of `IEventLog` the way CHR0015 does for projections (`ProjectionSideEffectAnalyzer.cs:16-24`). Reactors may inject it only to read, so that rule would be noisy.
- **CHR0022.** CHR0022 (`ReactorOnceOnlyAnalyzer.cs:16-24`) says returned events are appended again on replay. That stays true only against kernels without `idempotent-appends`. Its description is updated to say so and to give the remaining reason for `[OnceOnly]`: a replay whose output changed fails the partition. Its severity is unchanged.

## Options considered

- **Compare content with `VerifyContent` (#4510).** Its `Unavailable` answer must mean either "done", which is unsafe, or "refuse", which would make every SQL retry fail. It also releases protected values on each check. Not taken. Its canonical comparison remains for explicit verification.
- **Plain SHA-256 fingerprint over plaintext content.** After crypto-shredding it confirms guessed personal data. Refused by ruling 3.
- **Fingerprint over the stored, encrypted form.** Not stable for identical inputs (`Encryption.cs:50, 53, 113`). Recomputing it on a retry also needs encryption, which is refused for an erased subject. Making encryption deterministic would change the at-rest format and leak equality through the ciphertext itself. Not taken.
- **HMAC keyed per event store.** Crypto-shredding erases per-subject keys, not an event-store key. Whoever holds that key, including the kernel through the append path, can still confirm guesses after erasure. This is weaker than ruling 3 requires. Not taken, in favor of section 4.
- **Refuse a retry whose protected part was erased.** It fails the partition forever over an erasure that was lawful and deliberate. Not taken; `AlreadyAppendedAfterErasure` instead.
- **Implicit migration of a retry to the stored generation.** Not in v1 (ruling 4). It would need migrations on the retry path and a definition of equality across generations. It is added only if a real consumer needs it.
- **A dedicated idempotency table or collection.** It needs a second atomic write per append (a multi-document transaction on MongoDB singles) for the same semantics the event row already gives. Not taken.
- **New columns separate from the publication fields.** That means a migration on every SQL table and a second unique index for an identical contract; the domain prefix already separates the two producers. Not taken.
- **Client-computed fingerprint.** Kotlin, TypeScript and Elixir would each have to reproduce canonical JSON exactly, and clients do not hold encryption keys. Not taken.
- **One key per batch instead of per event.** A partially retried or reshaped batch could not be settled event by event. Not taken; the batch key is only a client convenience that expands per index.
- **Making `DeliveryId` the key.** It is ambiguous on `#` and identifies a delivery, not an effect. Not taken.
- **Refusing all keyed batches that contain an already-done event.** Simpler, but the issue requires that a partially retried batch is not appended again. Not taken.
- **Expiring keys (retention B, C, D).** They reopen duplicates after a horizon. Not taken (section 9).
- **An analyzer on reactor constructors that inject `IEventLog`.** Noisy, because injection is also used for reads. Not taken (section 10).

## Default if unanswered

Nothing ships. Reactor partition recovery and replay keep appending returned events again, Arc reactors that both produce events and run commands keep producing permanent duplicates, and Stage keeps refusing such reactions with STAGE-ESM-037.

## Timeline and scope

Five pull requests, in order. Each lists the specs that prove it. Each new-behavior spec is red before its change.

1. **Storage: identities in batch appends, all providers (patch).** Add `EventToAppendToStorage.Identity` and `TryGetPublications`. Add `AppendManyWithIdentities` returning `AppendManyStorageFailure` (`DuplicateEventSequenceNumber` or `IdentitiesAlreadyDurable`, classified by lookup), leaving the existing batch methods unchanged. Keyed receipts carry the stored fingerprint, event type and appended generation. Specs live in `Storage.InMemory.Specs`, `Storage.MongoDB.Specs` and `Storage.Sql.Specs` (SQLite and PostgreSQL fixtures), under `for_EventSequenceStorage/when_appending_many_with_identities/`:
   - `and_the_identities_are_written` (receipts resolve)
   - `and_one_identity_is_already_durable` (nothing written; reported as an identity, not a slot; receipt carries fingerprint, type and generation)
   - `and_the_stored_fingerprint_differs` (storage reports, does not refuse)
   - `and_two_batches_race_for_the_same_identity`
   - `and_a_sequence_slot_collides` (still `DuplicateEventSequenceNumber`)
   - `and_the_event_is_redacted` (identity survives)
   - `and_a_non_identity_unique_index_is_violated` (SQL: not reported as an identity)
2. **Kernel settlement and fingerprint (minor).** This PR adds:
   - `AppendFingerprint` with its plain and protected parts;
   - the key-location member on the four value handlers;
   - the `IdempotencyKey` concept and `AppendOutcome`;
   - the grain classification and the new grain method;
   - the refusal sentences.

   Core.Specs:
   - `for_AppendFingerprint`:
     - `when_properties_are_reordered` (equal)
     - `when_only_tags_differ` (equal)
     - `when_content_differs`
     - `when_the_generation_differs`
     - `when_a_pii_value_differs` (plain part equal, protected part differs)
     - `when_an_encrypted_value_differs`
     - `when_the_content_carries_pii` (the stored form contains no digest computable without the key; a SHA-256 over the guessed plaintext matches no part)
     - `when_a_boundary_carries_two_protections` (unsupported)
   - `for_AppendFingerprint/when_comparing_after_the_key_changed/`:
     - `and_the_subject_was_erased` (erased)
     - `and_a_new_lifecycle_was_allowed` (erased, not a conflict)
     - `and_the_key_was_deleted_without_an_erasure` (unverifiable)
     - `and_the_key_storage_is_unavailable` (throws)
   - `for_EventSequence/when_appending_with_an_idempotency_key/`:
     - `and_the_key_is_new`
     - `and_the_key_is_durable_with_the_same_content` (no storage append, no concurrency check)
     - `and_the_key_is_durable_with_different_content` (content sentence)
     - `and_the_key_is_durable_with_another_generation` (exact generation sentence; reason `Generation`; nothing written)
     - `and_the_subject_was_erased_with_the_same_non_personal_content` (`AlreadyAppendedAfterErasure`; no key provisioned; nothing written)
     - `and_the_subject_was_erased_and_non_personal_content_differs` (conflict)
     - `and_the_subject_was_erased_and_a_guessed_value_is_retried` (same outcome for the true value and a wrong guess)
     - `and_the_protecting_key_is_missing_without_an_erasure` (unverifiable sentence)
     - `and_the_key_storage_is_unavailable` (failure, nothing written)
     - `and_the_key_is_durable_at_the_next_sequence_number` and `and_the_key_is_durable_beyond_the_next_sequence_number` (number advanced in both)
     - `and_the_key_is_durable_with_a_later_constraint_change` (no constraint mutation)
     - `and_another_activation_appended_it_first` (settles instead of returning the concurrency violation)
     - `and_it_equals_a_publication_identity`
   - `for_EventSequence/when_appending_many_with_idempotency_keys/`:
     - `and_every_key_is_durable`
     - `and_some_keys_are_durable` (only the new ones appended, numbers in request order)
     - `and_one_key_conflicts` (nothing appended)
     - `and_one_key_is_unverifiable` (nothing appended)
     - `and_one_subject_was_erased` (settled as done after erasure, remainder appended)
     - `and_a_key_repeats`
     - `and_a_concurrency_scope_covers_the_remainder`
     - `and_another_activation_wins_the_race`
     - `and_another_activation_wins_before_validation`
3. **Wire (minor).** Add the Core artifact fields, the three-valued `AppendOutcome`, the conflict details and the four error codes. Build Core, then build Contracts again so the `.proto` files and `chronicle.desc` are regenerated from the new sources, and commit what generation produces. Add `KernelCapabilities.IdempotentAppends` and advertise it. Proven by:
   - the `wire-compatibility.yml` gates (stale check, every released minor still served);
   - validator specs for key length;
   - regenerated Workbench proxies.
4. **.NET client API (minor).** The concept, `AppendOnce`/`AppendManyOnce`, `EventForEventSourceId.IdempotencyKey`, outcomes and the capability guard. DotNET.Specs `for_EventSequence/when_appending_with_an_idempotency_key/`:
   - `and_the_kernel_lacks_the_capability` (throws, nothing sent)
   - `and_a_batch_key_is_given` (expanded per index)
   - `and_the_response_reports_already_appended`
   - `and_the_response_reports_already_appended_after_erasure` (`IsSuccess`)
   - `and_the_response_reports_a_generation_conflict` (details and sentence surfaced)
   - `for_IEventSequence/when_calling_existing_overloads_with_null_or_default` (compiles and binds as before; green before and after)
5. **Reactors, analyzer and testing parity (minor).** This PR adds:
   - `ReactorContext.Delivery`, set by `ReactorInvoker` at `ReactorInvoker.cs:223-226, 254-257` from `ResolveDelivery` (`:183-193`);
   - `ReactorDelivery.IdempotencyKeyFor`;
   - keyed built-in handlers, used only when the capability is present;
   - refusals failing the partition on every delivery kind, with the refusal sentence as the reason (section 8);
   - CHR0062 and the revised CHR0022 description;
   - `ReactorScenario` parity: `RecordingReactorSideEffectHandlers` records a redelivered keyed effect once, and `ProducedEvent` carries the key.

   Specs:
   - DotNET.Specs `for_ReactorDelivery/when_deriving_an_effect_key/`:
     - `and_the_partition_is_recovered` (same key)
     - `and_the_event_is_replayed` (same key)
     - `and_the_index_differs`
     - `and_the_partition_contains_the_separator` (unambiguous)
   - `for_EventsResultHandler/when_handling/`:
     - `and_keys_are_supported` (indices 0..n-1)
     - `and_a_replayed_effect_conflicts_beside_a_new_one` (partition fails, nothing written)
     - `and_a_replayed_effect_changed_generation` (failed partition messages contain the generation sentence)
   - `for_ReactionInvoker/when_a_partition_fails_and_is_recovered/and_the_handler_returned_events` (same keys twice)
   - DotNET.CodeAnalysis.Specs `for_ReactorAppendWithoutIdempotencyKeyAnalyzer/when_analyzing_reactor_methods/`:
     - `and_the_handler_appends_through_the_event_log` (reports)
     - `and_a_lambda_in_the_handler_appends` (reports)
     - `and_the_handler_is_once_only` (still reports)
     - `and_the_handler_calls_append_once` (silent)
     - `and_the_handler_appends_events_for_event_source_ids` (silent)
     - `and_a_helper_method_appends` (silent)
     - `and_the_type_is_not_a_reactor` (silent)
   - Testing.Specs `for_ReactorScenario/when_redelivering/with_returned_events`
   - Integration (`Integration/Client`, MongoDB and SQL runs, including SQL Server):
     - `when_a_reactor_partition_fails_after_appending_its_events`: one event per effect after recovery
     - `when_the_kernel_restarts_between_attempts`: the key still settles
     - `when_the_subject_is_erased_between_attempts`: the retry settles as `AlreadyAppendedAfterErasure`, one event, no new key

   Documentation:
   - `reactors/delivery-identity.mdx`: the built-in handlers are now idempotent, and replay conflicts fail the partition;
   - appending docs: keys, outcomes, the no-personal-data rule for keys, and behavior after erasure;
   - `clients/where-the-clients-differ.md`: the other clients have the wire fields but no derivation yet.

In scope: the event sequence append path, the three storage providers, the value handlers' key location, the wire contract, the .NET client, reactor side effects, the reactor analyzer and Chronicle.Testing.

Out of scope:

- Repairing unique-constraint claims after a crash between an event write and its claim update, and the stale constraint mutations that publication retries apply today. Both are tracked in Cratis/Chronicle#4895.
- Arc's `EventsAndCommandsResultHandler`. Arc keys the events it appends, by adopting `ReactorContext.Delivery` in its own repository. Whether and how a command runs again on redelivery is Arc's concern. A question to the Arc maintainers is pending, and this decision does not depend on their answer.
- The transactional unit-of-work commit path. It inherits the per-event key through `EventForEventSourceId` but gets no new API.
- Implicit migration of keyed retries between generations (ruling 4).
- Publication sink fingerprints. They never cover protected values today (`EventSequenceSinkFactory.cs:31-36`); if compliance targets are allowed later, they adopt section 4.
- Key exposure on reads and event export.
- Derived APIs for the Kotlin, TypeScript and Elixir clients.
- Possible analyzer follow-ups: appends reached through helpers or injected services, and revisiting CHR0022's severity once kernels without `idempotent-appends` are out of support.

### Resolutions

L0 resolved the six open questions on 2026-10-14, on behalf of woksin. The record stays Proposed until L0 accepts it.

1. **Key retention:** for the life of the event (section 9, option A). Any expiry would reopen duplicates.
2. **Replay conflicts:** fail the partition and fail closed, with the refusal sentence as the failed partition's reason (sections 5 and 8).
3. **Fingerprint and crypto-shredding:** the fingerprint must not survive crypto-shredding.
   - The stored, encrypted form is not stable for identical inputs, so it cannot be used.
   - The ruling's fallback, an HMAC keyed per event store, is weaker than the requirement, because crypto-shredding does not erase that key.
   - Section 4 therefore fingerprints protected values under keys derived from the keys that protect them, and settles a retry after erasure on the remaining parts.
   - This replaces the ruling's fallback with a stronger construction, at the cost section 4 lists. L0 confirms the substitution on acceptance.
4. **Generation-bump retries:** refused as `IdempotencyKeyConflict` with the actionable generation sentence (section 5). No implicit migration in v1; it is added only if a real consumer needs it.
5. **Analyzer:** a concrete, non-noisy rule exists today, so CHR0062 ships in PR 5 (section 10). Broader rules remain possible follow-ups.
6. **Arc command keys:** Arc's decision, and a question has gone to the Arc maintainers. Arc keys the events, and command re-execution is Arc's concern. This decision does not depend on Arc's answer.

## Verification

**Done when:** in every provider:

- A keyed append retried after a partition failure, a kernel restart, a duplicate concurrent delivery or a lost response produces exactly one event and reports `AlreadyAppended` with its sequence number.
- The same key with different content, or with a different generation, is refused with the exact sentence and nothing written.
- A keyed batch retried with extra events appends only the new ones.
- After the subject is crypto-shredded:
  - a retry with equal non-personal content reports `AlreadyAppendedAfterErasure`, provisions no key and writes nothing;
  - the outcome is the same for the true personal values and for a guess;
  - no stored value lets anyone confirm a guessed personal value.
- A protecting key that is missing without an erasure refuses the retry as unverifiable.
- A replay conflict leaves a failed partition whose reason names the key and sequence number.
- CHR0062 reports direct appends in reactor handlers and nothing else.
- A released-minor client against the new kernel, and a new client against an old kernel, both behave as they do today.

Each new-behavior spec listed above is red before its change. The existing-behavior regression specs, such as the overload-binding spec, are green before and after.

## Consequences

- Once the client and kernel both carry this, reactors that return events become idempotent across recovery and replay without code changes. Arc and Stage get the seam they asked for. STAGE-ESM-037 can be lifted when the kernel advertises `idempotent-appends` and Arc keys its events.
- **Public API:** additive only, so the client and kernel both ship as minor.
  - New `AppendOnce`/`AppendManyOnce` methods are default interface members, so existing overload resolution is unchanged.
  - `EventForEventSourceId` gets an init property, and results get new properties and a three-valued outcome.
  - `ReactorContext` gets a new init property, and `IJsonSchemaMetadataValueHandler` gets a key-location member.
- **Wire:** new optional fields only. An old client never sends a key and sees no new behavior. An old kernel never advertises the capability, so a new client refuses a keyed append before sending; the built-in handlers fall back to today's unkeyed appends and log once per reactor.
- **Replay behavior changes.** A replayed reactor that once appended duplicates now appends nothing. A reactor whose replayed output differs, including in generation, fails its partition with the refusal sentence instead of appending a contradicting event; an operator resolves it.
- **Storage:** keyed events carry about 140 bytes more, plus about 135 per protecting key, and an index entry, kept for the life of the event. No migration is needed.
- **Privacy:**
  - The fingerprint holds no plain digest of protected values. Their parts are keyed by keys derived from the protecting keys, so crypto-shredding makes them unverifiable by anyone, including the kernel.
  - After erasure, the comparison covers only the parts that do not depend on erased keys, so a differing personal value is not detected. This is by design.
  - Idempotency keys are stored in plaintext and survive shredding, so they must not carry personal data.
  - Settling a keyed hit reads the protecting key, cached, but never provisions one.
- **Analyzers:** CHR0062 is a new warning, so builds that treat warnings as errors fail on direct appends in reactor handlers until the code returns its events, uses `AppendOnce`, or suppresses the warning. CHR0022's description changes and its severity does not.
