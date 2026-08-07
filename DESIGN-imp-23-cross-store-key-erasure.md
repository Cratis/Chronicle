# Design note — erasing a subject whose encryption key was copied across event stores

Status: **open question, no implementation.** This note exists because IMP-23 asks for three capabilities
that change public API, and picking one without a ruling would be worse than not starting. Written against
`origin/main` at `v16.19.1-4-g1f0ce2bbe`.

Ask 4 (documentation) and the observability half of ask 2 (a log line) are already implemented on this
branch and are out of scope here. What follows covers asks 1, 2 and 3.

## What is actually true, measured

Two integration specs in `Integration/Client/for_PIIManager/` run against a real Orleans silo and a real
MongoDB and record the mechanism:

- Forwarding a subject's event from event store A to event store B copies **the very same key pair** into
  B. Not an equivalent key — the same `Public`/`Private` bytes.
- Erasing through A removes A's copy and leaves B's readable.
- A later event forwarded **back** from B into A finds no key in A, copies B's survivor in, and the PII in
  A that was already crypto-shredded **reads in clear again**.

Mutation-proving that by deleting the `CopyEncryptionKeyIfMissingForTargetStore` call turns exactly three
facts red — the two key-material facts and `should_make_the_erased_pii_readable_again` — and leaves the
key-*presence* facts green, because the target mints a key of its own on the inbox append. Two corrections
follow from that, and they matter for the design:

1. **The copy is not what makes the forwarded payload decryptable.** Observers receive events already
   decrypted (`Observer.Handling.cs:133` → `DecryptEvents`), and the inbox append re-encrypts under
   whatever key the target holds for that subject, minting one when there is none. Forwarding works with
   the copy removed. What the copy buys is *one key pair per subject across both stores*.
2. **What resurrects is therefore not "a key" but "the original key".** A store that has been erased will
   get a key again the moment anything appends PII for that subject there; that is correct and harmless.
   The defect is narrower and sharper: the copy re-installs the *pre-erasure* key material, which is the
   only thing that can make already-shredded ciphertext readable.

A third correction to the report's framing: a consumer **can** enumerate the surface today.
`IChronicleClient.GetEventStores()` and `IEventStore.GetNamespaces()` are both public, both real, and the
second spec runs the full fan-out over them and erases both copies. So ask 1 is convenience, and ask 2 is
diagnostics. **Ask 3 is the only one a consumer cannot write.**

## Current surface, for reference

| Layer | Today |
|---|---|
| Client | `IPIIManager.DeleteEncryptionKeyFor(EncryptionKeyIdentifier)`; `IEventStore.PII` is bound to one `(event store, namespace)` at construction (`EventStore.cs:249`) |
| Contract | `DeleteEncryptionKeyRequest { EventStore, Namespace, Identifier }`, `ICompliance.DeleteEncryptionKey` |
| Proto | `Source/Kernel/Protobuf/compliance.proto`, **generated from the C# contracts** by `Tools/ProtoGenerator` |
| Service | `ComplianceService.DeleteEncryptionKey` → `IPIIManager` grain keyed by `PIIManagerKey(EventStore, Namespace)` |
| Grain | `PIIManager.DeleteEncryptionKeyFor` → `IEncryptionKeyStorage.DeleteFor` + `IEncryptionKeyCacheClient.Evict` |
| Storage | `IEncryptionKeyStorage` — every method takes `(EventStoreName, EventStoreNamespaceName, EncryptionKeyIdentifier, EncryptionKeyRevision?)` |

Backend scoping, all five implementations:

| Backend | Key location |
|---|---|
| MongoDB | database per `(store, namespace)`, collection `encryption-keys`, `_id` = `(identifier, revision)` |
| SQL | `IDatabase.Namespace(store, namespace)` scope, `EncryptionKeys` table |
| Vault (KV v2) | path `{store}/{namespace}/{identifier}/{revision}` |
| Azure Key Vault | secret `chronicle--{store}--{namespace}--{identifier}--{revision}` (already listed and prefix-filtered) |
| In-memory | dictionary keyed by the same tuple |

Two decorators sit in front of all of them and both matter here:

- `CacheEncryptionKeyStorage` — positive cache plus a 5-second negative cache, per silo. `DeleteFor` clears
  both locally; cluster-wide eviction is a separate path (`IEncryptionKeyCacheClient` →
  `EncryptionKeyCacheGrainService`).
- `CompositeEncryptionKeyStorage` — `TryGetFor` **writes the key back** into every inner store that lacks
  it. That is a second, independent resurrection path, inside one `(store, namespace)` pair.

## Option 1 — cascade delete

**Surface.** Either an overload — `DeleteEncryptionKeyFor(EncryptionKeyIdentifier identifier, bool cascade)`
— or a distinct operation. Prefer the distinct operation: `cascade: true` on a method hanging off
`IEventStore.PII` reads as "cascade from *this* event store", which is exactly the wrong mental model,
because the fan-out is cluster-wide and has nothing to do with which store you resolved. Suggested shape:

```csharp
// Cratis.Chronicle.Compliance.GDPR, on IChronicleClient rather than IEventStore
Task EraseSubjectEverywhere(EncryptionKeyIdentifier identifier);
```

**Touch points.**

- Contract: new `EraseSubjectEverywhereRequest { Identifier }` (no event store, no namespace — that is the
  point), new `rpc` on `ICompliance`.
- Proto: regenerated; additive.
- Kernel: a new operation on `ComplianceService`. It cannot live on the existing `PIIManager` grain —
  `PIIManagerKey` *is* the `(store, namespace)` pair, so that grain has no address for the fan-out. Either a
  new grain keyed by identifier alone, or service-level orchestration over `IStorage.GetEventStores()` and
  `INamespaces.GetAll()` calling each per-pair `IPIIManager`. Both already exist kernel-side.
- Client: one method on `IChronicleClient` + implementation; a default interface method keeps it
  non-breaking for external implementers.
- Storage backends: **none**. The cascade is composed from `DeleteFor` calls that already exist.
- Cache: none beyond what each per-pair delete already does.

**Wire compatibility.** Additive: new message, new rpc. Passes `grpc-compatibility`. Version skew is the
only cost — a new client against an old kernel gets `UNIMPLEMENTED`, which is a loud failure, not a silent
one. `minor`.

**Blast radius.** Small and contained. Kotlin, TypeScript and Elixir clients expose no compliance surface
today, so they are unaffected until they choose to adopt it.

**Still owed.** Semantics on partial failure: if the delete succeeds in three stores and the fourth is
unreachable, is the call an error, and is the caller told *which* pairs were cleared? A cascade that
reports success after clearing some of the copies is worse than the manual fan-out, because the consumer
loses the per-store result they have today.

## Option 2 — ask which event stores hold a key

**Surface.**

```csharp
Task<IEnumerable<(EventStoreName EventStore, EventStoreNamespaceName Namespace)>> WhichHoldAKeyFor(
    EncryptionKeyIdentifier identifier);
```

**Two implementations, very different costs.**

*Probe.* Enumerate `(store, namespace)` pairs and call the existing `IEncryptionKeyStorage.HasFor` on each.
No new storage surface anywhere, exact by construction, O(stores × namespaces) round-trips. On Mongo that is
an indexed `Find(...).Limit(1)` per pair; on Azure Key Vault it is a secret listing per pair, which is
already how `GetHighestRevision` works.

*Reverse index.* A cluster-scoped `identifier → pairs` index. Fast, and the wrong trade: it adds a new
persisted artifact to **all five** backends (a new Mongo collection outside any per-pair database, a new SQL
table outside the per-namespace context, a new Vault path outside the `{store}/{ns}` tree, a new AKV secret
naming scheme, a new in-memory map), it must be written atomically with every `SaveFor`/`GetOrAddFor`/
`DeleteFor` or it drifts into lying, and it is itself a durable list of identifiers that have held PII —
which is a retention question of its own.

**Touch points (probe).** Contract + proto: new request/response, additive. Service: new operation. Grain:
same addressing problem as option 1, same two answers. Client: one method. Storage: none. Cache: the
negative cache would make a probe answer stale for up to 5 seconds; either bypass the cache for this call
or accept it and say so.

**Wire compatibility.** Additive; `minor`.

**Still owed.** Whether this is worth shipping at all once option 1 exists. Its remaining value is
diagnostic — "prove to my auditor that no store holds this key" — and that is a real use, but it is a
different feature from erasure and should be argued on its own.

## Option 3 — a tombstone the copy honours

This is the one that cannot be worked around, and the one with the most unanswered questions.

**What it has to defeat.** `CopyEncryptionKeyIfMissingForTargetStore` restores a key precisely because the
target has none. Any tombstone that is not consulted *on that path*, and is not durable, changes nothing.
Three concrete hazards:

1. **In-memory is not enough.** `CacheEncryptionKeyStorage`'s negative cache already remembers absence, and
   it deliberately expires after five seconds because another silo may have provisioned the key. A
   tombstone with that lifetime is not a tombstone.
2. **It must be atomic with the delete.** A tombstone written as a second operation after `DeleteFor` leaves
   a window in which the key is gone and the tombstone is not yet there — the exact window the feature
   exists to close.
3. **`CompositeEncryptionKeyStorage` back-fills.** Its `TryGetFor` copies the key into every inner store
   that lacks it. A tombstone honoured only by the subscriber would still be defeated by a composite
   configuration.

**Where it should live.** In the encryption key store, as a *state of the key record* rather than as a
separate artifact — that is what makes it atomic with the delete and impossible to drift. Concretely:
`DeleteFor` stops leaving nothing behind and instead leaves a record at a reserved revision (Mongo: a
document with `EncryptionKeyRevision.Erased`; SQL: a row with null key material; Vault/AKV: a
`.../{identifier}/erased` path or `--erased` secret; in-memory: an entry in the same dictionary).
`TryGetFor`/`HasFor`/`GetFor` keep answering exactly as they do today — no key — so nothing on the read or
release path changes. Only provisioning consults it.

Two consequences of that placement, both load-bearing:

- **The provisioning contract has to be able to refuse.** `GetOrAddFor` returns a non-nullable
  `EncryptionKey` today and is a public interface method with a default implementation. It cannot start
  returning `null` without a binary break. The wire-safe move is a new member —
  `Task<EncryptionKey?> TryProvisionFor(...)` — but a default interface implementation that ignores
  tombstones is exactly the silently-succeeding stub `framework.md` forbids: every backend that does not
  override it would keep resurrecting keys while the feature reports as shipped. Either the default throws,
  or all five backends land in the same change.
- **Cluster-wide propagation.** `CacheEncryptionKeyStorage` must cache the tombstone as a distinct state
  from "absent" (absence expires; a tombstone must not), and `IEncryptionKeyCacheClient` /
  `EncryptionKeyCacheGrainService` must propagate its arrival the way they propagate eviction today.

**Touch points.** `IEncryptionKeyStorage` (new member + a documented clause on `GetOrAddFor`), all five
backends, both decorators, `PIIManager` (write the tombstone inside the delete),
`EventStoreSubscriptionObserverSubscriber` (use the refusing primitive and log the refusal),
`EncryptionKeyRevision` (a reserved value) or an equivalent marker, the cache grain service. No contract,
no proto, no client change — **this option is entirely kernel-internal**, which is why it is the cheapest of
the three on the wire and the most expensive in the storage layer.

**Wire compatibility.** No proto change. `IEncryptionKeyStorage` is public shipped API, so the semver call
depends on the shape chosen: a new default-implemented member is `minor`; a change to `GetOrAddFor`'s
nullability is `major`.

**Still owed — and this is the ruling.** Three questions, none of which Chronicle has answered and none of
which an implementer may answer on its behalf:

1. **Does a tombstone block all provisioning, or only the copy?** If it blocks everything, a person who
   re-registers can never hold PII in that pair again and their new data is silently blanked — a data-loss
   footgun strictly worse than the defect. If it blocks only the forwarding copy, then it is not a property
   of the key store at all but of the subscription path, and belongs behind a narrower seam.
2. **How long does a tombstone live, and what clears it?** Never cleared means an unbounded, permanent set
   of identifiers that once held PII. Cleared on a new append means it protects nothing after the first new
   event. Cleared on a timer means the retention period is a Chronicle policy — and IMP-23 explicitly
   excludes retention policy from the ask.
3. **Is the tombstone itself personal data?** It is a durable record that a named subject was erased. If it
   is, it needs its own erasure story, and the recursion has to stop somewhere by decision, not by accident.

## Recommendation

**Ship option 1, decline option 2 for now, and do not start option 3 without a ruling.**

Option 1 is additive on the wire, needs no storage change, and turns a hard-coded list of event store names
— the shape a consumer writes today, which fails silently when it goes stale — into one call the kernel
owns. Option 2's remaining value after option 1 is auditing rather than erasure, and the log line already
shipped on this branch makes the propagation visible; a reverse index is not worth new persisted state in
five backends. Option 3 is the only ask that closes the hole, and it is blocked on question 1 above:
until Chronicle decides whether an erased subject may ever hold a key in that pair again, every tombstone
design is either a resurrection that still happens or a silent data loss.

**The open question Chronicle must answer, in one line:** *is erasing a subject in an
`(event store, namespace)` pair a permanent refusal to hold that subject's key there again, or only the
removal of the key that exists now?* Option 3 is a two-week change once that is decided and unbuildable
before it.
