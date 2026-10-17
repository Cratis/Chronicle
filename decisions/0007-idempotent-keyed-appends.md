---
id: 0007-idempotent-keyed-appends
title: Make keyed appends idempotent in the event sequence, and key reactor side effects by delivery
status: superseded
stage: none
class: contract
reversibility: costly
decided: 2026-10-14
decider: L0 (woksin's delegation)
superseded-by: 0012-keyed-appends-travel-on-their-own-rpcs
applies-to:
  - Source/Kernel/Core/Sequences/**
  - Source/Kernel/Core/EventSequences/**
  - Source/Kernel/Core/Schemas/IJsonSchemaMetadataValueHandler.cs
  - Source/Kernel/Core/Compliance/GDPR/PIICompliancePropertyValueHandler.cs
  - Source/Kernel/Core/ProtectedValues/**
  - Source/Kernel/Storage/Compliance/**
  - Source/Kernel/Storage/EventSequences/**
  - Source/Kernel/Storage.InMemory/Compliance/**
  - Source/Kernel/Storage.InMemory/EventSequences/**
  - Source/Kernel/Storage.MongoDB/Encryption/**
  - Source/Kernel/Storage.MongoDB/EventSequences/**
  - Source/Kernel/Storage.Sql/EventStores/Namespaces/Encryption/**
  - Source/Kernel/Storage.Sql/EventStores/Namespaces/EventSequences/**
  - Source/Kernel/Storage.AzureKeyVault/**
  - Source/Kernel/Storage.Vault/**
  - Source/Kernel/Contracts/**
  - Source/Kernel/Protobuf/**
  - Source/Kernel/Grpc/Clients/ConnectionService.cs
  - Source/Clients/DotNET/EventSequences/**
  - Source/Clients/DotNET/Reactors/**
  - Source/Clients/DotNET.CodeAnalysis/Analyzers/ReactorOnceOnlyAnalyzer.cs
  - Source/Clients/Testing/Reactors/**
  - Documentation/reactors/**
  - Documentation/events/appending.mdx
  - Documentation/events/appending-many.mdx
  - Documentation/code-analysis/CHR0022.mdx
  - Documentation/scenarios/react-to-an-event.mdx
  - Documentation/tutorial/reacting.mdx
  - Documentation/clients/where-the-clients-differ.md
---

<!-- markdownlint-disable-next-line MD025 -->
# Make keyed appends idempotent in the event sequence, and key reactor side effects by delivery

> **Superseded on 2026-10-17 by [Decision 0012](0012-keyed-appends-travel-on-their-own-rpcs.md).** 0012 changes four points (keyed appends use their own RPCs, a key hit marks the constraint gap without other side work, one storage failure shape following decision 0008, and minor release labels) and carries every other part of this record forward unchanged. The text below is preserved as accepted.

Accepted on 2026-10-14 by L0 on woksin's delegation for Cratis/Chronicle#4809, with the rulings recorded under Resolutions.

## Context

A reactor that returns events has them appended by a built-in side-effect handler, all under `Source/Clients/DotNET/Reactors/SideEffects/`:

- `Append`: `EventResultHandler.cs:54-60`.
- `AppendMany`: `EventsResultHandler.cs:62-68`, `EventForEventSourceIdResultHandler.cs:33` (one item), `EventsForEventSourceIdResultHandler.cs:36`, `MixedSideEffectsResultHandler.cs:77` and `EventsWithConcurrencyScopesResultHandler.cs:29-31`.

When the partition fails after that append, for example because Arc's `EventsAndCommandsResultHandler` then has a command refused, recovery delivers the event again and the handler appends the same events a second time. Nothing on the append path can stop it:

- `IEventSequence.Append` and `AppendMany` (`Source/Clients/DotNET/EventSequences/IEventSequence.cs:215-225, 244-254, 267-271`) and `EventForEventSourceId` (`EventForEventSourceId.cs:15-84`) have no field a caller can set to identify an event.
- The kernel `Append`, `AppendMany` and `AppendManyForEventSources` artifacts carry no identity either (`Source/Kernel/Core/Sequences/Append.cs:41-58`, `AppendMany.cs:30-41`, `AppendManyForEventSources.cs:28-37`, `EventForEventSourceId.cs:26-37`, `EventToAppend.cs:17`).
- `[OnceOnly]` only takes effect during replay (`Source/Clients/DotNET/Reactors/OnceOnlyAttribute.cs:7-19`). Recovering a failed partition is an ordinary delivery, not a replay (`Documentation/reactors/once-only.mdx:21-24`).
- `ReactorDelivery` (`Source/Clients/DotNET/Reactors/ReactorDelivery.cs:38-55`) is stable across recovery and replay (`DotNET.Specs/Reactors/for_ReactorDelivery/when_the_event_is_replayed.cs:13-22`). It explicitly suppresses nothing (`ReactorDelivery.cs:25-31`). Its `Id` joins components with `#` (`Source/Infrastructure/KeyHelper.cs:18, 27`), so it is ambiguous when a partition contains `#`.

The retry window is effectively unbounded. The defaults are 10 attempts with a 600-second backoff cap, `0` means retry forever (`Source/Kernel/Core/Configuration/Observers.cs:45-66`), and an operator can recover a failed partition at any later time.

The kernel already has an idempotent append, built for projection sinks:

- `EventSequence.AppendPublication` (`Source/Kernel/Core/EventSequences/EventSequence.Publications.cs:15-52`) looks up the identity before any validation and returns the stored sequence number on a hit (`EventSequence.Validation.cs:48-56`). On a hit it also finishes the grain-side work that a crashed activation never did (`ReconcileExistingPublication`, `EventSequence.Validation.cs:149-179`).
- `IEventPublicationStorage` (`Source/Kernel/Storage/EventSequences/IEventPublicationStorage.cs:8-33`) requires the identity and fingerprint to be written in the same atomic write as the event, and compares fingerprints inside the store (SQL `EventSequenceStorage.Publications.cs:29-33`).
  - MongoDB stores them on the event document (`Storage.MongoDB/EventSequences/Event.cs:70-75`) under a unique sparse index (`EventSequenceStorage.cs:1075-1082`) and resolves races (`EventSequenceStorage.Publications.cs:15-96`).
  - SQL stores the identity, its SHA-256 hash and the fingerprint on the event row, with a unique filtered index on the hash (`EventEntry.cs:100-118`, `EventSequenceDbContext.cs:95-97`, `EventSequenceMigrator.cs:61-72, 207-215`), and translates unique violations (`EventSequenceStorage.Publications.cs:39-97`).
  - InMemory checks under its lock (`Storage.InMemory/EventSequences/EventSequenceStorage.Publications.cs:12, 24-80`).
- Redaction updates the row in place. It keeps the identity and fingerprint, but it replaces the row's event type with the redaction type (MongoDB `EventSequenceStorage.cs:1124-1134`; SQL `EventSequenceStorage.cs:368-395`).
- The identity is single-event only. Batch storage appends never write it, and every MongoDB duplicate-key error in a batch is treated as a sequence-slot collision and renumbered (`Storage.MongoDB/EventSequences/EventSequenceStorage.cs:432-439, 1215-1225`). The batch runs in a session transaction (`:346-428`) and rethrows every other error after aborting (`:440-444`). The single-event MongoDB insert treats duplicate keys the same way (`EventSequenceStorage.cs:273, 316-326`).
- Publication fingerprints never cover protected values: a publication target whose schema carries compliance metadata is refused (`Source/Kernel/Core/Sinks/EventSequenceSinkFactory.cs:31-36, 48-53`).

The content check from #4510 (`Source/Kernel/Core/Sequences/VerifyContent.cs:50-197`) answers `Unavailable` in several cases: on SQL, which does not support revision tracking (`VerifyContent.cs:54-57`, `Storage.Sql/.../EventSequenceStorage.cs:43`); after a revision or redaction (`VerifyContent.cs:72-75`); and when a migration inspects protected values (`VerifyContent.cs:128-133`). Each comparison also releases protected values.

Compliance runs on the append path. `MakeEventCompliant` (`EventSequence.cs:1063-1069`) applies every protected property through `IJsonSchemaMetadataManager.Apply`, under the subject, or the event source id when no subject is set.

- `[PII]` values are encrypted under a per-subject key (`PIICompliancePropertyValueHandler.cs:48-57`).
- `[Encrypted]` values are encrypted under subject, namespace or global keys in a separate identifier space that has no erasure path (`EncryptedValueKeyIdentifiers.cs:10-33`, `EncryptedSubjectValueHandler.cs:19-26`, `PIIManager.cs:91-100`).
- Encryption is randomized. Every value gets a fresh AES-256-GCM data key and nonce (`Source/Kernel/Core/Compliance/Encryption.cs:45-67`, data key `:50`, nonce `:53`), and the data key is wrapped with RSA-OAEP under the public half of the key (`:33, 107-114`).
- Keys are read without their revision. `TryGetFor` returns only the `EncryptionKey` (`IEncryptionKeyStorage.cs:82-89`), `GetOrAddFor` returns only the key, and the append path gets its key from `EnsureKeyFor`, which also returns only the key (`PIICompliancePropertyValueHandler.cs:56`; `ManagedEncryptionKeyProvisioner.cs:30-49`).
- Key reads are cached. `CacheEncryptionKeyStorage` remembers an absence for 5 seconds (`CacheEncryptionKeyStorage.cs:59, 171-174, 194`). An absence is cleared cluster-wide on erasure and on `AllowNewKeyFor` (`PIIManager.cs:57, 80`; `EncryptionKeyCacheGrainService.cs:29-37`). Provisioning clears it only on the provisioning silo (`CacheEncryptionKeyStorage.cs:128-155`), so another silo can remember an absence for up to 5 seconds after a new lifecycle's key exists. Erasure state is never cached (`:94-95`). `IEvictEncryptionKeyCache.EvictFromCache` evicts one identifier from the local cache, on the cache store and on the composite store (`CacheEncryptionKeyStorage.cs:90`, `CompositeEncryptionKeyStorage.cs:275-283`).
- Crypto-shredding first records an erasure fence, then deletes the key material (`PIIManager.cs:50-51`). The fence holds `ErasedThrough`, the highest revision erased, and `ErasedKeyFingerprints`, the public fingerprints of the destroyed keys (`EncryptionKeyErasure.cs:27-33`; `EncryptionKey.cs:18-26`). Its floor is at least `Initial` even when no key was present (`EncryptionKeyErasure.cs:53-64`). After that, provisioning a key for the subject throws until a new lifecycle is explicitly allowed, and the new key is minted above the floor (`IEncryptionKeyStorage.cs:41, 53-56, 153-170`).
- By default the keys are stored in the kernel's own database (`Storage.MongoDB/Encryption/EncryptionKeyStorage.cs:20`). The Azure Key Vault store deletes keys with `StartDeleteSecretAsync` and never purges them (`Storage.AzureKeyVault/AzureKeyVaultEncryptionKeyStorage.cs:323-334`).

Appends are serialized inside the non-reentrant `EventSequence` grain; only read methods are `[AlwaysInterleave]` (`Source/Kernel/Core/EventSequences/IEventSequence.cs:61, 72, 83, 97`). A batch is all-or-nothing:

- validation failures stop it (`EventSequence.cs:359-385`);
- the concurrency check runs next (`EventSequence.cs:387-391`);
- then a single storage transaction runs (`EventSequence.cs:625-720`; MongoDB session `EventSequenceStorage.cs:346-428`; SQL one `SaveChangesAsync`, `EventSequenceStorage.cs:274-367`).
- A slot collision is renumbered by `AppendWithRetries`, which then runs the post-renumber validation `ValidateAppendRetry` or `ValidateAppendManyRetry` before it writes again (`EventSequence.cs:722-738`; `EventSequence.Validation.cs:120-130`).

Grain calls between silos are serialized as JSON and omit null values (`Source/Kernel/Core/Setup/Serialization/SerializationConfigurationExtensions.cs:150-162`). An old silo therefore ignores a property it does not know.

This crosses every storage provider, the wire contract, the public client API, reactor semantics on replay and the guarantees of crypto-shredding, so it needs a durable decision. [Decision 0004](0004-ordered-lanes-observer-delivery.md) Q4 promises at-least-once delivery, not exactly-once effects. This decision makes the *appended* effects idempotent and leaves that promise unchanged.

## Decision

An append may carry an **idempotency key** per event. The event sequence grain settles the key against an identity stored **on the event itself**, in the same atomic write. This reuses and generalizes the publication identity.

- Same key and same intent: the append is reported as already done, with the stored sequence number. No new event, no error.
- Same key and different intent: the append is refused with `IdempotencyKeyConflict`. Nothing is overwritten.
- Same key after the subject was crypto-shredded: the parts that do not depend on erased keys are compared. If they are equal, the append is reported as already done (section 4).

Built-in reactor side-effect handlers key every returned event by delivery and position.

### 1. Key shape and who computes it

- `IdempotencyKey` is a client concept, `ConceptAs<string>`, between 1 and 512 characters. It has **no** implicit conversion from `string`, because a string literal would then be ambiguous with the `EventStreamType` parameter (`Source/Clients/DotNET/Events/EventStreamType.cs:27`).
- An empty string on the wire means "no key". Proto3 strings have no presence, so the Kotlin, TypeScript and Elixir clients send `""` for an unset field. The kernel validator refuses only keys longer than 512 characters.
- The caller computes the key; the kernel never derives one. A key is scoped to one event store, namespace and event sequence, which is where the index lives.
- Reactor keys have two derivations, so a handler-chosen key never collides with a built-in one. Both are a length-prefixed SHA-256 over: reactor, event store, namespace, observed sequence, partition, triggering sequence number, effect epoch, a kind and an effect. This is the same encoding as `PublicationIdentity` (`Source/Kernel/Core/Sinks/PublicationIdentity.cs:31-40`), so it does not inherit `DeliveryId`'s `#` ambiguity.
  - **Built-in handlers** use the kind `returned`, and the effect is the zero-based position of the event in the value the handler returned. This derivation is internal to the handlers.
    - The handlers carry the key through the overload they can already use, `AppendMany(IEnumerable<EventForEventSourceId>, ..., concurrencyScopes)` (`IEventSequence.cs:267-271`). `EventsResultHandler` (`EventsResultHandler.cs:62-68`) converts each returned event to an `EventForEventSourceId` carrying the context's stream type, stream id, source type and subject, plus its derived key.
    - `EventsWithConcurrencyScopesResultHandler` (`:29-31`) already holds `EventForEventSourceId` items. It sets the derived key on those without one and passes its scopes unchanged to the same overload.
    - `EventResultHandler` (`EventResultHandler.cs:54-60`) calls `AppendOnce` with the derived key and the same context values.
  - **Handler-chosen keys** come from `ReactorDelivery.IdempotencyKeyFor(string effect)`, with the kind `named` and a non-empty effect name the handler chooses. A handler uses it for `AppendOnce` calls and for keys it sets on an `EventForEventSourceId` itself.
- **Precedence.** A key the handler set on a returned `EventForEventSourceId` wins over the derived one, and the built-in handler sends it unchanged. If the connected kernel lacks the capability, such an event fails the partition with `IdempotentAppendsNotSupported`; the key is never dropped.
- **Effect epoch.** `ReactorAttribute` (`Source/Clients/DotNET/Reactors/ReactorAttribute.cs:13-30`) gains an optional third parameter, `uint effectEpoch = 0`, exposed as `EffectEpoch`: `[Reactor("stable-id", effectEpoch: 1)]`.
  - `ReactorDelivery` gains an `EffectEpoch` property set from it. The property is not part of `Id` (`ReactorDelivery.cs:55`), so delivery ids do not change.
  - Raising the epoch deliberately starts a new key space for the reactor (section 8).
  - A reactor without `[Reactor]` has epoch 0. To set an epoch it adds the attribute; an empty id keeps the default id (`ReactorAttribute.cs:11`).
  - Fluent reactors do not need an epoch, because their handlers return no events (`IReactorBuilder.cs:31-81`).
- The key is stored in plaintext for the life of the event and is not affected by crypto-shredding. **A key must not contain personal data**, and neither may an effect name, because anyone who knows the delivery components can recompute a derived key from a guessed name. The built-in derivation hashes delivery components only. The documentation states this rule for caller-chosen keys and effect names.

### 2. Wire and client surface

- **Core artifacts.** A nullable `IdempotencyKey` string is added at the end of `Append`, `AppendWithNamedTags`, `Sequences.EventToAppend`, `EventToAppendWithNamedTags`, `EventForEventSourceId` and `EventForEventSourceIdWithNamedTags`.
  - Building Core regenerates the Contracts sources (`Core.csproj:51-53`). Protobuf is generated by the Contracts build (`Contracts.csproj:51-56`), which ran earlier in the same invocation, before those sources existed.
  - The sequence is therefore: build Core, then build Contracts again to regenerate the `.proto` files and `chronicle.desc`, then rebuild consumers and run the stale-output and wire-compatibility checks.
  - `ProtoMemberIndexReader` assigns the next free field numbers.
- **Kernel results.**
  - `AppendResult` gains `Outcome` (`AppendOutcome`: `Appended`, `AlreadyAppended` or `AlreadyAppendedAfterErasure`).
  - `AppendManyResult` gains `Outcomes`, one entry per request event, aligned with `SequenceNumbers`.
  - Both gain `IdempotencyConflicts`. Each entry holds the key, the existing sequence number, the stored event type and generation (read from the stored fingerprint, section 4), the requested generation, and a reason: `Content`, `Generation` or `Unverifiable`.
  - Refusals travel in the existing `Errors` field (`AppendResponse` field 9, `Source/Kernel/Contracts/Sequences/IEventSequences.cs:419-423`) as a code followed by one explanatory sentence (section 5). The codes are `AppendError.IdempotencyKeyConflict`, `IdempotencyKeyRepeatedInBatch`, `IdempotencyKeyUnverifiable`, `IdempotencyFingerprintUnsupported` and `IdempotencyKeyUnsettled`.
- **Grain.** Keyed appends use **new** grain methods: `AppendWithIdempotencyKey` for a single event, and `AppendManyWithIdempotencyKeys` for a batch. The batch method takes the keys as a parameter aligned with the events.
  - The keys never travel as a new property on the existing grain types or methods. An old silo would ignore such a property (Context) and append without deduplication.
  - In a mixed-version cluster, a keyed call that reaches an old silo fails, because that silo has no such method. A key is never silently ignored.
  - Unkeyed appends keep the existing methods, and no existing grain signature changes.
- **Capability.** The kernel advertises `KernelCapabilities.IdempotentAppends` (`"idempotent-appends"`) through `ConnectionService.cs:142`. The capability comes from the silo the client connected to, not from the whole cluster; the new grain methods close that gap.
- **.NET client.**
  - `EventForEventSourceId.IdempotencyKey { get; init; }`.
  - Distinctly named methods `AppendOnce(EventSourceId, object, IdempotencyKey, ...)` and `AppendManyOnce(EventSourceId, IEnumerable<object>, IdempotencyKey, ...)`. New overloads of `Append` and `AppendMany` are not used:
    - existing calls such as `Append(id, @event, null)` and `AppendMany(id, events, default)` bind to the `EventStreamType?` parameter today (`IEventSequence.cs:215-225, 244-254`);
    - a second reference-typed third parameter would make those calls ambiguous;
    - a compilation spec covers those existing `null` and `default` call shapes.
  - The batch form expands the key per event to `key.ForIndex(i)`. `ForIndex(i)` is the length-prefixed SHA-256 of (key, `batch`, i), in the encoding of `PublicationIdentity`, so it never collides with a caller key and is always 64 characters, whatever the length of the caller's key.
  - These are default interface members that throw `IdempotentAppendsNotSupported`, following the `AppendPrepared` pattern (`IEventSequence.cs:67-77`).
  - Client `AppendResult` and `AppendManyResult` gain `Outcome`/`Outcomes` and `IdempotencyConflicts`. Both `AlreadyAppended` outcomes count as `IsSuccess`.
  - If the connected kernel lacks the capability, a keyed append throws `IdempotentAppendsNotSupported` **before** sending, so a key is never silently ignored.

### 3. Where dedup is enforced, and why it is race-safe

The check runs in the grain turn, before constraint, schema, compliance and concurrency validation, mirroring `EventSequence.Validation.cs:48-56`. The stored identity is `append-key:` followed by the key. Sink identities are uppercase hex (`PublicationIdentity.cs:39`), so the two domains can never collide. **Fingerprints of keyed appends are compared in the grain, never in storage** (section 4). Storage returns the sequence number and the stored fingerprint with the receipt. Keyed single appends use the same storage method as keyed batches, with one event, so `AppendPublication`'s in-store ordinal comparison stays with publication sinks.

- **Concurrent duplicate deliveries to one activation.** They serialize in the non-reentrant grain. The second one finds the first one's durable identity.
- **Two activations (silo handover or split brain).** Both may miss in the lookup. Exactly one write wins; the loser settles or fails retryably. **Settling again** means looking up the keys again. A key that is now durable gets its section 4 outcome instead of whatever the loser saw. Keys that are still absent keep their own result. The loser can meet the winner at four points, and each one settles again:
  1. **Validation before storage.** A constraint, or the concurrency check that now sees the winner's event (`EventSequence.Validation.cs:58-78`; batch `EventSequence.cs:359-391`; `ConcurrencyValidator.cs:28-49`), can refuse the loser before it reaches the unique index. A keyed append that missed in the first lookup settles again before returning any validation failure.
  2. **Validation after a renumber.** Both activations share `State.SequenceNumber`, so the first collision is usually the sequence slot, not the identity. `AppendWithRetries` renumbers and then runs `ValidateAppendRetry` or `ValidateAppendManyRetry` (`EventSequence.cs:722-738`; `EventSequence.Validation.cs:120-130`). Their concurrency violation is caused by the winner's event, and it is returned after storage was attempted. For a keyed append, the grain settles again before returning any failure from that path.
  3. **The unique index.** The store's unique index (MongoDB sparse unique, SQL filtered unique on the hash) or the InMemory lock lets exactly one write win. The loser's storage call reports `IdentitiesAlreadyDurable` with the winner's receipts.
  4. **A MongoDB transaction conflict.** Keyed singles take the batch path, which runs in a session transaction. In MongoDB, a unique-key collision with another *uncommitted* transaction raises `WriteConflict`, labelled `TransientTransactionError`, not `DuplicateKey`. A lookup made while the winner is still uncommitted also misses. Storage aborts and reports `TransactionConflicted` instead of rethrowing it as today (`EventSequenceStorage.cs:440-444`).

  After `IdentitiesAlreadyDurable` or `TransactionConflicted`, the grain settles again and retries the events whose keys are still absent, **once**. A second `IdentitiesAlreadyDurable` or `TransactionConflicted` on that retry is not retried again. After a second report the grain settles again without writing. Keys now durable get their section 4 outcome. Only if a key is still absent does the append fail with `IdempotencyKeyUnsettled`, with nothing written; the caller retries later, and for a reactor the partition fails and is recovered. This matters on MongoDB, where the usual sequence is a `WriteConflict` first and a duplicate key on the retry once the winner commits, and the receipt is then available. Deterministic interleaving specs cover every point above on both the single and the batch path, and a MongoDB spec covers two concurrent keyed transactions.
- **Kernel crash between event and key.** This window does not exist: the key is part of the event document or row, and a batch is one transaction.
- **Crash after the write but before the grain finished.** Keyed settlement does **no** side work on a hit. It returns the stored receipt and provisions no encryption key.
  - It does not reuse `ReconcileExistingPublication` (`EventSequence.Validation.cs:149-179`). That method rebuilds constraint updates from the retry's content, and without a durable ordering for claims and removals no retry can apply those safely.
  - The grain's sequence number and per-type tails are rebuilt from durable events on activation (`EventSequencesStorageProvider.cs:40-43, 87-93`).
  - An event whose turn failed before `CompleteDurableAppend` (`EventSequence.cs:945-979`) was never enqueued to observers, and a keyed hit does not enqueue it either. Observers that activate or catch up read it from storage from their persisted cursor (`EventSequence.cs:934-943`). A subscription the queue dropped is rescued (`Observer.Rescue.cs:11-29`). An observer that stays subscribed can move past the event; skip detection reports that but does not repair it (`Observer.SkipDetection.cs:13-16`, `ObserverLogging.cs:20`). Every append whose turn fails after the write has this exposure; keys do not add to it, and this decision does not close it.
  - A missing unique-constraint claim after a crash between the event write and the claim update affects every append, keyed or not, and publication retries too. Cratis/Chronicle#4895 fixes it; this decision does not.
  - Within one activation, whenever the grain finds a durable key, whatever the outcome, it updates both counters, matching `EventSequence.cs:888-895`. It sets the next sequence number to `max(State.SequenceNumber, receipt.SequenceNumber.Next())`, and it raises the per-type tail for the stored event type (read from the fingerprint) to the receipt's sequence number. A write can commit while its turn fails, so without this the next append could collide with it.
- **Batch storage.** A new storage method `AppendManyWithIdentities` returns `Result<IEnumerable<AppendedEvent>, AppendManyStorageFailure>`. The failure is one of:
  - `DuplicateEventSequenceNumber`, dispatched to the existing slot-renumbering loop (`EventSequence.cs:722-738`);
  - `IdentitiesAlreadyDurable`, with the receipts found (sequence number and stored fingerprint);
  - `TransactionConflicted` (MongoDB only), after the transaction was aborted.

  The existing batch methods (`IEventSequenceStorage.cs:145, 153`) keep their contract. On a unique violation, each provider classifies the failure by looking up the batch's identities and the colliding slots, never by the violation predicate alone, because SQL's `IsPublicationUniqueViolation` matches every unique violation (`Storage.Sql/.../EventSequenceStorage.Publications.cs:91-97`). The grain then settles again (section 6) and retries once, as above.

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

**How protected values enter it.** The content is walked with the traversal compliance itself uses (`JsonSchemaMetadataManager.cs:261-276, 297-384`).

- Every value at a protected boundary is replaced in the canonical JSON by a marker naming its protection, and set aside with its JSON path.
- Metadata that has no registered value handler is treated as append treats it. Append applies a handler only when one is registered and otherwise stores the value as it is (`JsonSchemaMetadataManager.cs:330`, noted at `:284-285`), so the fingerprint covers that value as ordinary content in the plain part. The comparison-only check `ValidateComparisonBoundary` (`:277-291`), which refuses unknown handlers, is not applied to it. Otherwise built-in keyed handlers would fail reactors that append successfully today.

The fingerprint then has two kinds of part:

- **The plain part.** A SHA-256 over the length-prefixed fields listed above, with the masked content. It records the path and protection kind of every protected value, but no protected value.
- **One protected part per protecting key.** An HMAC-SHA-256 over the length-prefixed (path, canonical value) pairs that key protects, in document order.
  - The HMAC key is derived with HKDF-SHA-256 from the private part of that encryption key (`EncryptionKey.cs:15`). The salt is the stored identity, `append-key:` followed by the idempotency key (UTF-8), and the info label is `cratis-chronicle/append-fingerprint/v1`. The derived key is computed when needed and never stored.
  - The salt binds each part to its idempotency key. Two keyed events of the same subject with the same protected values store different parts, so a copy of the database shows no equality between events, before or after shredding. A retry carries the same key, so it recomputes the same part.
  - Each protected part is stored with a reference to its key and that key's revision. The reference is the key's public fingerprint (`EncryptionKey.cs:18-26`), the same value an erasure fence records (`EncryptionKeyErasure.cs:28`). It names key material without holding any.
  - `[PII]` values belong to the subject's PII key (identifier as in `EventSequence.cs:1065`). `[Encrypted]` values belong to their subject, namespace or global key (`EncryptedValueKeyIdentifiers.cs`).
  - Each value handler reports where its key lives through a new `IJsonSchemaMetadataValueHandler` member (the key identifier and protection kind), which a hit uses to read the stored revision. The latest key is read **with its revision** through a new `IEncryptionKeyStorage.TryGetLatestFor`, which returns the latest key and its revision together, and never provisions. `TryGetLatestFor` is still needed: no store exposes a revision today, and the same-read rule above is built on it. Every key store implements it: MongoDB, SQL, both InMemory stores, Azure Key Vault, Vault, and the composite and cache decorators.
    - The cache stores the key and revision as one entry under its latest cache key, so a cached read returns a consistent pair.
    - The composite store reads its stores in order and returns the first store's latest key with that store's revision, after its destroyed-key check and heal, which is the key its `TryGetFor` returns for the latest revision. When its stores disagree on the latest revision, the encryption and the fingerprint therefore still share the first store's key and revision. A later retry reads the stored revision with `TryGetFor`, and the stored key reference decides whether the part is verified or unverifiable.
  - Some boundaries cannot be fingerprinted: a handler that reports no key location, or a boundary under more than one protection (which `ValidateComparisonBoundary` also refuses, `JsonSchemaMetadataManager.cs:277-291`, and CHR0053 flags at compile time). A keyed append whose event type has such a boundary is refused with `IdempotencyFingerprintUnsupported` before anything is written.

**Stored form.** The stored form starts with `v1:` and the plain part in hex. Next comes `;type=` with the event type id and generation, in the length-prefixed encoding of `PublicationIdentity`.

- Recording the type is what lets the grain name the event type and tell a `Generation` conflict from a `Content` conflict after a redaction, which rewrites the row's own type (Context).
- Then, for each protecting key, in the fixed order `pii`, `encrypted-subject`, `encrypted-namespace`, `encrypted-global`, it appends `;`, the kind, `:`, the key reference, `@`, the key revision, `=` and the HMAC in hex. Content without protected values stores no protected parts.
- On a miss, the fingerprint is computed after `MakeEventCompliant`, under the key and revision that the same read supplied to the encryption. The fingerprint never makes a second key read, so no erasure or new lifecycle can land between encrypting a value and fingerprinting it. Nothing is provisioned for the fingerprint alone.
  - The value handlers' `Apply` reports the key it used (reference and revision), taken from a revision-reporting form of `EnsureKeyFor`. That form reads with `TryGetLatestFor` and, only on an absence, provisions through a revision-reporting form of `GetOrAddFor`, which reports the revision it minted or found. The encrypted value and the fingerprint share that one key.
  - If the values of one protection kind in the event were encrypted under different keys or revisions, because the key changed during the append, no single protected part can cover them. The append fails retryably with `IdempotencyKeyUnsettled` and nothing is written.
- The version prefix lets a future canonicalization recompute under the stored version.

**Comparison on a hit.** The grain compares in this order:

1. The plain parts must be equal and the set of protection kinds must match. Otherwise the result is `IdempotencyKeyConflict`; the reason is `Generation` when the stored and requested event type ids are equal and the generations differ, and `Content` otherwise.
2. For each stored protected part, the grain reads the key **at the part's stored revision** with `TryGetFor` (`IEncryptionKeyStorage.cs:89`). It never calls `EnsureKeyFor` or `GetOrAddFor`, so a hit never provisions a key.
   - **Present, with the stored reference as its fingerprint:** the grain recomputes the HMAC over the retry's values. Equal means the part is **verified**; different means `IdempotencyKeyConflict` with reason `Content`.
   - **Absent:** the grain evicts the identifier locally through `IEvictEncryptionKeyCache` and reads again. The negative cache can still remember an absence after another silo provisioned the key (Context), and the grain must not mistake that for a missing key. If the key is still absent, the grain reads the erasure fence with `GetErasureFor` (`IEncryptionKeyStorage.cs:131-132`).
     - The part is **erased** only if its stored reference is in `ErasedKeyFingerprints`, or its stored revision is at or below `ErasedThrough`. The revision test covers a key that was erased while it was already missing, because the fence's floor is at least `Initial` (`EncryptionKeyErasure.cs:56-57`).
     - An erasure recorded for the identifier is **not** enough on its own. After a new lifecycle (`AllowNewKeyFor`), a part written under the new key sits above the floor, and its key is not fenced.
   - **Otherwise** the part is **unverifiable**. That covers an absent key that the fence does not cover, for example a new lifecycle's key lost in a restore, and a present key with a different fingerprint.
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
| Plain SHA-256 over plaintext | yes | anyone who can read the row can confirm a guessed value | refused by the ruling |
| Hash of the stored, encrypted form | **no**: fresh data key and nonce per value (`Encryption.cs:50, 53`) and randomized OAEP wrap (`:33, 113`), so every retry would conflict. Recomputing would also need encryption, which is refused after erasure (`IEncryptionKeyStorage.cs:41`) | no comparison is defined | not viable |
| HMAC keyed per event store | yes | the key is not erased with the subject's. The kernel, and anyone who holds the database and the key store (by default the same database), can still confirm guesses. The kernel itself becomes an online oracle: a keyed retry answers `AlreadyAppended` or a conflict for a guessed value, and reactor keys can be computed from delivery components | **weaker than the requirement** |
| Plain part plus HMAC under a key derived from each protecting key, unsalted | yes | nobody can recompute the parts, but equal values of one subject store equal parts, so a database copy shows equality between events, before and after shredding | **leaks equality** |
| Plain part plus HMAC under a key derived from each protecting key, salted with the idempotency key | yes | the derived key goes with the subject's key, so nobody can recompute a part; the outcome is the same for every guess; parts of different events are unrelated | **chosen** |

**Costs.** Of keyed appends in general, and of the chosen construction:

- Every keyed append or batch makes one indexed identity lookup before validation. Once the capability is present, that means every event a reactor returns.
- A keyed append makes a second lookup before returning any validation failure, and after a durable-identity or transaction-conflict report (section 3).
- On MongoDB, every keyed single append runs in a session transaction, because it takes the batch path.
- On a miss with protected values: no extra key read, because the encryption's own read supplies the key and revision (cached, as today), plus one HKDF and one HMAC per protecting key. That puts the private key on the append path, which today needs only the public half to encrypt (`Encryption.cs:33, 107-114`).
- On a hit with protected values: up to four cached key reads, one per protecting kind; for each absent key, a local cache eviction, a second read and an uncached `GetErasureFor` (`CacheEncryptionKeyStorage.cs:94-95`).
- A new member on the four value handlers, `Apply` reporting the key it used, revision-reporting forms of `EnsureKeyFor` and `GetOrAddFor`, and `TryGetLatestFor` on every key store.
- Storage: about 250 bytes per keyed event for a reactor key, plus the extra length of a longer caller key (about 700 bytes for a 512-character key), plus about 160 bytes per protecting key. A sparse or filtered index entry comes on top.
- A third outcome on the wire.
- After shredding, the comparison covers only the plain part, so a retry whose personal values differ is not detected. This is by design.

**Residual exposure.** What the construction does not hide:

- **Key backups.** A backup, replica or export of the key store that still holds the subject's key undoes shredding for the ciphertext and the protected parts alike. The fence travels into backups taken after the erasure (`EncryptionKeyErasure.cs:17-20`), but older backups keep the key.
- **Azure Key Vault soft-delete.** The Key Vault store deletes and never purges (Context). Where the vault has soft-delete on, a deleted key stays recoverable for the vault's retention period. Until it is purged, whoever can recover it can verify guesses against the protected parts and decrypt the ciphertext.
- **Holders of the key before shredding.** They can confirm a guess, but they can decrypt the ciphertext anyway.
- **Clients allowed to append.** Such a client can probe equality by retrying a key with chosen values. The same client can already read the event with its values released (`EventCompliance.cs:21-33`).
- **The fact of an erasure.** `AlreadyAppendedAfterErasure` shows that an erasure happened, not what was erased.
- **Which key protected the values.** The key reference and revision say which key and lifecycle protected an event. The subject column already shows whose data it is.
- **Kernel memory.** The kernel holds the private key and the derived key in memory while it compares, as it holds the private key while it releases values.
- **The plain part.** It covers only data the row already stores in plaintext: the event source, subject, route and unprotected content.

### 5. Outcomes surfaced to clients

| Case | Result | Events written |
| --- | --- | --- |
| New key | `Appended` | yes |
| Durable key, every fingerprint part verified | `AlreadyAppended`, stored sequence number, `IsSuccess` | no |
| Durable key, plain part equal, protected part erased with the subject's key | `AlreadyAppendedAfterErasure`, stored sequence number, `IsSuccess` | no, and no key is provisioned |
| Durable key, same event type, different generation | `IsSuccess = false`, `IdempotencyKeyConflict`, reason `Generation` | no |
| Durable key, any other difference | `IsSuccess = false`, `IdempotencyKeyConflict`, reason `Content` | no |
| Durable key, protecting key missing and not covered by an erasure | `IsSuccess = false`, `IdempotencyKeyUnverifiable` | no |
| Same key twice in one request | `IsSuccess = false`, `IdempotencyKeyRepeatedInBatch` | no |
| Event type with an unsupported protected boundary | `IsSuccess = false`, `IdempotencyFingerprintUnsupported` | no |
| A key still absent after the grain settled again (another append with the same key still in progress, section 3), or the protecting key changed during the append (section 4) | `IsSuccess = false`, `IdempotencyKeyUnsettled`, retryable | no |

An `AlreadyAppended` or `AlreadyAppendedAfterErasure` outcome performs no concurrency check (`ConcurrencyCheckPerformed = false`) and no constraint validation. The event already holds its values, as `EventSequence.Validation.cs:143-148` reasons for publications. `ReactorSideEffectFailure.FromAppendResult` (`Source/Clients/DotNET/Reactors/SideEffects/ReactorSideEffectFailure.cs:74`) turns every refusal into a failed partition, and its messages (`:47-51`) carry the kernel's sentence verbatim.

**Refusal sentences.** These are exact templates; specs assert them. The event type and stored generation come from the stored fingerprint.

- Generation (ruling 4): `Idempotency key '{key}' already appended event type '{eventType}' generation {storedGeneration} at sequence number {sequenceNumber}; this append is generation {requestedGeneration}. Keyed appends are not migrated between generations. To repeat the recorded effect, append generation {storedGeneration}; for a new effect, use a new idempotency key. Nothing was written.`
- Content: `Idempotency key '{key}' already appended event type '{eventType}' generation {storedGeneration} at sequence number {sequenceNumber} with a different event source, route or content. Nothing was written.`
- Unverifiable: `Idempotency key '{key}' already appended event type '{eventType}' at sequence number {sequenceNumber}, but the key that protected its {pii|encrypted} values is not in key storage and no erasure covers it, so this retry cannot be compared. Nothing was written. Restore the key from a backup, or, for personal data that is meant to be gone, erase the subject; then recover the partition.` The grain says this only after it has evicted the identifier locally and read again (section 4). Erasing records a fence even when no key is present (`EncryptionKeyErasure.cs:49-51`).
- Unsettled: `Idempotency key '{key}' could not be settled because another append with the same key was still in progress, or the key protecting its values changed during the append. Nothing was written. Retry the append.`

**Reactor remedy sentence.** When a built-in handler receives a `Content` or `Generation` conflict, `ReactorSideEffectFailure` adds one sentence after the kernel's: `Reactor '{reactor}' produced different output for a delivery whose effects are already appended. To append the delivery's output again under a new key space, raise the reactor's effect epoch with [Reactor("{reactorId}"{, "{eventSequence}"}, effectEpoch: {nextEpoch})], keeping its id and event sequence unchanged, and recover the partition; to keep replays from running the handler again, mark it [OnceOnly].` `ReactorSideEffectFailure` writes the reactor's current id into the sentence, and its event sequence only when the attribute sets one, so that copying the attribute does not change the reactor's identity. Section 8 explains both remedies and what the epoch costs.

No sentence contains a protected value. Built-in reactor keys are hashes, and caller keys and effect names must not hold personal data (section 1).

### 6. AppendMany is settled per event

The grain looks up every keyed event in one storage call and classifies each event as new, done (including done after erasure), conflicting or unverifiable.

- **Any conflict, unverifiable key, unsupported boundary or repeated key:** the whole batch is refused and nothing is written.
- **All events done:** success with the stored numbers and no concurrency check.
- **Mixed batch:** only the new events go through validation (with `ConstraintBatchClaims`, `EventSequence.cs:359`), the concurrency check and one storage transaction, keeping their relative order. `SequenceNumbers` and `Outcomes` follow the request order and mix stored and new numbers.
- **Concurrency scopes on a mixed batch:** scopes are validated for the remainder against the current tail, by the normal rule. A remainder whose scope was overtaken by the batch's own earlier events is refused with a concurrency violation; that is the correct answer once the stream has moved.
- **Unkeyed events in a keyed batch:** always new.
- **Settling again:** after a validation failure, `IdentitiesAlreadyDurable` or `TransactionConflicted`, the grain classifies the batch again by the same rules, and retries the remainder once (section 3).

### 7. Storage per provider

Every provider stores the identity in the publication fields that already exist. There is no new table, no new index and no migration.

- **MongoDB.** `PublicationId` and `PublicationFingerprint` on the event, under the existing `publication_identity` unique sparse index.
  - The batch path writes them inside its session transaction.
  - It classifies a duplicate-key error by looking up the identities and the colliding slots.
  - It reports a `WriteConflict` (`TransientTransactionError`) as `TransactionConflicted` after aborting, instead of rethrowing it (`EventSequenceStorage.cs:440-444`).
- **SQL (SQLite, PostgreSQL, SQL Server).** `PublicationIdentityHash`, `PublicationId` and `PublicationFingerprint`, under the existing filtered unique index on every event-sequence table. `PublicationFingerprint` is an unbounded string column (`EventSequenceMigrator.cs:151, 212`), so the longer protected form fits. The batch path catches `DbUpdateException` and classifies it by lookup, as section 3 describes.
- **InMemory.** The existing `_publications` map, extended to batches under the same lock. It lasts exactly as long as the events, which is the provider's existing contract.

The key therefore survives a kernel restart for as long as the event exists, on MongoDB and SQL. A batch lookup method, `TryGetPublications`, is added to `IEventPublicationStorage`. For `append-key:` identities it returns the sequence number and the stored fingerprint, and compares nothing. The grain reads the event type and generation from the fingerprint rather than from the row, because redaction rewrites the row's type (Context).

### 8. Replay, revision and redaction

- **Keys across replay.** A replayed delivery produces the same key by design (`when_the_event_is_replayed.cs:13-22`), so replaying a reactor that returns events no longer duplicates them.
- **Revision and redaction.** Both keep the identity and fingerprint on the row, so a retry after a redaction cannot bring the redacted content back. The fingerprint still names the original event type and generation (section 4).
- **Changed output on replay (ruling 2).** Revision and redaction rewind affected partitions (`EventSequence.cs:451, 488, 515, 1340-1349`), and a reactor may then produce different content under the same key.
  - The kernel refuses the whole batch. The built-in handlers fail the partition for every delivery kind, replay included, as handlers already propagate batch failures (`EventsForEventSourceIdResultHandler.cs:36-43`, `ReactorInvoker.cs:229-234`). This fails closed.
  - The failed partition's messages carry the refusal sentence and the reactor remedy sentence of section 5. The reason names the key, the stored sequence number and, for a generation change, both generations.
  - A conflict is deterministic, so retries repeat it until someone changes the reactor. Treating a replay conflict as handled would drop any new effects refused in the same batch while reporting success. A specification covers a conflict alongside a new effect.
- **Remedies for a permanent conflict.** Both are deliberate code changes, named in the failed partition's message:
  - **Raise the reactor's effect epoch** (`[Reactor("stable-id", effectEpoch: n)]`, keeping the reactor's id and event sequence as they are, section 1). Raising the epoch abandons every earlier key of the reactor. Recovery then appends the conflicting delivery's entire output, including effects already stored. Every later replayed delivery is appended again unless the handler is also `[OnceOnly]`. Any other failed partition of the reactor appends its stored effects again on recovery. Recover or clear other failed partitions before raising the epoch, and pair the epoch with `[OnceOnly]` when replay must not re-derive effects.
    - The batch was refused whole, so if the replay produced a changed E0' beside an unchanged E1, recovery appends both E0' and E1, and E0 and E1 are already stored.
    - The rest of the rewound range is affected immediately, not only future replays.
    - Keys a handler set on an `EventForEventSourceId` itself do not change with the epoch; the handler changes them.
  - **Mark the handler `[OnceOnly]`** (`OnceOnlyAttribute.cs:7-19`), so a replay does not run it and cannot conflict again. This decides that replays never derive the handler's effects again.
    - It does not clear a partition that has already failed. Recovering a failed partition is an ordinary delivery, not a replay (`Documentation/reactors/once-only.mdx:21-24`), so that delivery still conflicts.
    - Raising the epoch is what clears a failed partition, at the cost above.

### 9. Retention (resolved: life of the event)

| Option | Survives retry window | Replay protected | Cost and risk |
| --- | --- | --- | --- |
| **A. Keep for the life of the event** (as publications do) | always | yes | about 250 bytes per keyed reactor event (more for a longer caller key), plus about 160 per protecting key, plus a sparse or filtered index entry; no job |
| B. On the row, cleared after a configured window by a background job | only within the window | only within the window | a manual partition recovery after the window duplicates again; a new job in every provider |
| C. Separate key store with time-to-live (MongoDB TTL index, SQL sweep) | only within the time-to-live | only within the time-to-live | MongoDB single appends need a multi-document transaction to stay atomic; new schema in every provider; same window hazard |
| D. Kept until no failed partition refers to the triggering event | yes | no | ties event storage to observer state |

**Resolved: A** (ruling 1). The retry window has no bound (Context), and only A keeps the guarantee unconditionally. Any expiry (B, C or D) would introduce a deduplication horizon after which recovery and replay can duplicate again. Adopting one later is a contract revision that must define that horizon and the behavior after it, through a superseding decision.

### 10. Analyzers

**No new analyzer ships with this decision** (ruling 5). The ruling allowed one only if a concrete, non-noisy rule exists today. A rule was evaluated and found noisy: a warning, provisionally CHR0062, on direct append calls inside reactor handler methods.

- It fires on at least two existing reactors that append directly on purpose: `Integration/Client/for_EventAppendCollection/AReactorThatDirectlyAppendsUniqueEvent.cs:11-14` and `Integration/Client/for_Reactors/when_appending_to_outbox/OutboxForwardingReactor.cs:13-16`.
- It would flag the published guidance. "When you want to inspect the outcome yourself, append explicitly" (`Documentation/scenarios/react-to-an-event.mdx:40-42`) and the matching tutorial step (`Documentation/tutorial/reacting.mdx:50-52`) show `explicit-append` snippets. Those snippets are compiled from `Documentation/client-snippets/scenarios/react-to-an-event/explicit-append.md` and `Documentation/client-snippets/tutorial/reacting/explicit-append.md` by `.github/workflows/client-snippets.yml`.
- It has no usable remedy for part of what it reports. Against a kernel without `idempotent-appends`, suppression is the only remedy. For `AppendWithNamedTags`, `AppendThroughEventSource` and `AppendPrepared` there is no keyed equivalent, so it would warn on shapes that cannot be fixed.

The rule is recorded as a possible follow-up. Revisit it once kernels without `idempotent-appends` are out of support and every append shape has a keyed form. The documentation update in PR 5 covers the risk the rule targeted: the explicit-append pages say that a direct append runs again when the partition is recovered, and point to `AppendOnce` with `ReactorDelivery.IdempotencyKeyFor`.

**CHR0022** (`ReactorOnceOnlyAnalyzer.cs:16-24`) still needs its text changed. Its message says that during replay an event-returning handler "will append duplicate events", and `CHR0022.mdx` says the same. Against a kernel that advertises `idempotent-appends`, the keyed built-in handlers make that false. The message, description and `CHR0022.mdx` are updated:

- duplicates on replay happen only against kernels without the capability;
- the remaining reasons for `[OnceOnly]` are that it skips the handler's replay work, and that a replay whose output changed fails the partition (section 8).

Its severity is unchanged.

## Options considered

- **Compare content with `VerifyContent` (#4510).** Its `Unavailable` answer must mean either "done", which is unsafe, or "refuse", which would make every SQL retry fail. It also releases protected values on each check. Not taken. Its canonical comparison remains for explicit verification.
- **Plain SHA-256 fingerprint over plaintext content.** After crypto-shredding it confirms guessed personal data. Refused by ruling 3.
- **Fingerprint over the stored, encrypted form.** Not stable for identical inputs (`Encryption.cs:50, 53, 113`). Recomputing it on a retry also needs encryption, which is refused for an erased subject. Making encryption deterministic would change the at-rest format and leak equality through the ciphertext itself. Not taken.
- **HMAC keyed per event store.** Crypto-shredding erases per-subject keys, not an event-store key. Whoever holds that key, including the kernel through the append path, can still confirm guesses after erasure. This is weaker than ruling 3 requires. Not taken, in favor of section 4.
- **Protected parts without the idempotency key as salt.** Equal values of one subject would store equal parts, which a database copy shows before and after shredding. Not taken.
- **Classify a part as erased whenever any erasure is recorded for its identifier.** It reports erased for a part written under a new lifecycle's key whenever that key is absent, including a stale cached absence. A retry with different personal values would then report success. Not taken; section 4 decides by fingerprint and revision.
- **Refuse a retry whose protected part was erased.** It fails the partition forever over an erasure that was lawful and deliberate. Not taken; `AlreadyAppendedAfterErasure` instead.
- **Implicit migration of a retry to the stored generation.** Not in v1 (ruling 4). It would need migrations on the retry path and a definition of equality across generations. It is added only if a real consumer needs it.
- **A dedicated idempotency table or collection.** It needs a second atomic write per append (a multi-document transaction on MongoDB singles) for the same semantics the event row already gives. Not taken.
- **New columns separate from the publication fields.** That means a migration on every SQL table and a second unique index for an identical contract; the domain prefix already separates the two producers. Not taken.
- **Client-computed fingerprint.** Kotlin, TypeScript and Elixir would each have to reproduce canonical JSON exactly, and clients do not hold encryption keys. Not taken.
- **One key per batch instead of per event.** A partially retried or reshaped batch could not be settled event by event. Not taken; the batch key is only a client convenience that expands per index.
- **Making `DeliveryId` the key.** It is ambiguous on `#` and identifies a delivery, not an effect. Not taken.
- **One reactor derivation for built-in and handler-chosen keys.** A handler's named effect could equal a returned index. Not taken; section 1 separates them by kind.
- **Carrying batch keys as a new property on the existing grain method.** An old silo ignores the property and appends without deduplication. Not taken; section 2 uses new grain methods.
- **Advertising `idempotent-appends` only when every silo supports it.** It needs cluster-wide version knowledge at connection time and still races a rolling upgrade. Not taken; new grain methods fail closed on each call.
- **Refusing all keyed batches that contain an already-done event.** Simpler, but the issue requires that a partially retried batch is not appended again. Not taken.
- **Expiring keys (retention B, C, D).** They reopen duplicates after a horizon. Not taken (section 9).
- **An analyzer on direct appends in reactor handlers (CHR0062).** Noisy against published guidance, with no remedy for some shapes and kernels. Not taken; a possible follow-up (section 10).
- **An analyzer on reactor constructors that inject `IEventLog`.** Noisy, because injection is also used for reads, unlike CHR0015 for projections (`ProjectionSideEffectAnalyzer.cs:16-24`). Not taken.

## Default if unanswered

Nothing ships. Reactor partition recovery and replay keep appending returned events again, Arc reactors that both produce events and run commands keep producing permanent duplicates, and Stage keeps refusing such reactions with STAGE-ESM-037.

## Timeline and scope

Five pull requests, in order. Each lists the specs that prove it. Each new-behavior spec is red before its change.

1. **Storage: identities in batch appends, all providers (patch).** Add `EventToAppendToStorage.Identity` and `TryGetPublications`. Add `AppendManyWithIdentities` returning `AppendManyStorageFailure` (`DuplicateEventSequenceNumber`, `IdentitiesAlreadyDurable` or `TransactionConflicted`, classified by lookup), leaving the existing batch methods unchanged. Keyed receipts carry the sequence number and the stored fingerprint. Specs live in `Storage.InMemory.Specs`, `Storage.MongoDB.Specs` (against `MongoDBFixture`) and `Storage.Sql.Specs` (SQLite and PostgreSQL fixtures), under `for_EventSequenceStorage/when_appending_many_with_identities/`:
   - `and_the_identities_are_written` (receipts resolve)
   - `and_one_identity_is_already_durable` (nothing written; reported as an identity, not a slot; receipt carries sequence number and fingerprint)
   - `and_the_stored_fingerprint_differs` (storage reports, does not refuse)
   - `and_two_batches_race_for_the_same_identity`
   - `and_a_concurrent_transaction_writes_the_same_identity` (MongoDB: the loser aborts and reports `TransactionConflicted`, nothing written)
   - `and_a_sequence_slot_collides` (still `DuplicateEventSequenceNumber`)
   - `and_the_event_is_redacted` (identity and fingerprint survive)
   - `and_a_non_identity_unique_index_is_violated` (SQL: not reported as an identity)
2. **Kernel settlement and fingerprint (minor).** This PR adds:
   - `AppendFingerprint` with its plain and protected parts, and the stored form with event type, generation and key revisions;
   - `IEncryptionKeyStorage.TryGetLatestFor` in every key store, and the revision-reporting forms of `EnsureKeyFor` and `GetOrAddFor` that the encryption and the fingerprint share (the grain's cache eviction uses the existing `IEvictEncryptionKeyCache`, so no new cache API);
   - the key-location member on the four value handlers, and `Apply` reporting the key it used;
   - the `IdempotencyKey` concept and `AppendOutcome`;
   - the grain classification, settling again, and the new grain methods;
   - the refusal sentences.

   Core.Specs and storage specs:
   - each key store's specs, `when_getting_the_latest_key_with_its_revision`
   - `for_CacheEncryptionKeyStorage/when_getting_the_latest_key_with_its_revision/and_the_key_is_cached` (the cached read returns the same key and revision)
   - `for_CompositeEncryptionKeyStorage/when_getting_the_latest_key_with_its_revision/and_the_stores_disagree` (the first store's key and revision, the same key `TryGetFor` returns)
   - `for_AppendFingerprint`:
     - `when_properties_are_reordered` (equal)
     - `when_only_tags_differ` (equal)
     - `when_content_differs`
     - `when_the_generation_differs`
     - `when_a_pii_value_differs` (plain part equal, protected part differs)
     - `when_an_encrypted_value_differs`
     - `when_the_content_carries_pii` (the stored form contains no digest computable without the key; a SHA-256 over the guessed plaintext matches no part)
     - `when_two_keys_protect_the_same_values` (same subject and values, different protected parts)
     - `when_the_content_carries_metadata_without_a_handler` (fingerprinted as plain content, as append stores it)
     - `when_a_boundary_carries_two_protections` (unsupported)
   - `for_AppendFingerprint/when_the_key_changes_during_the_append/`:
     - `and_the_encrypting_key_was_erased_after_the_read` (the protected part is computed under the key that encrypted the value, not under a new lifecycle's key)
     - `and_two_values_of_one_kind_get_different_keys` (`IdempotencyKeyUnsettled`, nothing written)
   - `for_AppendFingerprint/when_comparing_after_the_key_changed/`:
     - `and_the_subject_was_erased` (erased)
     - `and_the_key_was_erased_while_missing` (revision at or below the floor: erased)
     - `and_a_new_lifecycle_was_allowed` (part under the erased key: erased, not a conflict)
     - `and_the_new_lifecycle_key_is_remembered_absent` (key A erased, part written under new-lifecycle key B, this silo's cache remembers B as absent: the grain evicts through `IEvictEncryptionKeyCache` and reads again, so the part is verified, not erased; a retry with different personal values conflicts)
     - `and_the_new_lifecycle_key_is_lost` (unverifiable, not erased)
     - `and_the_key_was_deleted_without_an_erasure` (unverifiable)
     - `and_the_key_storage_is_unavailable` (throws)
   - `for_EventSequence/when_appending_with_an_idempotency_key/`:
     - `and_the_key_is_new`
     - `and_the_key_is_durable_with_the_same_content` (no storage append, no concurrency check)
     - `and_the_key_is_durable_with_different_content` (content sentence)
     - `and_the_key_is_durable_with_another_generation` (exact generation sentence; reason `Generation`; nothing written)
     - `and_a_redacted_event_holds_the_key_with_another_generation` (reason `Generation`, original event type named)
     - `and_the_subject_was_erased_with_the_same_non_personal_content` (`AlreadyAppendedAfterErasure`; no key provisioned; nothing written)
     - `and_the_subject_was_erased_and_non_personal_content_differs` (conflict)
     - `and_the_subject_was_erased_and_a_guessed_value_is_retried` (same outcome for the true value and a wrong guess)
     - `and_the_protecting_key_is_missing_without_an_erasure` (unverifiable sentence)
     - `and_the_key_storage_is_unavailable` (failure, nothing written)
     - `and_the_key_is_durable_at_the_next_sequence_number` and `and_the_key_is_durable_beyond_the_next_sequence_number` (sequence number and per-type tail advanced in both)
     - `and_the_key_is_durable_with_a_later_constraint_change` (no constraint mutation)
     - `and_another_activation_appended_it_first` (settles instead of returning the concurrency violation)
     - `and_a_sequence_slot_collides_with_the_winner_before_the_identity` (the post-renumber concurrency violation is settled, not returned)
     - `and_storage_reports_a_transaction_conflict` (settles a key that is now durable; otherwise retries once)
     - `and_the_conflict_repeats_after_the_retry` (settles again without writing: a key now durable gets its section 4 outcome; a key still absent fails with `IdempotencyKeyUnsettled`, nothing written)
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
     - `and_a_sequence_slot_collides_with_the_winner_before_the_identity`
     - `and_the_identities_are_durable_again_after_the_retry` (settles again without writing: durable keys get their section 4 outcome; only a still-absent key fails with `IdempotencyKeyUnsettled`)
3. **Wire (minor).** Add the Core artifact fields, the three-valued `AppendOutcome`, the conflict details and the five error codes. Build Core, then build Contracts again so the `.proto` files and `chronicle.desc` are regenerated from the new sources, and commit what generation produces. Add `KernelCapabilities.IdempotentAppends` and advertise it. Proven by:
   - the `wire-compatibility.yml` gates (stale check, every released minor still served);
   - validator specs for key length, and for an empty key read as no key;
   - regenerated Workbench proxies.
4. **.NET client API (minor).** The concept, `AppendOnce`/`AppendManyOnce`, `EventForEventSourceId.IdempotencyKey`, outcomes and the capability guard. DotNET.Specs `for_EventSequence/when_appending_with_an_idempotency_key/`:
   - `and_the_kernel_lacks_the_capability` (throws, nothing sent)
   - `and_a_batch_key_is_given` (expanded per index)
   - `and_a_batch_key_index_matches_a_single_key` (`AppendOnce("x:0")` and `AppendManyOnce("x")[0]` send different keys; every expanded key is 64 characters, also for a 512-character key)
   - `and_the_response_reports_already_appended`
   - `and_the_response_reports_already_appended_after_erasure` (`IsSuccess`)
   - `and_the_response_reports_a_generation_conflict` (details and sentence surfaced)
   - `for_IEventSequence/when_calling_existing_overloads_with_null_or_default` (compiles and binds as before; green before and after)
5. **Reactors, CHR0022 text and testing parity (minor).** This PR adds:
   - `ReactorContext.Delivery`, set by `ReactorInvoker` at `ReactorInvoker.cs:223-226, 254-257` from `ResolveDelivery` (`:183-193`);
   - `ReactorAttribute`'s `effectEpoch`, `ReactorDelivery.EffectEpoch` and `ReactorDelivery.IdempotencyKeyFor(string effect)`, with the internal derivation for returned events;
   - keyed built-in handlers, used only when the capability is present, with a caller's own key taking precedence;
   - refusals failing the partition on every delivery kind, with the refusal and remedy sentences as the reason (sections 5 and 8);
   - the revised CHR0022 message and description;
   - `ReactorScenario` parity: `RecordingReactorSideEffectHandlers` records a redelivered keyed effect once, and `ProducedEvent` carries the key.

   Specs:
   - DotNET.Specs `for_ReactorDelivery/when_deriving_an_effect_key/`:
     - `and_the_partition_is_recovered` (same key)
     - `and_the_event_is_replayed` (same key)
     - `and_the_index_differs`
     - `and_a_named_effect_matches_a_returned_index` (different keys)
     - `and_the_effect_epoch_differs` (different keys, same delivery id)
     - `and_the_partition_contains_the_separator` (unambiguous)
   - `for_EventsResultHandler/when_handling/`:
     - `and_keys_are_supported` (indices 0..n-1)
     - `and_a_replayed_effect_conflicts_beside_a_new_one` (partition fails, nothing written)
     - `and_a_replayed_effect_changed_generation` (failed partition messages contain the generation sentence and the remedy sentence, and the remedy contains the reactor's existing id)
     - `and_the_returned_events_are_sent_as_keyed_events` (each is an `EventForEventSourceId` with the context's stream, source type, subject and derived key)
   - `for_EventsWithConcurrencyScopesResultHandler/when_handling/and_concurrency_scopes_are_returned` (keys and scopes both sent)
   - `for_ReactorSideEffectFailure/when_adding_the_remedy/`:
     - `and_the_reactor_has_an_explicit_event_sequence` (the sentence names the id and the event sequence)
     - `and_the_reactor_has_no_explicit_event_sequence` (the sentence names the id only)
   - `for_EventForEventSourceIdResultHandler/when_handling/`:
     - `and_keys_are_supported` (index 0)
     - `and_the_event_carries_its_own_key` (sent unchanged)
     - `and_the_event_carries_its_own_key_and_the_kernel_lacks_the_capability` (partition fails with `IdempotentAppendsNotSupported`, nothing appended)
   - `for_ReactionInvoker/when_a_partition_fails_and_is_recovered/and_the_handler_returned_events` (same keys twice)
   - Testing.Specs `for_ReactorScenario/when_redelivering/with_returned_events`
   - Integration (`Integration/Client`, MongoDB and SQL runs, including SQL Server):
     - `when_a_reactor_partition_fails_after_appending_its_events`: one event per effect after recovery
     - `when_the_kernel_restarts_between_attempts`: the key still settles
     - `when_the_subject_is_erased_between_attempts`: the retry settles as `AlreadyAppendedAfterErasure`, one event, no new key
     - `when_the_effect_epoch_is_raised_after_a_replay_conflict`: the recovered partition appends the delivery's whole output again, including the unchanged effect, which the documentation states as the cost

   Documentation:
   - `reactors/delivery-identity.mdx`:
     - the built-in handlers are now idempotent, and replay conflicts fail the partition;
     - the two remedies and their limits (section 8);
     - `IdempotencyKeyFor(string effect)`, and the precedence of a caller's own key;
   - `reactors/side-effects.mdx` and `reactors/once-only.mdx`: replay no longer duplicates returned events against kernels with `idempotent-appends`, and the remaining reasons for `[OnceOnly]`;
   - `code-analysis/CHR0022.mdx`: the revised text (section 10);
   - appending docs (`events/appending.mdx`, `events/appending-many.mdx`): keys, outcomes, the empty-string rule, the no-personal-data rule for keys, and behavior after erasure;
   - `scenarios/react-to-an-event.mdx` and `tutorial/reacting.mdx`: a direct append in a handler runs again when the partition is recovered, and `AppendOnce` with `IdempotencyKeyFor` prevents it (the compiled snippets are unchanged);
   - `clients/where-the-clients-differ.md`: the other clients have the wire fields but no derivation yet.

In scope: the event sequence append path, the three storage providers, the key stores' revision-aware read, the value handlers' key location, the wire contract, the .NET client, reactor side effects and the effect epoch, CHR0022's text and Chronicle.Testing.

Out of scope:

- Repairing unique-constraint claims after a crash between an event write and its claim update, and the stale constraint mutations that publication retries apply today. Both are tracked in Cratis/Chronicle#4895.
- Delivering an event whose turn failed after the write to an observer that stays subscribed (section 3).
- Purging soft-deleted Azure Key Vault secrets on erasure (section 4, residual exposure).
- Arc's `EventsAndCommandsResultHandler`. Arc keys the events it appends, by adopting `ReactorContext.Delivery` in its own repository. Whether and how a command runs again on redelivery is Arc's concern. A question to the Arc maintainers is pending, and this decision does not depend on their answer.
- The transactional unit-of-work commit path. It inherits the per-event key through `EventForEventSourceId` but gets no new API.
- Implicit migration of keyed retries between generations (ruling 4).
- Publication sink fingerprints. They never cover protected values today (`EventSequenceSinkFactory.cs:31-36`); if compliance targets are allowed later, they adopt section 4.
- Key exposure on reads and event export.
- Derived APIs for the Kotlin, TypeScript and Elixir clients.
- Possible analyzer follow-ups:
  - the rule on direct appends in reactor handlers, once it can be made non-noisy (section 10);
  - appends reached through helpers or injected services;
  - revisiting CHR0022's severity once kernels without `idempotent-appends` are out of support.

### Resolutions

L0 resolved the six open questions on 2026-10-14, on behalf of woksin. L0 accepted the record on the same day.

1. **Key retention:** for the life of the event (section 9, option A). Any expiry would reopen duplicates.
2. **Replay conflicts:** fail the partition and fail closed, with the refusal sentence as the failed partition's reason (sections 5 and 8). The deliberate remedies are a reactor effect epoch or `[OnceOnly]`, both named in the failed partition's message (section 8).
3. **Fingerprint and crypto-shredding:** the fingerprint must not survive crypto-shredding.
   - The stored, encrypted form is not stable for identical inputs, so it cannot be used.
   - The ruling's fallback, an HMAC keyed per event store, is weaker than the requirement, because crypto-shredding does not erase that key.
   - Section 4 therefore fingerprints protected values under keys derived from the keys that protect them, salted with the idempotency key so that no two events' parts can be compared. It settles a retry after erasure on the remaining parts.
   - This replaces the ruling's fallback with a stronger construction, at the cost section 4 lists, and leaves the residual exposure section 4 lists. L0 confirms the substitution on acceptance.
4. **Generation-bump retries:** refused as `IdempotencyKeyConflict` with the actionable generation sentence (section 5). No implicit migration in v1; it is added only if a real consumer needs it.
5. **Analyzer:** the ruling allowed an analyzer only if a concrete, non-noisy rule exists today; otherwise none, recorded as a possible follow-up.
   - A rule on direct appends in reactor handlers was evaluated and found noisy. It fires on two existing reactors that append directly on purpose. It would flag compiled, published guidance to append explicitly. And it has no remedy against older kernels, or for append shapes that have no keyed form.
   - No analyzer ships with this decision; the rule is recorded as a follow-up (section 10).
   - CHR0022's text is corrected in PR 5.
6. **Arc command keys:** Arc's decision, and a question has gone to the Arc maintainers. Arc keys the events, and command re-execution is Arc's concern. This decision does not depend on Arc's answer.

## Verification

**Done when:** in every provider:

- A keyed append retried after a partition failure, a kernel restart, a duplicate concurrent delivery or a lost response produces exactly one event and reports `AlreadyAppended` with its sequence number.
- Two activations racing on one key write exactly one event. The loser settles to the winner's receipt, or fails with `IdempotencyKeyUnsettled` and writes nothing; this holds when the first collision is the sequence slot, and for a MongoDB transaction conflict.
- The same key with different content, or with a different generation, is refused with the exact sentence and nothing written; a redacted event still reports its original type and generation.
- A keyed batch retried with extra events appends only the new ones.
- After the subject is crypto-shredded:
  - a retry with equal non-personal content reports `AlreadyAppendedAfterErasure`, provisions no key and writes nothing;
  - the outcome is the same for the true personal values and for a guess;
  - no stored value lets anyone confirm a guessed personal value without the subject's key.
- Two keyed events of the same subject with equal personal values store different protected parts.
- A part written under a new lifecycle's key is never reported as erased, even while a cache remembers that key as absent.
- A protecting key that is missing without a covering erasure refuses the retry as unverifiable.
- A replay conflict leaves a failed partition whose reason names the key, the sequence number and both remedies. Raising the effect epoch lets the recovered partition append its output again (spec: the unchanged effect is appended a second time, and the cost is documented).
- A keyed call that reaches a silo without the new grain methods fails; it is never appended unkeyed.
- A released-minor client against the new kernel, and a new client against an old kernel, both behave as they do today.

**Verify by:**

- The PR 1 storage specs on InMemory, MongoDB (against `MongoDBFixture`, including the concurrent-transaction spec) and SQL (SQLite and PostgreSQL).
- The PR 2 Core.Specs and key-store specs, including every settle-again path for single and batch appends.
- The `wire-compatibility.yml` gates and the validator specs of PR 3.
- The DotNET.Specs of PR 4 and PR 5, and the Testing.Specs parity spec.
- The PR 5 integration specs on the MongoDB and SQL runs, including SQL Server.
- Each new-behavior spec is red before its change. The existing-behavior regression specs, such as the overload-binding spec, are green before and after.

## Consequences

- Once the client and kernel both carry this, reactors that return events become idempotent across recovery and replay without code changes. Arc and Stage get the seam they asked for. STAGE-ESM-037 can be lifted when the kernel advertises `idempotent-appends` and Arc keys its events.
- **Public API:** additive only, so the client and kernel both ship as minor.
  - New `AppendOnce`/`AppendManyOnce` methods are default interface members, so existing overload resolution is unchanged.
  - `EventForEventSourceId` gets an init property, and results get new properties and a three-valued outcome.
  - `ReactorAttribute` gets an optional `effectEpoch` parameter, and `ReactorDelivery` gets `EffectEpoch` and `IdempotencyKeyFor(string effect)`; delivery ids do not change.
  - `ReactorContext` gets a new init property.
  - In the kernel, `IJsonSchemaMetadataValueHandler` gets a key-location member, and `IEncryptionKeyStorage` gets `TryGetLatestFor`.
- **Wire:** new optional fields only, and an empty key means no key. An old client never sends a key and sees no new behavior. An old kernel never advertises the capability, so a new client refuses a keyed append before sending; the built-in handlers fall back to today's unkeyed appends and log once per reactor. In a mixed-version cluster, a keyed call that reaches an old silo fails instead of appending without its key.
- **Replay behavior changes.**
  - A replayed reactor that once appended duplicates now appends nothing.
  - A reactor whose replayed output differs, including in generation, fails its partition with the refusal and remedy sentences instead of appending a contradicting event.
  - Someone then raises the reactor's effect epoch, which starts a new key space, or marks the handler `[OnceOnly]` to prevent later conflicts (section 8).
- **Storage:** keyed events carry about 250 bytes more for a reactor key, more for a longer caller key, plus about 160 per protecting key, and an index entry, kept for the life of the event. No migration is needed.
- **Privacy:**
  - The fingerprint holds no plain digest of protected values. Their parts are keyed by keys derived from the protecting keys, so crypto-shredding makes them unverifiable by anyone, including the kernel.
  - Each part is salted with its idempotency key, so equal personal values in different events are not visible as equal.
  - After erasure, the comparison covers only the parts that do not depend on erased keys, so a differing personal value is not detected. This is by design.
  - Idempotency keys are stored in plaintext and survive shredding, so they must not carry personal data.
  - Settling a keyed hit reads the protecting key, but never provisions one. A miss with protected values uses the private key on the append path.
  - Key backups and soft-deleted Key Vault secrets can still undo shredding, for the fingerprint as for the ciphertext (section 4, residual exposure).
- **Analyzers:** no new analyzer. CHR0022's message and description change, and its severity does not.
